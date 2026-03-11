using System.ComponentModel;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreShortTermTool(IMemoryService memoryService) : Tool
{
    private const string MemoryName = "short-term";

    [McpServerTool(Name = "store_shortterm", Title = "Store Short-Term Memory")]
    [Description("Use this tool when you finish work for the human and want to record the recent working state for fast next-session resume, including completed tasks, milestones, touched files, and the active area of the workspace.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(MemoryName, text, cancellationToken);
    }
}
