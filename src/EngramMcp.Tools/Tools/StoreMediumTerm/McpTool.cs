using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.StoreMediumTerm;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store_mediumterm", Title = "Store Medium-Term Memory")]
    [Description("Store information that is useful across sessions but may change over time. Use for evolving preferences, personal events, decisions made, lessons learned.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        [Description("Optional relative priority within the selected section: low, normal, high. Do not use high just because the memory is worth storing. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.MediumTerm, text, importance.Parse(), cancellationToken);
    }
}
