using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IEvidenceCollector<in TDiagnosticResult>
{
    EvidenceCollectionResult Collect(
        TDiagnosticResult result,
        string sessionId);
}
