using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class ReadSectionTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "read_section", Title = "Read Section", ReadOnly = true, Idempotent = true)]
    [Description("Read a single memory section by name. Works for built-in and custom sections using case-insensitive section lookup.")]
    public async Task<ReadSectionResponse> ExecuteAsync(
        [Description("The memory section to read.")]
        string section,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await memoryService.ReadAsync(section, cancellationToken).ConfigureAwait(false);

            return document.ToReadSectionResponse();
        }
        catch (KeyNotFoundException)
        {
            return new ReadSectionResponse
            {
                Memories = new Dictionary<string, IReadOnlyList<MemoryVisibleItemResponse>>(StringComparer.Ordinal)
                {
                    [section] = []
                }
            };
        }
    }
}
