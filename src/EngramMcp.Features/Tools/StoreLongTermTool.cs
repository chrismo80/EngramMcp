using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : Tool
{
    private const string MemoryName = "long-term";

    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Store this when you learn something that defines who the user or you fundamentally are: name, identity, character, values, and vibe. The soul of the relationship. These facts rarely change.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(MemoryName, text, cancellationToken);
    }
}
