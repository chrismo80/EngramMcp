using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreShortTermTool(IMemoryService memoryService) : StoreMemoryToolBase(memoryService)
{
    private const string TargetMemoryName = "short-term";

    [McpServerTool(Name = "store_shortterm", Title = "Store Short-Term Memory")]
    [Description("Stores a plain-text memory in the short-term memory section.")]
    public Task ExecuteAsync(
        [Description("Plain-text memory content to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return MemoryService.StoreAsync(TargetMemoryName, text, cancellationToken);
    }
}
