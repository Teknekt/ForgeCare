using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IStartupFileInspector
{
    Task<StartupFileInspection> InspectAsync(
        string resolvedPath,
        CancellationToken cancellationToken = default);
}
