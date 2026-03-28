using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class StoreLongTermToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_memory_in_long_term_section()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.StoreLongTerm.McpTool(memoryService);

        await tool.ExecuteAsync("Durable fact", "high");

        memoryService.StoredSection.Is(BuiltInMemorySections.LongTerm);
        memoryService.StoredText.Is("Durable fact");
        memoryService.StoredImportance.Is(MemoryImportance.High);
    }
}
