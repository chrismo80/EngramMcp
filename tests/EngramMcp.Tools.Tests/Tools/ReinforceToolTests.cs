using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class ReinforceToolTests
{
    private sealed class RejectingMemoryService(string errorMessage) : IMemoryService
    {
        public Task<IReadOnlyList<RecallMemory>> RecallAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecallMemory>>([]);

        public Task<MemoryChangeResult> RememberAsync(RetentionTier retentionTier, string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(MemoryChangeResult.Success());

        public Task<MemoryChangeResult> ReinforceAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(MemoryChangeResult.Reject(errorMessage));
    }

    [Fact]
    public async Task ExecuteAsync_reinforces_requested_memories()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.Reinforce.ReinforceTool(memoryService);

        var response = await tool.ExecuteAsync(["id-1", "id-2"]);

        response.Is("Reinforced memories.");
        memoryService.ReinforcedMemoryIds.IsNotNull();
        memoryService.ReinforcedMemoryIds!.Count.Is(2);
        memoryService.ReinforcedMemoryIds[0].Is("id-1");
        memoryService.ReinforcedMemoryIds[1].Is("id-2");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_for_invalid_input()
    {
        var tool = new EngramMcp.Tools.Tools.Reinforce.ReinforceTool(new RejectingMemoryService("At least one memory id is required."));

        var response = await tool.ExecuteAsync([]);

        response.Is("At least one memory id is required.");
    }
}
