using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Retention;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RememberShortToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_short_term_memory()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.RememberShort.RememberShortTool(memoryService);

        var response = await tool.ExecuteAsync("Remember this");

        response.Is("Stored short-term memory.");
        memoryService.RememberedTier.Is(RetentionTier.Short);
        memoryService.RememberedText.Is("Remember this");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_for_invalid_text()
    {
        var memoryService = new ToolTestMemoryService
        {
            RememberResult = MemoryChangeResult.Reject("Memory text must not be null, empty, or whitespace.")
        };
        var tool = new EngramMcp.Tools.Tools.RememberShort.RememberShortTool(memoryService);

        var response = await tool.ExecuteAsync("");

        response.Is("Memory text must not be null, empty, or whitespace.");
        memoryService.RememberedText.Is("");
    }
}
