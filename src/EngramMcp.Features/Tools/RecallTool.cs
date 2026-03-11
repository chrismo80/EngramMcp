using System.ComponentModel;
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

        return string.Join(
            Environment.NewLine + Environment.NewLine,
            FormatSection("Short-Term", GetEntries(document, "shortTerm")),
            FormatSection("Medium-Term", GetEntries(document, "mediumTerm")),
            FormatSection("Long-Term", GetEntries(document, "longTerm")));
    }

    private static IReadOnlyList<MemoryEntry> GetEntries(MemoryDocument document, string memoryName)
    {
        return document.Memories.TryGetValue(memoryName, out var entries)
            ? entries
            : [];
    }

    private static string FormatSection(string title, IReadOnlyList<MemoryEntry> entries)
    {
        var lines = new List<string> { $"# {title}" };

        if (entries.Count == 0)
        {
            lines.Add("- No entries");
            return string.Join(Environment.NewLine, lines);
        }

        lines.AddRange(entries.Select(entry => $"- {entry.Text}"));
        return string.Join(Environment.NewLine, lines);
    }
}
