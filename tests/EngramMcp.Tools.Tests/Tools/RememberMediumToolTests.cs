using EngramMcp.Tools.Memory.Retention;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class RememberMediumToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_medium_term_memory()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.RememberMedium.McpTool(memoryService);

        await tool.ExecuteAsync("Remember this");

        memoryService.RememberedTier.Is(RetentionTier.Medium);
        memoryService.RememberedText.Is("Remember this");
    }
}
