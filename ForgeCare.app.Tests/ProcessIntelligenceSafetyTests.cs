using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class ProcessIntelligenceSafetyTests
{
    private static readonly Type[] ProductionTypes =
    [
        typeof(ProcessInstanceObservation), typeof(ProcessExecutableInspection),
        typeof(ProcessIntelligenceEntry), typeof(ProcessApplicationGroup),
        typeof(ProcessIntelligenceResult), typeof(IProcessExecutableInspector),
        typeof(WindowsProcessExecutableInspector), typeof(ProcessClassificationPolicy),
        typeof(ProcessIntelligenceService)
    ];

    [TestMethod]
    public void ModelsExposeNoCommandAccountProcessOrMetadataEscapeHatch()
    {
        string[] forbiddenNames = ["CommandLine", "Arguments", "UserName", "Owner", "Sid", "Metadata"];
        foreach (Type type in ProductionTypes.Where(type => type.Namespace == "ForgeCare.App.Models"))
        {
            string[] properties = type.GetProperties().Select(property => property.Name).ToArray();
            foreach (string forbidden in forbiddenNames)
                Assert.DoesNotContain(forbidden, properties);
        }

        Assert.IsFalse(ProductionTypes.SelectMany(type => type.GetProperties())
            .Any(property => property.PropertyType.FullName == "System.Diagnostics.Process"));
    }

    [TestMethod]
    public void FoundationHasNoEvidenceLiveAnalyzerUiOrPersistenceDependencies()
    {
        string[] forbidden =
        [
            "EvidenceService", "IEvidenceRepository", "JsonEvidenceRepository", "EvidenceRecord",
            "ResourceAnalyzerService", "ResourceAnalysisResult", "SystemScanner", "MainWindow"
        ];

        foreach (Type type in ProductionTypes)
        {
            IEnumerable<Type> dependencies = type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(field => field.FieldType));
            foreach (Type dependency in dependencies)
                Assert.IsFalse(forbidden.Any(name => dependency.FullName?.Contains(name, StringComparison.Ordinal) == true));
        }
    }

    [TestMethod]
    public void NewProductionSourceContainsNoLiveProcessMutationPersistenceOrNetworkCalls()
    {
        string root = FindRepositoryRoot();
        string[] files = Directory.GetFiles(Path.Combine(root, "ForgeCare.app"), "*Process*.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) is
                "ProcessInstanceObservation.cs" or "ProcessExecutableInspection.cs" or
                "ProcessIdentityClassification.cs" or "ProcessIntelligenceEntry.cs" or
                "ProcessApplicationGroup.cs" or "ProcessIntelligenceResult.cs" or
                "IProcessExecutableInspector.cs" or "WindowsProcessExecutableInspector.cs" or
                "ProcessClassificationPolicy.cs" or "ProcessIntelligenceService.cs")
            .ToArray();
        string source = string.Join("\n", files.Select(File.ReadAllText));
        string[] forbiddenTokens =
        [
            "Process.GetProcesses", "Process.GetProcessById", "Process.Start", "Process.Kill",
            "Process.MainModule", "Process.StartTime", "TerminateProcess", "SuspendThread",
            "NtSuspendProcess", "PriorityClass =", "ProcessorAffinity =", "CreateSubKey",
            ".SetValue(", ".DeleteValue(", "File.Move", "File.Delete", "File.Write",
            "ServiceController", "ControlledInstallerHandoffService", "EvidenceService",
            "IEvidenceRepository", "HttpClient", "WebClient"
        ];

        foreach (string token in forbiddenTokens)
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
