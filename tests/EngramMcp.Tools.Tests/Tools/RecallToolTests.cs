using EngramMcp.Tools.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RecallToolTests
{
    [Fact]
    public async Task ExecuteAsync_returns_memories_from_service()
    {
        var memoryService = new ToolTestMemoryService
        {
            RecallResult = [new RecallMemory("id-1", "Remember this")]
        };
        var tool = new EngramMcp.Tools.Tools.Recall.McpTool(memoryService);

        var response = await tool.ExecuteAsync();

        response.Memories.Count.Is(1);
        response.Memories[0].Id.Is("id-1");
        response.Memories[0].Text.Is("Remember this");
    }
}
