using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreMemoryTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store_memory", Title = "Store Memory")]
    [Description("Store information in a custom memory section. Use built-in sections for the standard defaults or provide a custom section name to create one on first write.")]
    public Task ExecuteAsync(
        [Description("The memory section to store into.")]
        string section,
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(section, text, cancellationToken);
    }
}
