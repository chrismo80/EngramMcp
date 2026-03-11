using EngramMcp.Core.Abstractions;
using Microsoft.Extensions.Hosting;

namespace EngramMcp.Host;

public sealed class StartupValidationService(IMemoryFileStore memoryFileStore) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // TODO(code-monkey): Trigger startup-time initialization and validation of the configured memory file
        // so malformed JSON or invalid paths fail before the server starts accepting tool calls.
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
