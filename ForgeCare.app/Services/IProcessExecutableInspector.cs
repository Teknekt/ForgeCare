using System.Threading;
using System.Threading.Tasks;
using ForgeCare.App.Models;

namespace ForgeCare.App.Services;

public interface IProcessExecutableInspector
{
    Task<ProcessExecutableInspection> InspectAsync(
        string canonicalExecutablePath,
        CancellationToken cancellationToken = default);
}
