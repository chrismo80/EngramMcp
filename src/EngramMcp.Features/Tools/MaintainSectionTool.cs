using System.ComponentModel;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using ModelContextProtocol.Server;

namespace EngramMcp.Features.Tools;

public sealed class MaintainSectionTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "maintain_section", Title = "Maintain Section")]
    [Description("Curate one existing memory section with a deliberate read-before-write workflow. Use read to fetch raw entries plus a maintenance token, then write the complete curated replacement list back for that same existing section. This is a cleanup and consolidation tool, not a deletion tool, append API, or partial patch API.")]
    public async Task<MaintainSectionResponse> ExecuteAsync(
        [Description("The maintenance mode: read or write.")]
        string mode,
        [Description("The existing memory section to curate. Write never creates a missing section.")]
        string section,
        [Description("The maintenance token returned by read. Required for write mode. After any successful write, including a no-op rewrite, all previously issued tokens for that section become stale and clients must read again before another maintenance round.")]
        string? maintenanceToken = null,
        [Description("The complete curated replacement entry list for write mode. Write fully replaces the target section, requires at least one entry, requires valid non-empty text and valid timestamps on every entry, rejects unsupported importance values, and may normalize tags.")]
        IReadOnlyList<MaintenanceMemoryEntry>? entries = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return mode switch
            {
                "read" => (await memoryService.ReadForMaintenanceAsync(section, cancellationToken).ConfigureAwait(false)).ToMaintainSectionResponse(),
                "write" => (await ExecuteWriteAsync(section, maintenanceToken, entries, cancellationToken).ConfigureAwait(false)).ToMaintainSectionResponse(),
                _ => throw new ArgumentException("Maintenance mode must be 'read' or 'write'.", nameof(mode))
            };
        }
        catch (MaintenanceSectionWriteException exception)
        {
            return new MaintainSectionResponse
            {
                Failure = exception.Failure
            };
        }
    }

    private async Task<MaintenanceSectionWriteResult> ExecuteWriteAsync(
        string section,
        string? maintenanceToken,
        IReadOnlyList<MaintenanceMemoryEntry>? entries,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(maintenanceToken))
            throw MaintenanceSectionWriteException.MaintenanceTokenMissing("Maintenance token is required for write mode. Read the section again before submitting a replacement.");

        if (entries is null)
        {
            throw MaintenanceSectionWriteException.ValidationFailed(
                "Maintenance write request is invalid.",
                [new MaintenanceSectionFailureDetail
                {
                    Field = "entries",
                    Message = "Entries are required for write mode and must contain the complete curated replacement list."
                }]);
        }

        return await memoryService.WriteForMaintenanceAsync(section, maintenanceToken, entries, cancellationToken).ConfigureAwait(false);
    }
}
