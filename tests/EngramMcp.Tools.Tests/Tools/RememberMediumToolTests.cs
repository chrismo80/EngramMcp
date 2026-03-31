using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RememberMediumToolTests : ToolTests<RememberMediumTool>
{
    [Fact]
    public async Task ExecuteAsync_stores_medium_term_memory()
    {
        var response = await Sut.ExecuteAsync("Remember this");

        response.Is("Stored medium-term memory.");
        MemoryService.RememberedTier.Is(RetentionTier.Medium);
        MemoryService.RememberedText.Is("Remember this");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_from_memory_service()
    {
        MemoryService.RememberResult = MemoryChangeResult.Reject("Memory text must not be null, empty, or whitespace.");

        var response = await Sut.ExecuteAsync("");

        response.Is("Memory text must not be null, empty, or whitespace.");
        MemoryService.RememberedText.Is("");
    }
}
