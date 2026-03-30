using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RecallToolTests : ToolTests<RecallTool>
{
    [Fact]
    public async Task ExecuteAsync_returns_memories_from_service()
    {
        MemoryService.RecallResult = [new RecallMemory("id-1", "Remember this")];

        var response = await Sut.ExecuteAsync();

        response.ReturnedCount.Is(1);
        response.TotalCount.Is(1);
        response.Memories.Count.Is(1);
        response.Memories[0].Id.Is("id-1");
        response.Memories[0].Text.Is("Remember this");
    }

    [Fact]
    public async Task ExecuteAsync_caps_returned_memories_at_100()
    {
        MemoryService.RecallResult = Enumerable.Range(1, 101)
            .Select(index => new RecallMemory($"id-{index}", $"Memory {index}"))
            .ToArray();

        var response = await Sut.ExecuteAsync();

        response.ReturnedCount.Is(100);
        response.TotalCount.Is(101);
        response.Memories.Count.Is(100);
        response.Memories[0].Id.Is("id-1");
        response.Memories[99].Id.Is("id-100");
    }
}
