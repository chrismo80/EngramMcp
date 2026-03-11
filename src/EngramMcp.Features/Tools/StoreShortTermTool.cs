using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreShortTermTool(IMemoryService memoryService) : StoreMemoryToolBase(memoryService)
{
    private const string TargetMemoryName = "shortTerm";

    [McpServerTool(Name = "store_shortterm", Title = "Store Short-Term Memory")]
    [Description("Stores a plain-text memory in the short-term memory section.")]
    public Task ExecuteAsync(
        [Description("Plain-text memory content to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        // TODO(code-monkey): Delegate to the shared memory service using TargetMemoryName.
        throw new NotImplementedException();
    }
}
