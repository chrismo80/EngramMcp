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
        var tool = new EngramMcp.Tools.Tools.RememberShort.McpTool(memoryService);

        await tool.ExecuteAsync("Remember this");

        memoryService.RememberedTier.Is(RetentionTier.Short);
        memoryService.RememberedText.Is("Remember this");
    }
}
