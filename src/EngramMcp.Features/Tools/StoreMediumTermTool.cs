using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreMediumTermTool(IMemoryService memoryService) : StoreMemoryToolBase(memoryService)
{
    private const string TargetMemoryName = "mediumTerm";

    [McpServerTool(Name = "store_mediumterm", Title = "Store Medium-Term Memory")]
    [Description("Stores a plain-text memory in the medium-term memory section.")]
    public Task ExecuteAsync(
        [Description("Plain-text memory content to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return MemoryService.StoreAsync(TargetMemoryName, text, cancellationToken);
    }
}
