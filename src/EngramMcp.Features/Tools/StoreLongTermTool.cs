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
        [Description("Optional relative priority within the selected section: low, normal, high. Do not use high just because the memory is worth storing. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.LongTerm, text, importance.Parse(), cancellationToken);
    }
}
