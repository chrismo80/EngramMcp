using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberMedium;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_medium", Title = "Remember Medium")]
    [Description("Create a new medium-term memory.")]
    public Task ExecuteAsync(
        [Description("The memory text.")]
        string text,
        CancellationToken cancellationToken = default) =>
        memoryService.RememberAsync(RetentionTier.Medium, text, cancellationToken);
}
