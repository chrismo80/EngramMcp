using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "store", Title = "Store Memory")]
    [Description("Store information in a memory section. Use built-in sections for the standard defaults or provide a custom section name to create one on first write.")]
    public Task ExecuteAsync(
        [Description("The memory section to store into.")]
        string section,
        [Description("The memory to store.")]
        string text,
        [Description("Optional normalized tags to store with this memory entry.")]
        IReadOnlyList<string>? tags = null,
        [Description("Optional importance level: low, normal, high. Defaults to normal.")]
        string? importance = null,
        CancellationToken cancellationToken = default)
    {
        return memoryService.StoreAsync(section, text, tags, importance.Parse(), cancellationToken);
    }
}