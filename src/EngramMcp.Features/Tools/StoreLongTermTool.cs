using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class StoreLongTermTool(IMemoryService memoryService) : StoreMemoryToolBase(memoryService)
{
    private const string TargetMemoryName = "longTerm";

    [McpServerTool(Name = "store_longterm", Title = "Store Long-Term Memory")]
    [Description("Stores a plain-text memory in the long-term memory section.")]
    public Task ExecuteAsync(
        [Description("Plain-text memory content to store.")]
        string text,
        CancellationToken cancellationToken)
    {
        // TODO(code-monkey): Delegate to the shared memory service using TargetMemoryName.
        throw new NotImplementedException();
    }
}
