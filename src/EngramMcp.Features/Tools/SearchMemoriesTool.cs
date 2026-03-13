using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class SearchMemoriesTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "search_memories", Title = "Search Memories", ReadOnly = true, Idempotent = true)]
    [Description("Search memory entries across built-in and custom sections using a single query string.")]
    public async Task<string> ExecuteAsync(
        [Description("The text to search for in section names, tags, and memory entries.")]
        string query,
        CancellationToken cancellationToken)
    {
        var results = await memoryService.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        return results.ToMarkdown();
    }
}
