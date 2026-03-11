using System.ComponentModel;
using System.Text;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class RecallTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "recall", Title = "Recall Memories", ReadOnly = true, Idempotent = true)]
    [Description("Loads all configured memory sections and returns them as markdown.")]
    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var document = await memoryService.RecallAsync(cancellationToken).ConfigureAwait(false);

        return FormatSection(document);
    }
    
    private static string FormatSection(MemoryDocument document)
    {
        var sb = new StringBuilder("# Memory").AppendLine();

        foreach (var block in document.Memories.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine().AppendLine($"## {block.Key}");
            
            foreach(var memory in block.Value)
                sb.AppendLine($"- {memory.Text}");
        }

        return sb.ToString();
    }
}
