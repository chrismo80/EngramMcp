using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreMediumTermTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store_mediumterm", Title = "Store Medium-Term Memory")]
    [Description("Store information that is useful across sessions but may change over time. Use for evolving preferences, personal events, decisions made, lessons learned.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        [Description("Optional normalized tags to store with this memory entry.")]
        IReadOnlyList<string>? tags = null,
        [Description("Optional relative priority within the selected section: low, normal, high. Do not use high just because the memory is worth storing. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.MediumTerm, text, tags, importance.Parse(), cancellationToken);
    }
}
