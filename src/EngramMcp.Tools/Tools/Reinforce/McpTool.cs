using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Reinforce;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "reinforce", Title = "Reinforce Memories")]
    [Description("Strengthen recalled memories that materially influenced your work in the current session. Do not reinforce memories merely because they were present.")]
    public async Task<string> ExecuteAsync(
        [Description("The memory ids to reinforce.")]
        IReadOnlyList<string> memoryIds,
        CancellationToken cancellationToken = default)
    {
        var errorMessage = await memoryService.ReinforceAsync(memoryIds, cancellationToken).ConfigureAwait(false);
        return errorMessage ?? "Reinforced memories.";
    }
}
