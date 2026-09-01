using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class UpdateHardeningTests
{
    private const string AppId = "{0F34D1F2-0B94-4F4F-A63D-F0A15E7D11C7}";
    private const string Secret = "Bearer abcdef123456 C:\\Users\\Alice\\SecretCustomer sk-test-secret-value";

    [TestMethod]
    public void MissingAndMalformedLocalManifestFailWithoutRawDetails()
    {
        using var temp = new TemporaryDirectory();
        var service = new UpdateDiscoveryService();

        UpdateCheckResult missing = service.CheckLocalManifest(string.Empty);
        Assert.AreEqual("NO MANIFEST", missing.State);

        string path = Path.Combine(temp.Path, "manifest.json");
        File.WriteAllText(path, "{ invalid " + Secret);
        UpdateCheckResult malformed = service.CheckLocalManifest(path);

        Assert.AreEqual("INVALID MANIFEST", malformed.State);
        Assert.AreEqual("The selected update manifest is malformed or unreadable.", malformed.Detail);
        Assert.IsFalse(malformed.Detail.Contains(Secret, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task RemoteOfflineFailureIsDistinctAndPrivacySafe()
    {
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            throw new HttpRequestException(Secret)));
        var service = new RemoteUpdateDiscoveryService(client);

        RemoteUpdateCheckResult result = await service.CheckAsync(
            "https://updates.example.test/release-manifest.json?token=secret-value",
            "beta");

        Assert.AreEqual("OFFLINE / CHECK FAILED", result.State);
        Assert.AreEqual("https://updates.example.test/release-manifest.json", result.ManifestUrl);
        Assert.IsFalse(result.Detail.Contains(Secret, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.ManifestUrl.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ValidRemoteManifestKeepsSuccessfulDiscoverySemantics()
    {
        string json = $$"""
            {
              "product": "ForgeCare",
              "version": "1.2.0-beta.1",
              "channel": "beta",
              "appId": "{{AppId}}",
              "installer": {
                "file": "ForgeCare-v1.2.0-beta.1-Setup.exe",
                "sha256": "{{new string('A', 64)}}"
              }
            }
            """;
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            })));
        var service = new RemoteUpdateDiscoveryService(client);

        RemoteUpdateCheckResult result = await service.CheckAsync(
            "https://updates.example.test/release-manifest.json",
            "beta");

        Assert.AreEqual("UPDATE AVAILABLE", result.State);
        Assert.IsTrue(result.UpdateAvailable);
        Assert.AreEqual("ForgeCare-v1.2.0-beta.1-Setup.exe", result.InstallerFile);
        Assert.AreEqual(new string('A', 64), result.InstallerSha256);
    }

    [TestMethod]
    public async Task HttpFailureDoesNotExposeServerReasonPhrase()
    {
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = Secret
            })));
        var service = new RemoteUpdateDiscoveryService(client);

        RemoteUpdateCheckResult result = await service.CheckAsync(
            "https://updates.example.test/release-manifest.json",
            "beta");

        Assert.AreEqual("CHECK FAILED", result.State);
        Assert.AreEqual("Manifest request returned HTTP 503.", result.Detail);
        Assert.IsFalse(result.Detail.Contains(Secret, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task DownloadHashMismatchRemainsExplicitAndDeletesPartialFile()
    {
        using var temp = new TemporaryDirectory();
        byte[] payload = Encoding.UTF8.GetBytes("not-the-expected-installer");
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            })));
        var service = new SecureUpdateDownloadService(client, temp.Path);

        SecureUpdateDownloadResult result = await service.DownloadAndVerifyAsync(
            "https://updates.example.test/release-manifest.json",
            "ForgeCare-Setup.exe",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("different"))));

        Assert.AreEqual("HASH MISMATCH", result.State);
        Assert.IsFalse(result.Success);
        Assert.IsFalse(File.Exists(Path.Combine(temp.Path, "ForgeCare-Setup.exe.partial")));
    }

    [TestMethod]
    public async Task SuccessfulDownloadRemainsVerifiedAndUsesInjectedStorage()
    {
        using var temp = new TemporaryDirectory();
        byte[] payload = Encoding.UTF8.GetBytes("verified-installer-content");
        string expected = Convert.ToHexString(SHA256.HashData(payload));
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            })));
        var service = new SecureUpdateDownloadService(client, temp.Path);

        SecureUpdateDownloadResult result = await service.DownloadAndVerifyAsync(
            "https://updates.example.test/release-manifest.json",
            "ForgeCare-Setup.exe",
            expected);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("VERIFIED", result.State);
        Assert.AreEqual(Path.Combine(temp.Path, "ForgeCare-Setup.exe"), result.DownloadPath);
        Assert.AreEqual(expected, result.ActualSha256);
        CollectionAssert.AreEqual(payload, await File.ReadAllBytesAsync(result.DownloadPath));
    }

    [TestMethod]
    public async Task NullRemoteManifestUrlFailsSafelyWithoutNetworkAccess()
    {
        using var client = new HttpClient(new DelegateHandler((_, _) =>
            throw new AssertFailedException("Network handler must not be called.")));
        var service = new RemoteUpdateDiscoveryService(client);

        RemoteUpdateCheckResult result = await service.CheckAsync(null, "beta");

        Assert.AreEqual("INVALID URL", result.State);
        Assert.AreEqual(string.Empty, result.ManifestUrl);
    }

    [TestMethod]
    public void InstallerHandoffConfirmationGateRequiresExplicitCheckedState()
    {
        string source = ReadMainWindowSource();
        string prepare = ExtractMethod(source, "PrepareInstallerHandoffButton_Click");
        string confirmation = ExtractMethod(source, "ConfirmInstallerHandoffCheckBox_Checked");

        StringAssert.Contains(
            prepare,
            "ConfirmInstallerHandoffCheckBox.IsChecked =\n            false;");
        StringAssert.Contains(
            prepare,
            "LaunchVerifiedInstallerButton.IsEnabled =\n            false;");
        StringAssert.Contains(
            prepare,
            "ConfirmInstallerHandoffCheckBox.IsEnabled =\n            validation.Success;");
        StringAssert.Contains(
            confirmation,
            "ConfirmInstallerHandoffCheckBox.IsChecked == true &&");
        StringAssert.Contains(
            confirmation,
            "_lastSecureUpdateDownload?.Success == true;");
    }

    [TestMethod]
    public void InstallerHandoffConfirmationGateFailsClosedOnPreparationFailureAndReset()
    {
        string source = ReadMainWindowSource();
        string prepare = ExtractMethod(source, "PrepareInstallerHandoffButton_Click");
        string reset = ExtractMethod(source, "ResetInstallerHandoffConfirmationUi");
        string discovery = ExtractMethod(source, "CheckRemoteUpdateButton_Click");
        string download = ExtractMethod(source, "DownloadVerifiedUpdateButton_Click");

        StringAssert.Contains(
            prepare,
            "ConfirmInstallerHandoffCheckBox.IsEnabled =\n            validation.Success;");
        StringAssert.Contains(
            reset,
            "ConfirmInstallerHandoffCheckBox.IsChecked =\n            false;");
        StringAssert.Contains(
            reset,
            "ConfirmInstallerHandoffCheckBox.IsEnabled =\n            false;");
        StringAssert.Contains(
            reset,
            "LaunchVerifiedInstallerButton.IsEnabled =\n            false;");
        StringAssert.Contains(discovery, "ResetInstallerHandoffConfirmationUi();");
        StringAssert.Contains(download, "ResetInstallerHandoffConfirmationUi();");
        StringAssert.Contains(discovery, "_lastSecureUpdateDownload =\n                null;");
        StringAssert.Contains(download, "_lastSecureUpdateDownload =\n            null;");
    }

    [TestMethod]
    public void InstallerLaunchHandlerRetainsExecutionTimeConfirmationCheck()
    {
        string source = ReadMainWindowSource();
        string launch = ExtractMethod(source, "LaunchVerifiedInstallerButton_Click");

        StringAssert.Contains(
            launch,
            "ConfirmInstallerHandoffCheckBox.IsChecked != true ||");
        StringAssert.Contains(launch, "CONFIRMATION REQUIRED");
        StringAssert.Contains(launch, "ValidateForHandoffAsync(");
        StringAssert.Contains(launch, "LaunchInstaller(");
    }

    private static string ReadMainWindowSource()
    {
        string path = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "ForgeCare.app",
                "MainWindow.xaml.cs"));

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string ExtractMethod(
        string source,
        string methodName)
    {
        Match signature = Regex.Match(
            source,
            $@"private\s+(?:async\s+)?void\s+{Regex.Escape(methodName)}\s*\(");

        Assert.IsTrue(signature.Success, $"Method '{methodName}' was not found.");

        int start = source.IndexOf('{', signature.Index);
        if (start < 0)
            Assert.Fail($"Method '{methodName}' has no body.");

        int depth = 0;
        for (int index = start; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        Assert.Fail($"Method '{methodName}' body was incomplete.");
        return string.Empty;
    }

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
