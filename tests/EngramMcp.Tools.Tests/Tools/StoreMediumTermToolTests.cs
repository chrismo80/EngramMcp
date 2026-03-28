using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class StoreMediumTermToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_memory_in_medium_term_section()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.StoreMediumTerm.McpTool(memoryService);

        await tool.ExecuteAsync("Useful context", "normal");

        memoryService.StoredSection.Is(BuiltInMemorySections.MediumTerm);
        memoryService.StoredText.Is("Useful context");
        memoryService.StoredImportance.Is(MemoryImportance.Normal);
    }
}
