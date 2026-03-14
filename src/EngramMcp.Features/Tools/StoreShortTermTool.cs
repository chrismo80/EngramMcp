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
        [Description("Optional normalized tags to store with this memory entry.")]
        IReadOnlyList<string>? tags = null,
        [Description("Optional importance level for this memory entry. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.ShortTerm, text, tags, MemoryImportanceToolParser.ParseOrDefault(importance), cancellationToken);
    }
}
