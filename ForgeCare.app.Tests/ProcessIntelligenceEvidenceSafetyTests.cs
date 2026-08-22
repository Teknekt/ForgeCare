using System.Reflection;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceEvidenceSafetyTests
{
    private static readonly Type[] ProductionTypes =
    [
        typeof(ProcessIntelligenceEvidenceAdapter),
        typeof(ProcessEvidencePathProjector),
        typeof(ProcessEvidenceCorrelationKeyBuilder)
    ];

    [TestMethod]
    public void AdapterHasNoPersistenceAnalyzerInspectionOrLiveProcessDependencies()
    {
        string[] forbidden =
        [
            "JsonEvidenceRepository", "IEvidenceRepository", "EvidenceService", "ForgeReportService",
            "ResourceAnalyzerService", "ProcessIntelligenceService", "IProcessExecutableInspector",
            "WindowsProcessExecutableInspector", "IStartupFileInspector", "IStartupSignatureInspector"
        ];
        foreach (Type type in ProductionTypes)
        {
            IEnumerable<Type> dependencies = type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(field => field.FieldType));
            foreach (Type dependency in dependencies)
                Assert.IsFalse(forbidden.Any(name => dependency.FullName?.Contains(name, StringComparison.Ordinal) == true));
        }
    }

    [TestMethod]
    public void PhaseCSourceContainsNoInspectionMutationNetworkOrPersistenceCalls()
    {
        string root = FindRepositoryRoot();
        string[] names =
        [
            "ProcessIntelligenceEvidenceAdapter.cs", "ProcessEvidencePathProjector.cs",
            "ProcessEvidenceCorrelationKeyBuilder.cs"
        ];
        string source = string.Join('\n', names.Select(name => File.ReadAllText(Path.Combine(root, "ForgeCare.app", "Services", name))));
        string[] forbidden =
        [
            "Process.GetProcesses", "Process.GetProcessById", "Process.Start", "Process.Kill",
            "ResourceAnalyzerService", "ProcessIntelligenceService", "IProcessExecutableInspector",
            "WindowsProcessExecutableInspector", "FileVersionInfo", "WinVerifyTrust",
            "StartupScanner", "StartupManagerService", "CreateSubKey", ".SetValue(",
            "File.Move", "File.Delete", "File.Write", "ServiceController",
            "ControlledInstallerHandoffService", "HttpClient", "WebClient", "JsonEvidenceRepository",
            "IEvidenceRepository", "EvidenceService", "ForgeReportService"
        ];
        foreach (string token in forbidden)
            Assert.IsFalse(source.Contains(token, StringComparison.Ordinal), $"Forbidden token: {token}");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app")) &&
                Directory.Exists(Path.Combine(directory.FullName, "ForgeCare.app.Tests"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new AssertFailedException("Repository root could not be located.");
    }
}
