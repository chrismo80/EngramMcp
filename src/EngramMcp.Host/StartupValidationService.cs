using EngramMcp.Core.Abstractions;
using Microsoft.Extensions.Hosting;

namespace EngramMcp.Host;

public sealed class StartupValidationService(IMemoryStore memoryStore) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await memoryStore.EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        _ = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
