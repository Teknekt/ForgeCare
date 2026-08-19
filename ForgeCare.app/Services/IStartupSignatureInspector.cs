using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IStartupSignatureInspector
{
    Task<StartupSignatureInfo> InspectAsync(
        string resolvedPath,
        CancellationToken cancellationToken = default);
}
