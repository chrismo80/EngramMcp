using System.ComponentModel;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tools.Recall;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.ReadSection;

public sealed class McpTool(IMemoryService memoryService) : Tool
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

            return new ReadSectionResponse
            {
                Memories = RecallResponseFactory.ToVisibleMemories(document.Memories)
            };
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
