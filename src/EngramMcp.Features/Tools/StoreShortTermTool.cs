using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreShortTermTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store_shortterm", Title = "Store Short-Term Memory")]
    [Description("Store session-level context that helps future continuation. Use for recent progress, temporary working context, intermediate conclusions, or resume points.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        [Description("Optional relative priority within the selected section: low, normal, high. Do not use high just because the memory is worth storing. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.ShortTerm, text, importance.Parse(), cancellationToken);
    }
}
