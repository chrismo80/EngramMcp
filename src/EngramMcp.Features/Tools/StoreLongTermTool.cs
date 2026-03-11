using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : Tool
{
    private const string TargetMemoryName = "long-term";

    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Use this tool when you learn a stable personal fact about the human or yourself - such as name, preferred language, vibe, preferences, or interaction style - that is unlikely to change and worth remembering forever.")]
    public Task ExecuteAsync(
        [Description("The memory to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        return memoryService.StoreAsync(TargetMemoryName, text, cancellationToken);
    }
}
