using System.ComponentModel;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Reinforce;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "reinforce", Title = "Reinforce Memories")]
    [Description("Reinforce existing memories that proved useful again.")]
    public Task ExecuteAsync(
        [Description("The memory ids to reinforce.")]
        IReadOnlyList<string> memoryIds,
        CancellationToken cancellationToken = default) =>
        memoryService.ReinforceAsync(memoryIds, cancellationToken);
}
