using System.ComponentModel;
using EngramMcp.Tools.Maintenance;
using EngramMcp.Tools.Memory;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Tools.Consolidate;

public sealed class McpTool(IMemoryService memoryService) : Tool
{
    [McpServerTool(Name = "consolidate", Title = "Consolidate Memory Section")]
    [Description("Consolidate exactly one existing memory section using a strict read-before-write workflow. First call mode='read' to fetch that section's entries plus a consolidation token, consolidate the replacement externally, then call mode='write' with the same section, the returned token, and the full replacement entries. Write fully replaces that one section; this is not an append API, partial patch API, or deletion API.")]
    public async Task<ConsolidateResponse> ExecuteAsync(
        [Description("Workflow step for exactly one existing section: call 'read' first, then call 'write' with the returned token.")]
        string mode,
        [Description("The one existing memory section to consolidate. Read targets this section, and write must use that same section; write never creates a missing section.")]
        string section,
        [Description("The consolidation token returned by read. Required for write mode, valid only for that same section, and stale after any successful write; after a successful write, call read again before the next consolidation.")]
        string? consolidationToken = null,
        [Description("For write mode, the complete consolidated replacement entry list for that same section. Write fully replaces the section, requires at least one entry, requires valid non-empty text and valid timestamps on every entry, and rejects unsupported importance values.")]
        IReadOnlyList<MaintenanceMemoryEntry>? entries = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return mode switch
            {
                "read" => ConsolidateResponseFactory.Create(await memoryService.ReadForMaintenanceAsync(section, cancellationToken).ConfigureAwait(false)),
                "write" => ConsolidateResponseFactory.Create(await ExecuteWriteAsync(section, consolidationToken, entries, cancellationToken).ConfigureAwait(false)),
                _ => throw new ArgumentException("Consolidation mode must be 'read' or 'write'.", nameof(mode))
            };
        }
        catch (MaintenanceSectionWriteException exception)
        {
            return new ConsolidateResponse
            {
                Failure = exception.Failure
            };
        }
    }

    private async Task<MaintenanceSectionWriteResult> ExecuteWriteAsync(
        string section,
        string? consolidationToken,
        IReadOnlyList<MaintenanceMemoryEntry>? entries,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consolidationToken))
            throw MaintenanceSectionWriteException.ConsolidationTokenMissing("Consolidation token is required for write mode. Call read first, then submit the full replacement for that same section.");

        if (entries is null)
        {
            throw MaintenanceSectionWriteException.ValidationFailed(
                "Consolidation write request is invalid.",
                [new MaintenanceSectionFailureDetail
                {
                    Field = "entries",
                    Message = "Entries are required for write mode and must contain the complete consolidated replacement list for that same section."
                }]);
        }

        return await memoryService.WriteForMaintenanceAsync(section, consolidationToken, entries, cancellationToken).ConfigureAwait(false);
    }
}
