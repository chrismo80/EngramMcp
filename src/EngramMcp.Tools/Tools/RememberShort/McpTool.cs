using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberShort;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_short", Title = "Remember Short")]
    [Description("Create a new short-term memory.")]
    public Task ExecuteAsync(
        [Description("The memory text.")]
        string text,
        CancellationToken cancellationToken = default) =>
        memoryService.RememberAsync(RetentionTier.Short, text, cancellationToken);
}
