using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Store information expected to remain valid over long periods. Use for durable facts, stable constraints, or information with low expected change frequency.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        [Description("Optional normalized tags to store with this memory entry.")]
        IReadOnlyList<string>? tags = null,
        [Description("Optional importance level: low, normal, high. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.LongTerm, text, tags, importance.Parse(), cancellationToken);
    }
}