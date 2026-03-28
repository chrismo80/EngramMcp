using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Store;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store", Title = "Store Memory")]
    [Description("Store information in a memory section. Use built-in sections for the standard defaults or provide a custom section name to create one on first write.")]
    public Task ExecuteAsync(
        [Description("The memory section to store into.")]
        string section,
        [Description("The memory to store.")]
        string text,
        [Description("Optional relative priority within the selected section: low, normal, high. Do not use high just because the memory is worth storing. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(section, text, importance.Parse(), cancellationToken);
    }
}
