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
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(BuiltInMemorySections.MediumTerm, text, cancellationToken);
    }
}
