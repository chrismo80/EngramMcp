using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberShort;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_short", Title = "Remember Short")]
    [Description("Store session-level context that helps future continuation. Use for recent progress, temporary working context, intermediate conclusions, or resume points.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken = default)
    {
        var validationError = MemoryText.GetValidationError(text);

        if (validationError is not null)
            return validationError;

        await memoryService.RememberAsync(RetentionTier.Short, text, cancellationToken).ConfigureAwait(false);
        return "Stored short-term memory.";
    }
}
