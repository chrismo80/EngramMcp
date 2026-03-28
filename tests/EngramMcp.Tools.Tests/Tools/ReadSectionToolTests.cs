using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class ReadSectionToolTests
{
    [Fact]
    public async Task ExecuteAsync_returns_visible_memories_for_requested_section()
    {
        var memoryService = new ToolTestMemoryService
        {
            ReadResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    ["project-x"] = [new MemoryEntry(DateTime.Now, "Important detail", MemoryImportance.High)]
                }
            }
        };
        var tool = new EngramMcp.Tools.Tools.ReadSection.McpTool(memoryService);

        var response = await tool.ExecuteAsync("project-x", CancellationToken.None);

        response.Memories["project-x"].Count.Is(1);
        response.Memories["project-x"][0].Text.Is("Important detail");
        response.Memories["project-x"][0].Importance.Is("high");
    }
}
