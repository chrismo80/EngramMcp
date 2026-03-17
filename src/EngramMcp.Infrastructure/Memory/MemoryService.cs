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
        IReadOnlyList<string>? tags = null,
        MemoryImportance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        return memoryStore.UpdateAsync(
            container =>
            {
                var resolvedSection = ResolveSectionName(normalizedSection, container);
                var memory = memoryCatalog.GetByName(resolvedSection);
                memory.Store(container, new MemoryEntry(CreateTimestamp(), text, tags, importance));
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
                              ?? throw new KeyNotFoundException($"Memory section '{normalizedSection}' was not found. Available sections: {GetAvailableSectionNames(container)}.");
        var entries = container.Memories.TryGetValue(resolvedSection, out var existingEntries) ? existingEntries : [];

        return new MaintenanceSectionReadResult
        {
            Section = resolvedSection,
            Entries = [.. entries.Select(ToMaintenanceEntry)],
            MaintenanceToken = maintenanceTokenProvider.Issue(resolvedSection)
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

    public async Task<IReadOnlyList<MemorySearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Search query must not be null, empty, or whitespace.", nameof(query));

        var queryTokens = TokenizeQuery(query);
        var container = await memoryStore.LoadAsync(cancellationToken).ConfigureAwait(false);

        return container.Memories
            .SelectMany(section => section.Value.Select(entry => new MemorySearchResult(section.Key, entry)))
            .Select(result => new { Result = result, MatchedTokenCount = CountMatchedTokens(result, queryTokens) })
            .Where(result => result.MatchedTokenCount > 0)
            .OrderByDescending(result => result.MatchedTokenCount)
            .ThenByDescending(result => result.Result.Entry.Importance)
            .ThenByDescending(result => result.Result.Entry.Timestamp)
            .Select(result => result.Result)
            .ToList();
    }

    public async Task<MaintenanceSectionWriteResult> WriteForMaintenanceAsync(
        string section,
        string maintenanceToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var normalizedSection = NormalizeSectionIdentifier(section);

        if (string.IsNullOrWhiteSpace(maintenanceToken))
            throw MaintenanceSectionWriteException.MaintenanceTokenMissing("Maintenance token is required for write mode. Read the section again before submitting a replacement.");

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
                                      ?? throw MaintenanceSectionWriteException.SectionNotFound($"Memory section '{normalizedSection}' was not found. Read an existing section before starting maintenance.");

                    var reservationStatus = maintenanceTokenProvider.TryReserveForSection(maintenanceToken, resolvedSection, out reservation);

                    if (reservationStatus == MaintenanceTokenReservationStatus.Invalid)
                        throw MaintenanceSectionWriteException.MaintenanceTokenInvalid($"Maintenance token is invalid for section '{resolvedSection}'. Read the section again and use the returned token.");

                    if (reservationStatus == MaintenanceTokenReservationStatus.Stale)
                        throw MaintenanceSectionWriteException.MaintenanceTokenStale($"Maintenance token is stale for section '{resolvedSection}'. Read the section again before any further maintenance.");

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

    private static HashSet<string> TokenizeQuery(string query)
    {
        return query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static int CountMatchedTokens(MemorySearchResult result, IEnumerable<string> queryTokens)
    {
        return queryTokens.Count(token =>
            result.Section.Contains(token, StringComparison.OrdinalIgnoreCase)
            || result.Entry.Text.Contains(token, StringComparison.OrdinalIgnoreCase)
            || result.Entry.Tags.Any(tag => tag.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private static MaintenanceMemoryEntry ToMaintenanceEntry(MemoryEntry entry)
    {
        return new MaintenanceMemoryEntry
        {
            Timestamp = entry.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            Text = entry.Text,
            Tags = entry.Tags.Count == 0 ? null : entry.Tags,
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

        return new MemoryEntry(timestamp, entry.Text, entry.Tags, importance);
    }

    private static List<MemoryEntry> ValidateAndConvertMaintenanceEntries(IReadOnlyList<MaintenanceMemoryEntry> entries)
    {
        if (entries.Count == 0)
        {
            throw MaintenanceSectionWriteException.ValidationFailed(
                "Maintenance writes must include at least one entry. This tool is for curation, not clearing a section.",
                [new MaintenanceSectionFailureDetail
                {
                    Field = "entries",
                    Message = "Provide the complete curated replacement list with at least one entry."
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
            throw MaintenanceSectionWriteException.ValidationFailed("Maintenance write request is invalid.", details);

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
