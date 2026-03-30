using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.RememberLong;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "remember_long", Title = "Remember Long")]
    [Description("Store information expected to remain valid over long periods. Use for durable facts, stable constraints, or information with low expected change frequency.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken = default)
    {
        var validationError = MemoryText.GetValidationError(text);

        if (validationError is not null)
            return validationError;

        await memoryService.RememberAsync(RetentionTier.Long, text, cancellationToken).ConfigureAwait(false);
        return "Stored long-term memory.";
    }
}
