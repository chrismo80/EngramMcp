using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : Tool
{
    private const string MemoryName = "long-term";

    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Store this when you reach a meaningful checkpoint – a completed task or a clear point to resume from. What did we just do, and where do we continue?")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(MemoryName, text, cancellationToken);
    }
}
