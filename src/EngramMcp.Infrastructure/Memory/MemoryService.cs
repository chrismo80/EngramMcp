using System.Globalization;
using EngramMcp.Core;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class MemoryService(
    IMemoryCatalog memoryCatalog,
    IMemoryStore memoryStore,
    IMaintenanceTokenProvider maintenanceTokenProvider)
    : IMemoryService
{
    private const int MaxMemoryTextLength = 500;

    public MemoryService(IMemoryCatalog memoryCatalog, IMemoryStore memoryStore)
        : this(memoryCatalog, memoryStore, new InMemoryMaintenanceTokenProvider())
    {
    }

    public Task StoreAsync(
        string section,
        string text,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        return memoryStore.UpdateAsync(
            container =>
            {
                var resolvedSection = ResolveSectionName(normalizedSection, container);
                var memory = memoryCatalog.GetByName(resolvedSection);
                memory.Store(container, new MemoryEntry(CreateTimestamp(), text, importance));
            },
            cancellationToken);
    }

    public async Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var resolvedSection = TryResolveExistingSectionName(normalizedSection, container);

        if (resolvedSection is not null)
            return CreateSectionDocument(resolvedSection, container.Memories.TryGetValue(resolvedSection, out var entries) ? entries : []);

        throw new KeyNotFoundException($"Memory section '{normalizedSection}' was not found. Available sections: {GetAvailableSectionNames(container)}.");
    }

    public async Task<MaintenanceSectionReadResult> ReadForMaintenanceAsync(string section, CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var resolvedSection = TryResolveExistingSectionName(normalizedSection, container)
                              ?? throw MaintenanceSectionWriteException.SectionNotFound($"Memory section '{normalizedSection}' was not found. Available sections: {GetAvailableSectionNames(container)}.");
        var entries = container.Memories.TryGetValue(resolvedSection, out var existingEntries) ? existingEntries : [];

        return new MaintenanceSectionReadResult
        {
            Section = resolvedSection,
            Entries = [.. entries.Select(ToMaintenanceEntry)],
            ConsolidationToken = maintenanceTokenProvider.Issue(resolvedSection)
        };
    }

    public async Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
    {
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        var recalled = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memory in memoryCatalog.GetRecallOrder(container))
            recalled[memory.Name] = [.. memory.Read(container)];

        return new MemoryContainer
        {
            Memories = recalled,
            CustomSections = GetCustomSectionSummaries(container)
        };
    }

    public async Task<MaintenanceSectionWriteResult> WriteForMaintenanceAsync(
        string section,
        string consolidationToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        if (string.IsNullOrWhiteSpace(consolidationToken))
            throw MaintenanceSectionWriteException.ConsolidationTokenMissing("Consolidation token is required for write mode. Call read first, then submit the full replacement for that same section.");

        ArgumentNullException.ThrowIfNull(entries);

        var replacementEntries = ValidateAndConvertMaintenanceEntries(entries);
        string? resolvedSection = null;
        MaintenanceTokenReservation reservation = default;
        var hasReservation = false;

        try
        {
            await memoryStore.UpdateAsync(
                container =>
                {
                    resolvedSection = TryResolveExistingSectionName(normalizedSection, container)
                                      ?? throw MaintenanceSectionWriteException.SectionNotFound($"Memory section '{normalizedSection}' was not found. Read an existing section before starting consolidation.");

                    var reservationStatus = maintenanceTokenProvider.TryReserveForSection(consolidationToken, resolvedSection, out reservation);

                    if (reservationStatus == MaintenanceTokenReservationStatus.Invalid)
                        throw MaintenanceSectionWriteException.ConsolidationTokenInvalid($"Consolidation token is invalid for section '{resolvedSection}'. Call read again for that section and use the returned token.");

                    if (reservationStatus == MaintenanceTokenReservationStatus.Stale)
                        throw MaintenanceSectionWriteException.ConsolidationTokenStale($"Consolidation token is stale for section '{resolvedSection}'. Call read again before any further consolidation.");

                    hasReservation = true;

                    var memory = memoryCatalog.GetByName(resolvedSection);

                    if (replacementEntries.Count > memory.Capacity)
                        throw MaintenanceSectionWriteException.ValidationFailed(
                            $"Replacement entries exceed capacity {memory.Capacity} for section '{resolvedSection}'.",
                            [new MaintenanceSectionFailureDetail
                            {
                                Field = "entries",
                                Message = $"Provide between 1 and {memory.Capacity} entries for section '{resolvedSection}'."
                            }]);

                    container.Memories[resolvedSection] = [.. replacementEntries];
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (hasReservation)
                maintenanceTokenProvider.Release(reservation);

            throw;
        }

        maintenanceTokenProvider.Complete(reservation);

        return new MaintenanceSectionWriteResult
        {
            Section = resolvedSection!,
            Entries = [.. replacementEntries.Select(ToMaintenanceEntry)]
        };
    }

    private static MemoryContainer CreateSectionDocument(string section, IReadOnlyList<MemoryEntry> entries)
    {
        return new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [section] = [.. entries]
            }
        };
    }

    private static MaintenanceMemoryEntry ToMaintenanceEntry(MemoryEntry entry)
    {
        return new MaintenanceMemoryEntry
        {
            Timestamp = entry.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            Text = entry.Text,
            Importance = entry.Importance == MemoryImportance.Normal ? null : entry.Importance.ToSerializedValue()
        };
    }

    private static MemoryEntry ToMemoryEntry(MaintenanceMemoryEntry entry)
    {
        MemoryImportance? importance = null;

        if (entry.Importance is { } importanceValue)
        {
            importanceValue.TryParseSerializedValue(out var parsedImportance);
            importance = parsedImportance;
        }

        DateTime.TryParse(entry.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp);

        return new MemoryEntry(timestamp, entry.Text, importance);
    }

    private static List<MemoryEntry> ValidateAndConvertMaintenanceEntries(IReadOnlyList<MaintenanceMemoryEntry> entries)
    {
        if (entries.Count == 0)
        {
            throw MaintenanceSectionWriteException.ValidationFailed(
                "Consolidation writes must include at least one entry. This tool replaces one section with a consolidated set; it does not clear sections.",
                [new MaintenanceSectionFailureDetail
                {
                    Field = "entries",
                    Message = "Provide the complete consolidated replacement list with at least one entry."
                }]);
        }

        var details = new List<MaintenanceSectionFailureDetail>();

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var fieldPrefix = $"entries[{index}]";

            if (entry is null)
            {
                details.Add(new MaintenanceSectionFailureDetail
                {
                    Field = fieldPrefix,
                    Message = "Entry is required."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.Timestamp))
            {
                details.Add(new MaintenanceSectionFailureDetail
                {
                    Field = $"{fieldPrefix}.timestamp",
                    Message = "Timestamp is required and must be a valid round-trip datetime string."
                });
            }
            else if (!DateTime.TryParse(entry.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                details.Add(new MaintenanceSectionFailureDetail
                {
                    Field = $"{fieldPrefix}.timestamp",
                    Message = $"Timestamp '{entry.Timestamp}' is invalid."
                });
            }

            if (string.IsNullOrWhiteSpace(entry.Text))
            {
                details.Add(new MaintenanceSectionFailureDetail
                {
                    Field = $"{fieldPrefix}.text",
                    Message = "Text is required and must not be empty or whitespace."
                });
            }
            else
            {
                if (entry.Text.Contains('\r') || entry.Text.Contains('\n'))
                {
                    details.Add(new MaintenanceSectionFailureDetail
                    {
                        Field = $"{fieldPrefix}.text",
                        Message = "Text must be a single line without carriage returns or line feeds."
                    });
                }

                if (entry.Text.Length > MaxMemoryTextLength)
                {
                    details.Add(new MaintenanceSectionFailureDetail
                    {
                        Field = $"{fieldPrefix}.text",
                        Message = $"Text must be {MaxMemoryTextLength} characters or fewer."
                    });
                }
            }

            if (entry.Importance is { } importanceValue && !importanceValue.TryParseSerializedValue(out _))
            {
                details.Add(new MaintenanceSectionFailureDetail
                {
                    Field = $"{fieldPrefix}.importance",
                    Message = $"Importance '{importanceValue}' is invalid. Supported values: low, normal, high."
                });
            }
        }

        if (details.Count > 0)
            throw MaintenanceSectionWriteException.ValidationFailed("Consolidation write request is invalid.", details);

        return entries.Select(ToMemoryEntry).ToList();
    }

    private string GetAvailableSectionNames(MemoryContainer container)
    {
        var builtInNames = memoryCatalog.Memories.Select(memory => memory.Name);
        var customNames = container.Memories.Keys
            .Except(memoryCatalog.Memories.Select(memory => memory.Name), StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", builtInNames.Concat(customNames));
    }

    private List<MemorySectionSummary> GetCustomSectionSummaries(MemoryContainer container)
    {
        var builtInNames = memoryCatalog.Memories
            .Select(memory => memory.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return container.Memories
            .Where(pair => !builtInNames.Contains(pair.Key))
            .Select(pair => new MemorySectionSummary(pair.Key, pair.Value.Count))
            .OrderByDescending(summary => summary.EntryCount)
            .ThenBy(summary => summary.Name, StringComparer.Ordinal)
            .ToList();
    }

    private string ResolveSectionName(string requestedSection, MemoryContainer container)
    {
        var fixedMemory = memoryCatalog.GetByName(requestedSection);

        if (memoryCatalog.Memories.Any(memory => string.Equals(memory.Name, requestedSection, StringComparison.OrdinalIgnoreCase)))
            return fixedMemory.Name;

        return FindExistingCustomSectionName(requestedSection, container) ?? requestedSection;
    }

    private string? TryResolveExistingSectionName(string requestedSection, MemoryContainer container)
    {
        var fixedMemory = memoryCatalog.Memories.FirstOrDefault(memory => string.Equals(memory.Name, requestedSection, StringComparison.OrdinalIgnoreCase));

        if (fixedMemory is not null)
            return fixedMemory.Name;

        return FindExistingCustomSectionName(requestedSection, container);
    }

    private static string NormalizeSectionIdentifier(string? section)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Memory section identifier must not be null, empty, or whitespace.", nameof(section));

        return section.Trim();
    }

    private static string? FindExistingCustomSectionName(string requestedSection, MemoryContainer container)
    {
        var matches = container.Memories.Keys
            .Where(name => string.Equals(name, requestedSection, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return matches.Length switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new InvalidOperationException($"Memory store contains multiple sections that differ only by casing for '{requestedSection}'.")
        };
    }

    private static DateTime CreateTimestamp() => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
}
