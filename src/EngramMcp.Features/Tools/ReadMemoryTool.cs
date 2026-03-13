using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class ReadMemoryTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "read_memory", Title = "Read Memory", ReadOnly = true, Idempotent = true)]
    [Description("Read a single memory section by name. Works for built-in and custom sections.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory section to read.")]
        string section,
        CancellationToken cancellationToken)
    {
        var document = await memoryService.ReadAsync(section, cancellationToken).ConfigureAwait(false);

        return document.ToMarkdown();
    }
}
