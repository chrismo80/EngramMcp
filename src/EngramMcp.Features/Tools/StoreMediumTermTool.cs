using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreMediumTermTool(IMemoryService memoryService) : Tool
{
    private const string MemoryName = "medium-term";

    [McpServerTool(Name = "store_mediumterm", Title = "Store Medium-Term Memory")]
    [Description("Store this when you learn something about the user that would help in any future session: preferences, hobbies, working style, favorite tools, or anything personally meaningful. These facts may change over time.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(MemoryName, text, cancellationToken);
    }
}
