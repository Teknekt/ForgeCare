using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessExecutableInspectorTests
{
    [TestMethod]
    public async Task FacadeProjectsStartupInspectorsIntoNeutralImmutableResult()
    {
        var file = new StubFileInspector();
        var signature = new StubSignatureInspector();
        var inspector = new WindowsProcessExecutableInspector(file, signature);

        ProcessExecutableInspection result = await inspector.InspectAsync(@"C:\Apps\agent.exe");

        Assert.AreEqual(ProcessFileInspectionStatus.Available, result.FileStatus);
        Assert.AreEqual("File Company", result.CompanyName);
        Assert.AreEqual(ProcessSignatureStatus.Valid, result.SignatureStatus);
        Assert.AreEqual("Certificate Signer", result.SignerName);
        Assert.AreEqual(1, file.Calls);
        Assert.AreEqual(1, signature.Calls);
        Assert.IsFalse(result.GetType().GetProperties().Any(property =>
            property.PropertyType.Name.Contains("Startup", StringComparison.Ordinal)));
    }

    private sealed class StubFileInspector : IStartupFileInspector
    {
        public int Calls { get; private set; }
        public Task<StartupFileInspection> InspectAsync(string resolvedPath, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new StartupFileInspection
            {
                Status = StartupFileInspectionStatus.Available,
                NormalizedPath = resolvedPath,
                Exists = true,
                FileName = "agent.exe",
                Extension = ".exe",
                CompanyName = "File Company",
                ProductName = "Product"
            });
        }
    }

    private sealed class StubSignatureInspector : IStartupSignatureInspector
    {
        public int Calls { get; private set; }
        public Task<StartupSignatureInfo> InspectAsync(string resolvedPath, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new StartupSignatureInfo
            {
                Status = StartupSignatureStatus.Valid,
                SignerName = "Certificate Signer"
            });
        }
    }
}
