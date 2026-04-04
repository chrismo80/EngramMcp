using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Storage;
using EngramMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RecallToolTests : ToolTests<RecallTool>
{
    [Fact]
    public async Task ExecuteAsync_returns_memories_from_service()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "id-1", Text = "Remember this", Retention = 10 }
            ]
        });

        var response = await Sut.ExecuteAsync();

        response.Memories.Count.Is(1);
        response.TotalCount.Is(1);
        response.SelectedCount.Is(1);
        response.Memories[0].Id.Is("id-1");
        response.Memories[0].Text.Is("Remember this");
    }

    [Fact]
    public async Task ExecuteAsync_caps_returned_memories_at_50_by_default()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories = Enumerable.Range(1, 101)
                .Select(index => new PersistedMemory { Id = $"id-{index}", Text = $"Memory {index}", Retention = 102 - index })
                .ToList()
        });

        var response = await Sut.ExecuteAsync();

        response.TotalCount.Is(101);
        response.SelectedCount.Is(50);
        response.Memories.Count.Is(50);
        response.Memories[0].Id.Is("id-1");
        response.Memories[49].Id.Is("id-50");
    }

    [Fact]
    public async Task ExecuteAsync_returns_requested_memory_count_when_max_count_is_provided()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories = Enumerable.Range(1, 101)
                .Select(index => new PersistedMemory { Id = $"id-{index}", Text = $"Memory {index}", Retention = 102 - index })
                .ToList()
        });

        var response = await Sut.ExecuteAsync(maxCount: 100);

        response.TotalCount.Is(101);
        response.SelectedCount.Is(100);
        response.Memories.Count.Is(100);
        response.Memories[0].Id.Is("id-1");
        response.Memories[99].Id.Is("id-100");
    }

    [Fact]
    public async Task ExecuteAsync_treats_zero_as_default()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories = Enumerable.Range(1, 80)
                .Select(index => new PersistedMemory { Id = $"id-{index}", Text = $"Memory {index}", Retention = 81 - index })
                .ToList()
        });

        var response = await Sut.ExecuteAsync(maxCount: 0);

        response.TotalCount.Is(80);
        response.SelectedCount.Is(50);
        response.Memories.Count.Is(50);
    }
}
