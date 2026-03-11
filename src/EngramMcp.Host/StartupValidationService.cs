using EngramMcp.Core.Abstractions;
using Microsoft.Extensions.Hosting;

namespace EngramMcp.Host;

public sealed class StartupValidationService(IMemoryFileStore memoryFileStore) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await memoryFileStore.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        _ = await memoryFileStore.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
