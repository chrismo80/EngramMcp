using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RecallToolTests
{
    [Fact]
    public async Task ExecuteAsync_returns_visible_memories_and_custom_sections()
    {
        var memoryService = new ToolTestMemoryService
        {
            RecallResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [BuiltInMemorySections.LongTerm] = [new MemoryEntry(DateTime.Now, "Durable fact", MemoryImportance.High)],
                    [BuiltInMemorySections.MediumTerm] = [],
                    [BuiltInMemorySections.ShortTerm] = []
                },
                CustomSections = [new MemorySectionSummary("project-x", 2)]
            }
        };
        var tool = new EngramMcp.Tools.Tools.Recall.McpTool(memoryService);

        var response = await tool.ExecuteAsync(CancellationToken.None);

        response.Memories[BuiltInMemorySections.LongTerm].Count.Is(1);
        response.Memories[BuiltInMemorySections.LongTerm][0].Text.Is("Durable fact");
        response.Memories[BuiltInMemorySections.LongTerm][0].Importance.Is("high");
        response.CustomSections.IsNotNull();
        response.CustomSections![0].Name.Is("project-x");
        response.CustomSections[0].EntryCount.Is(2);
    }
}
