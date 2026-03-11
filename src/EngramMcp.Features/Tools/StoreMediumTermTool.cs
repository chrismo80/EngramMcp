using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreMediumTermTool(IMemoryService memoryService) : Tool
{
    private const string MemoryName = "medium-term";

    [McpServerTool(Name = "store_mediumterm", Title = "Store Medium-Term Memory")]
    [Description("Use this tool when you learn something that will likely be useful in future tasks or conversations but may change over time, and is therefore worth keeping beyond the current session without treating it as permanent.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(MemoryName, text, cancellationToken);
    }
}
