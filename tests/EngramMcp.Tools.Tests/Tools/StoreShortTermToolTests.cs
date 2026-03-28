using EngramMcp.Tools.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class StoreShortTermToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_memory_in_short_term_section()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.StoreShortTerm.McpTool(memoryService);

        await tool.ExecuteAsync("Recent progress", "low");

        memoryService.StoredSection.Is(BuiltInMemorySections.ShortTerm);
        memoryService.StoredText.Is("Recent progress");
        memoryService.StoredImportance.Is(MemoryImportance.Low);
    }
}
