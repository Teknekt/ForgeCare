using System.Reflection;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceSafetyTests
{
    private static readonly Type[] ProductionTypes =
    [
        typeof(StartupCommandParser),
        typeof(WindowsStartupFileInspector),
        typeof(WinVerifyTrustStartupSignatureInspector),
        typeof(StartupClassificationPolicy),
        typeof(StartupIntelligenceService)
    ];

    [TestMethod]
    public void FoundationExposesNoScannerManagerEvidenceOrUiDependencies()
    {
        string[] forbidden =
        [
            "StartupScanner", "SystemScanner", "StartupManagerService",
            "StartupImpactService", "StartupChangeItem", "StartupUndoRecord",
            "SafetyJournalService", "StartupReviewWindow", "EvidenceService",
            "IEvidenceRepository", "MainWindow"
        ];

        foreach (Type type in ProductionTypes)
        {
            IEnumerable<Type> dependencies = type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(field => field.FieldType));

            foreach (Type dependency in dependencies)
            {
                Assert.IsFalse(
                    forbidden.Any(name => dependency.FullName?.Contains(name, StringComparison.Ordinal) == true),
                    $"{type.Name} references forbidden dependency {dependency.FullName}.");
            }
        }
    }

    [TestMethod]
    public void FoundationSourceContainsNoMutationExecutionOrNetworkApis()
    {
        string root = FindRepositoryRoot();
        string[] files = Directory.GetFiles(
            Path.Combine(root, "ForgeCare.app", "Services"),
            "*Startup*.cs")
            .Where(path => Path.GetFileName(path) is
                "StartupCommandParser.cs" or
                "IStartupFileInspector.cs" or
                "WindowsStartupFileInspector.cs" or
                "IStartupSignatureInspector.cs" or
                "WinVerifyTrustStartupSignatureInspector.cs" or
                "StartupClassificationPolicy.cs" or
                "StartupIntelligenceService.cs")
            .ToArray();

        string source = string.Join("\n", files.Select(File.ReadAllText));
        string[] forbiddenTokens =
        [
            "Process.Start", "Process.Kill", "CreateProcess", "File.Move",
            "File.Delete", "File.Write", "CreateSubKey", ".SetValue(",
            ".DeleteValue(", "HttpClient", "WebClient", "System.Net.Sockets",
            "EvidenceService", "AddRangeAsync"
        ];

        foreach (string token in forbiddenTokens)
        {
            Assert.IsFalse(
                source.Contains(token, StringComparison.Ordinal),
                $"Phase B production source contains forbidden token '{token}'.");
        }
    }

    [TestMethod]
    public void NativeTrustOptionsAreUiFreeCacheOnlyAndNoAdditionalRevocation()
    {
        Type inspector = typeof(WinVerifyTrustStartupSignatureInspector);

        Assert.AreEqual((uint)2, ReadConstant(inspector, "WtdUiNone"));
        Assert.AreEqual((uint)0, ReadConstant(inspector, "WtdRevokeNone"));
        Assert.AreEqual((uint)0x1000, ReadConstant(inspector, "WtdCacheOnlyUrlRetrieval"));
    }

    private static uint ReadConstant(Type type, string name) =>
        (uint)(type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)?.GetRawConstantValue()
               ?? throw new AssertFailedException($"Constant {name} was not found."));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app.Tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new AssertFailedException("Repository root could not be located.");
    }
}
