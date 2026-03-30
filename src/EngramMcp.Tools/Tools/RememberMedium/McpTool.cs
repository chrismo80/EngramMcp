using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberMedium;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_medium", Title = "Remember Medium")]
    [Description("Store information that is useful across sessions but may change over time. Use for evolving preferences, personal events, decisions made, lessons learned.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken = default)
    {
        var validationError = MemoryText.GetValidationError(text);

        if (validationError is not null)
            return validationError;

        await memoryService.RememberAsync(RetentionTier.Medium, text, cancellationToken).ConfigureAwait(false);
        return "Stored medium-term memory.";
    }
}
