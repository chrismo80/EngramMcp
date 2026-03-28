using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class StoreToolTests
{
    [Fact]
    public async Task ExecuteAsync_stores_memory_in_requested_section()
    {
        var memoryService = new ToolTestMemoryService();
        var tool = new EngramMcp.Tools.Tools.Store.McpTool(memoryService);

        await tool.ExecuteAsync("project-x", "Remember this", "high");

        memoryService.StoredSection.Is("project-x");
        memoryService.StoredText.Is("Remember this");
        memoryService.StoredImportance.Is(MemoryImportance.High);
    }
}
