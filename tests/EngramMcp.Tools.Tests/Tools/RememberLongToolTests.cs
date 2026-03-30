using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RememberLongToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_long_term_memory()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.RememberLong.RememberLongTool(memoryService);

        var response = await tool.ExecuteAsync("Remember this");

        response.Is("Stored long-term memory.");
        memoryService.RememberedTier.Is(RetentionTier.Long);
        memoryService.RememberedText.Is("Remember this");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_from_memory_service()
    {
        var memoryService = new ToolTestMemoryService
        {
            RememberResult = MemoryChangeResult.Reject("Memory text must not be null, empty, or whitespace.")
        };
        var tool = new EngramMcp.Tools.Tools.RememberLong.RememberLongTool(memoryService);

        var response = await tool.ExecuteAsync("");

        response.Is("Memory text must not be null, empty, or whitespace.");
        memoryService.RememberedText.Is("");
    }
}
