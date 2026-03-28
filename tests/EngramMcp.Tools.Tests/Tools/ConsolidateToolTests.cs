using EngramMcp.Tools.Maintenance;
using EngramMcp.Tools.Tests.TestDoubles;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class ConsolidateToolTests
{
    [Fact]
    public async Task ExecuteAsync_read_mode_returns_section_entries_and_token()
    {
        var memoryService = new ToolTestMemoryService
        {
            MaintenanceReadResult = new MaintenanceSectionReadResult
            {
                Section = "project-x",
                Entries = [new MaintenanceMemoryEntry { Timestamp = "2026-03-28T10:15:30.0000000+01:00", Text = "Keep this", Importance = "high" }],
                ConsolidationToken = "token-42"
            }
        };
        var tool = new EngramMcp.Tools.Tools.Consolidate.McpTool(memoryService);

        var response = await tool.ExecuteAsync("read", "project-x");

        response.Section.Is("project-x");
        response.ConsolidationToken.Is("token-42");
        response.Entries.IsNotNull();
        response.Entries![0].Text.Is("Keep this");
        response.Failure.IsNull();
    }

    [Fact]
    public async Task ExecuteAsync_write_mode_returns_written_entries()
    {
        var memoryService = new ToolTestMemoryService
        {
            MaintenanceWriteResult = new MaintenanceSectionWriteResult
            {
                Section = "project-x",
                Entries = [new MaintenanceMemoryEntry { Timestamp = "2026-03-28T10:15:30.0000000+01:00", Text = "Consolidated", Importance = null }]
            }
        };
        var tool = new EngramMcp.Tools.Tools.Consolidate.McpTool(memoryService);

        var response = await tool.ExecuteAsync(
            "write",
            "project-x",
            "token-42",
            [new MaintenanceMemoryEntry { Timestamp = "2026-03-28T10:15:30.0000000+01:00", Text = "Consolidated", Importance = null }]);

        response.Section.Is("project-x");
        response.ConsolidationToken.IsNull();
        response.Entries.IsNotNull();
        response.Entries![0].Text.Is("Consolidated");
        response.Failure.IsNull();
    }
}
