using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools;

public sealed class RememberMediumTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_medium", Title = "Remember Medium")]
    [Description("Store information that is useful across sessions but may change over time. Use for evolving preferences, personal events, decisions made, lessons learned.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken = default)
    {
        var result = await memoryService.RememberAsync(RetentionTier.Medium, text, cancellationToken).ConfigureAwait(false);
        return result.Rejection ?? "Stored medium-term memory.";
    }
}
