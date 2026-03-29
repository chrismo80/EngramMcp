using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberLong;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_long", Title = "Remember Long")]
    [Description("Create a new long-term memory.")]
    public Task ExecuteAsync(
        [Description("The memory text.")]
        string text,
        CancellationToken cancellationToken = default) =>
        memoryService.RememberAsync(RetentionTier.Long, text, cancellationToken);
}
