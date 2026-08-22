using System.Reflection;
using ForgeCare.App.Models;
using ForgeCare.App.Services;
using ForgeCare.App.Views;
using ForgeCare.App.ViewModels;

namespace ForgeCare.App.Tests;

[TestClass]
public sealed class EvidenceSafetyTests
{
    private static readonly string[] ForbiddenTypeNames =
    {
        "CleanupExecutor",
        "StartupManagerService",
        "StorageCleanupService",
        "ServiceController",
        "Process",
        "Registry",
        "ControlledInstallerHandoffService"
    };

    [TestMethod]
    public void PhaseAEvidenceTypesExposeNoSystemChangingDependencies()
    {
        Type[] phaseATypes =
        {
            typeof(EvidenceService),
            typeof(JsonEvidenceRepository),
            typeof(SystemScanEvidenceAdapter),
            typeof(DeepAnalysisEvidenceAdapter)
        };

        foreach (Type type in phaseATypes)
        {
            IEnumerable<Type> exposedTypes = type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(field => field.FieldType))
                .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(method => method.ReturnType));

            foreach (Type exposedType in exposedTypes)
            {
                Assert.IsFalse(
                    ForbiddenTypeNames.Any(name => exposedType.FullName?.Contains(name, StringComparison.Ordinal) == true),
                    $"{type.Name} exposes forbidden dependency {exposedType.FullName}.");
            }
        }
    }

    [TestMethod]
    public void SystemScanAdapterOnlyImplementsPureCollectorContract()
    {
        Type adapter = typeof(SystemScanEvidenceAdapter);
        string[] declaredMethods = adapter
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { "Collect" }, declaredMethods);
    }

    [TestMethod]
    public void DeepAnalysisAdapterOnlyExposesTranslationAndSeverityMapping()
    {
        Type adapter = typeof(DeepAnalysisEvidenceAdapter);
        string[] declaredMethods = adapter
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[] { "Collect", "MapSeverity" },
            declaredMethods);
    }

    [TestMethod]
    public void ExplorerPresentationExposesNoDiagnosticOrSystemChangingDependencies()
    {
        Type[] explorerTypes =
        {
            typeof(EvidenceExplorerViewModel),
            typeof(EvidenceExplorerItem),
            typeof(EvidenceExplorerFacet),
            typeof(EvidenceExplorerMetadataItem),
            typeof(EvidenceDisplayFormatter),
            typeof(EvidenceExplorerView)
        };

        string[] explorerForbiddenTypes = ForbiddenTypeNames
            .Concat(new[]
            {
                "SystemScanner",
                "ResourceAnalyzerService",
                "SystemScanEvidenceAdapter",
                "DeepAnalysisEvidenceAdapter"
            })
            .ToArray();

        foreach (Type type in explorerTypes)
        {
            IEnumerable<Type> dependencies = type
                .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(field => field.FieldType));

            foreach (Type dependency in dependencies)
            {
                Assert.IsFalse(
                    explorerForbiddenTypes.Any(name => dependency.FullName?.Contains(name, StringComparison.Ordinal) == true),
                    $"{type.Name} references forbidden dependency {dependency.FullName}.");
            }
        }
    }

    [TestMethod]
    public async Task ExplorerViewModelUsesOnlySessionReadMethod()
    {
        string sessionId = Guid.NewGuid().ToString("N");
        var repository = new EvidenceExplorerTestRepository();
        var viewModel = new EvidenceExplorerViewModel(repository, (_, _) => { });

        await viewModel.LoadSessionAsync(sessionId);
        await viewModel.RefreshAsync(sessionId);

        Assert.AreEqual(0, repository.AddCalls);
        Assert.AreEqual(0, repository.AddRangeCalls);
    }
}
