using System.Reflection;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class StartupIntelligenceEvidenceSafetyTests
{
    [TestMethod]
    public void PhaseCProductionTypesExposeOnlyPureProjectionDependencies()
    {
        Type[] phaseCTypes =
        [
            typeof(StartupIntelligenceEvidenceAdapter),
            typeof(StartupEvidencePathNormalizer),
            typeof(StartupEvidenceCorrelationKeyBuilder)
        ];
        string[] forbidden =
        [
            "StartupScanner", "SystemScanner", "StartupManagerService",
            "StartupImpactService", "StartupIntelligenceService",
            "IStartupFileInspector", "IStartupSignatureInspector",
            "StartupCommandParser", "Registry", "Process", "ServiceController",
            "ForgeReportService", "JsonEvidenceRepository", "EvidenceService",
            "MainWindow", "CleanupExecutor", "ControlledInstallerHandoffService"
        ];

        foreach (Type type in phaseCTypes)
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
    public void PhaseCSourceContainsNoInspectionMutationExecutionOrNetworkCalls()
    {
        string root = FindRepositoryRoot();
        string[] files =
        [
            Path.Combine(root, "ForgeCare.app", "Services", "StartupIntelligenceEvidenceAdapter.cs"),
            Path.Combine(root, "ForgeCare.app", "Services", "StartupEvidencePathNormalizer.cs"),
            Path.Combine(root, "ForgeCare.app", "Services", "StartupEvidenceCorrelationKeyBuilder.cs")
        ];
        string source = string.Join("\n", files.Select(File.ReadAllText));
        string[] forbiddenTokens =
        [
            "StartupScanner", "SystemScanner", "StartupManagerService",
            "StartupImpactService", "StartupIntelligenceService",
            "WindowsStartupFileInspector", "WinVerifyTrustStartupSignatureInspector",
            "StartupCommandParser", "Microsoft.Win32", "Process.Start", "Process.Kill",
            "File.Move", "File.Delete", "File.Write", "HttpClient", "WebClient",
            "EvidenceService", "JsonEvidenceRepository", "ForgeReportService"
        ];

        foreach (string token in forbiddenTokens)
        {
            Assert.IsFalse(
                source.Contains(token, StringComparison.Ordinal),
                $"Phase C production source contains forbidden token '{token}'.");
        }
    }

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
