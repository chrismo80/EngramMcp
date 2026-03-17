using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class MaintainSectionTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "maintain_section", Title = "Maintain Section")]
    [Description("Maintain one existing memory section using an explicit read/write workflow guarded by a maintenance token.")]
    public async Task<MaintainSectionResponse> ExecuteAsync(
        [Description("The maintenance mode: read or write.")]
        string mode,
        [Description("The existing memory section to maintain.")]
        string section,
        [Description("The maintenance token returned by a successful read. Required for write mode.")]
        string? maintenanceToken = null,
        [Description("The complete replacement entry list for write mode.")]
        IReadOnlyList<MaintenanceMemoryEntry>? entries = null,
        CancellationToken cancellationToken = default)
    {
        return mode switch
        {
            "read" => (await memoryService.ReadForMaintenanceAsync(section, cancellationToken).ConfigureAwait(false)).ToMaintainSectionResponse(),
            "write" => (await memoryService.WriteForMaintenanceAsync(
                section,
                maintenanceToken ?? throw new ArgumentException("Maintenance token is required for write mode.", nameof(maintenanceToken)),
                entries ?? throw new ArgumentException("Entries are required for write mode.", nameof(entries)),
                cancellationToken).ConfigureAwait(false)).ToMaintainSectionResponse(),
            _ => throw new ArgumentException("Maintenance mode must be 'read' or 'write'.", nameof(mode))
        };
    }
}
