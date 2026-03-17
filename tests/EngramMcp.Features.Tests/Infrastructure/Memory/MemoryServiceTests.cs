using EngramMcp.Core;
using EngramMcp.Infrastructure.Memory;
using EngramMcp.Features.Tests.TestDoubles;
using Is.Assertions;
using System.Text.Json;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Infrastructure.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task StoreAsync_RejectsWhitespaceOnlyText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => service.StoreAsync(ShortTerm, "   "));
    }

    [Fact]
    public async Task StoreAsync_RejectsMultilineText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        }));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.StoreAsync(ShortTerm, "first\nsecond"));

        exception.Message.Is("Memory text must be a single line without carriage returns or line feeds. (Parameter 'text')");
    }

    [Fact]
    public async Task StoreAsync_RejectsOverlyLongText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        }));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.StoreAsync(ShortTerm, new string('a', 581)));

        exception.Message.Is("Memory text must be 500 characters or fewer. (Parameter 'text')");
    }

    [Fact]
    public async Task StoreAsync_PersistsValidSingleLineText()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync(ShortTerm, "valid single line");

        memoryStore.Container.Memories[ShortTerm].Count.Is(1);
        memoryStore.Container.Memories[ShortTerm][0].Text.Is("valid single line");
    }

    [Fact]
    public async Task StoreAsync_PersistsNormalizedTags()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync(ShortTerm, "tagged", ["Docker", "ops", "docker", "   "]);

        memoryStore.Container.Memories[ShortTerm][0].Tags.SequenceEqual(["docker", "ops"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_PersistsExplicitImportance()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync(ShortTerm, "important", importance: MemoryImportance.High);

        memoryStore.Container.Memories[ShortTerm][0].Importance.Is(MemoryImportance.High);
    }

    [Fact]
    public async Task StoreAsync_AllowsDuplicates_AndUsesTargetMemoryOnly()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0), "existing")],
            [LongTerm] = []
        }));

        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync(ShortTerm, "duplicate");
        await service.StoreAsync(ShortTerm, "duplicate");

        memoryStore.Container.Memories[ShortTerm].Count.Is(2);
        memoryStore.Container.Memories[ShortTerm][0].Text.Is("duplicate");
        memoryStore.Container.Memories[ShortTerm][1].Text.Is("duplicate");
        memoryStore.Container.Memories[ShortTerm][0].Tags.Count.Is(0);
        memoryStore.Container.Memories[ShortTerm][0].Importance.Is(MemoryImportance.Normal);
        memoryStore.Container.Memories[MediumTerm].Count.Is(1);
    }

    [Fact]
    public async Task StoreAsync_CreatesCustomBucketOnFirstWrite()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync("project-x", "custom");

        memoryStore.Container.Memories.ContainsKey("project-x").IsTrue();
        memoryStore.Container.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_ReusesExistingCustomSectionCaseInsensitively()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-x"] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "existing")]
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);

        await service.StoreAsync("PROJECT-X", "custom");

        memoryStore.Container.Memories.ContainsKey("PROJECT-X").IsFalse();
        memoryStore.Container.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["existing", "custom"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_UsesSharedCustomBucketCapacity()
    {
        var catalog = new CodeMemoryCatalog(MemorySize.Normal);
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(catalog, memoryStore);

        foreach (var index in Enumerable.Range(1, 50))
            await service.StoreAsync("project-x", $"entry-{index}");

        var entries = memoryStore.Container.Memories["project-x"];
        entries.Count.Is(catalog.GetByName("project-x").Capacity);
        entries[0].Text.Is("entry-11");
        entries[^1].Text.Is("entry-50");
    }

    [Fact]
    public async Task StoreAsync_EvictsLowerImportanceBeforeOlderHigherImportance()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 8, 0, 0), "low", importance: MemoryImportance.Low),
                new(new DateTime(2026, 3, 11, 9, 0, 0), "normal-1", importance: MemoryImportance.Normal),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "normal-2", importance: MemoryImportance.Normal),
                new(new DateTime(2026, 3, 11, 11, 0, 0), "normal-3", importance: MemoryImportance.Normal),
                new(new DateTime(2026, 3, 11, 12, 0, 0), "high", importance: MemoryImportance.High)
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Small), memoryStore);

        await service.StoreAsync(ShortTerm, "new-high", importance: MemoryImportance.High);

        memoryStore.Container.Memories[ShortTerm].Select(entry => entry.Text).ToArray()
            .SequenceEqual(["normal-1", "normal-2", "normal-3", "high", "new-high"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_SerializesConcurrentUpdatesAgainstJsonFileStore()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var filePath = Path.Combine(rootPath, "memory.json");
            var catalog = new CodeMemoryCatalog(MemorySize.Normal);
            var memoryStore = new JsonMemoryStore(filePath, catalog);
            var service = new MemoryService(catalog, memoryStore);
            const int operationCount = 10;
            var shortTermExpectedCount = Math.Min(operationCount, catalog.GetByName(ShortTerm).Capacity);
            var mediumTermExpectedCount = Math.Min(operationCount, catalog.GetByName(MediumTerm).Capacity);
            var longTermExpectedCount = Math.Min(operationCount, catalog.GetByName(LongTerm).Capacity);
            var operations = Enumerable.Range(1, 10)
                .SelectMany(index => new[]
                {
                    service.StoreAsync(ShortTerm, $"short-{index}"),
                    service.StoreAsync(MediumTerm, $"medium-{index}"),
                    service.StoreAsync(LongTerm, $"long-{index}")
                })
                .ToArray();

            await Task.WhenAll(operations);

            var recalled = await service.RecallAsync();

            recalled.Memories[ShortTerm].Count.Is(shortTermExpectedCount);
            recalled.Memories[MediumTerm].Count.Is(mediumTermExpectedCount);
            recalled.Memories[LongTerm].Count.Is(longTermExpectedCount);
            recalled.Memories[ShortTerm].Select(entry => entry.Text).Distinct().Count().Is(shortTermExpectedCount);
            recalled.Memories[ShortTerm].All(entry => entry.Text.StartsWith("short-", StringComparison.Ordinal)).IsTrue();
            recalled.Memories[MediumTerm].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, mediumTermExpectedCount).Select(index => $"medium-{index}").OrderBy(text => text)).IsTrue();
            recalled.Memories[LongTerm].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, longTermExpectedCount).Select(index => $"long-{index}").OrderBy(text => text)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReadAsync_ReturnsBuiltInSectionOnly()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
            [MediumTerm] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "medium")],
            [LongTerm] = []
        })));

        var result = await service.ReadAsync(ShortTerm);

        result.Memories.Keys.ToArray().SequenceEqual([ShortTerm]).IsTrue();
        result.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["short"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_ReturnsCustomSectionOnly()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-x"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
        })));

        var result = await service.ReadAsync("project-x");

        result.Memories.Keys.ToArray().SequenceEqual(["project-x"]).IsTrue();
        result.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_NormalizesBuiltInSectionLookupCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var result = await service.ReadAsync(" SHORT-TERM ");

        result.Memories.Keys.ToArray().SequenceEqual([ShortTerm]).IsTrue();
        result.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["short"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_NormalizesCustomSectionLookupCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-x"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
        })));

        var result = await service.ReadAsync(" PROJECT-X ");

        result.Memories.Keys.ToArray().SequenceEqual(["project-x"]).IsTrue();
        result.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom"]).IsTrue();
    }

    [Fact]
    public async Task ReadAsync_ThrowsWhenSectionDoesNotExist()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-a"] = [],
            ["project-z"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
        })));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ReadAsync("project-x"));

        exception.Message.Is($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a, project-z.");
    }

    [Fact]
    public async Task ReadForMaintenanceAsync_ReturnsBuiltInSectionWithRawEntriesAndToken()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc), "short", ["ops"], MemoryImportance.High)],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var result = await service.ReadForMaintenanceAsync(ShortTerm);

        result.Section.Is(ShortTerm);
        result.Entries.Count.Is(1);
        result.Entries[0].Timestamp.Is("2026-03-11T10:00:00.0000000Z");
        result.Entries[0].Text.Is("short");
        result.Entries[0].Tags!.SequenceEqual(["ops"]).IsTrue();
        result.Entries[0].Importance.Is("high");
        result.MaintenanceToken.IsNotEmpty();
    }

    [Fact]
    public async Task ReadForMaintenanceAsync_ReturnsExistingCustomSectionWithRawEntriesAndToken()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["Project-X"] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0, DateTimeKind.Utc), "custom")]
        })));

        var result = await service.ReadForMaintenanceAsync("project-x");

        result.Section.Is("Project-X");
        result.Entries.Count.Is(1);
        result.Entries[0].Timestamp.Is("2026-03-11T12:00:00.0000000Z");
        result.Entries[0].Text.Is("custom");
        result.MaintenanceToken.IsNotEmpty();
    }

    [Fact]
    public async Task ReadForMaintenanceAsync_ThrowsWhenSectionDoesNotExist()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-a"] = []
        })));

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.ReadForMaintenanceAsync("project-x"));

        exception.Failure.Category.Is("section_not_found");
        exception.Message.Is($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_ReplacesOnlyTargetSection_AndUpdatedContentIsVisibleEverywhere()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [new(new DateTime(2026, 3, 11, 10, 0, 0, DateTimeKind.Utc), "medium")],
            [LongTerm] = [new(new DateTime(2026, 3, 11, 11, 0, 0, DateTimeKind.Utc), "long")],
            ["project-x"] = [new(new DateTime(2026, 3, 11, 12, 0, 0, DateTimeKind.Utc), "custom old")]
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var written = await service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "new short",
                    Tags = ["ops"],
                    Importance = "high"
                }
            ]);

        written.Section.Is(ShortTerm);
        written.Entries.Count.Is(1);
        written.Entries[0].Text.Is("new short");
        memoryStore.Container.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["new short"]).IsTrue();
        memoryStore.Container.Memories[MediumTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["medium"]).IsTrue();
        memoryStore.Container.Memories[LongTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["long"]).IsTrue();
        memoryStore.Container.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["custom old"]).IsTrue();

        var readSection = await service.ReadAsync(ShortTerm);
        readSection.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["new short"]).IsTrue();

        var recall = await service.RecallAsync();
        recall.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["new short"]).IsTrue();

        var search = await service.SearchAsync("new ops");
        search.Count.Is(1);
        search[0].Section.Is(ShortTerm);
        search[0].Entry.Text.Is("new short");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_ThrowsForWrongSectionToken()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(MediumTerm, read.MaintenanceToken, [CreateMaintenanceEntry("moved") ]));

        exception.Failure.Category.Is("maintenance_token_invalid");
        exception.Failure.Message.Is($"Maintenance token is invalid for section '{MediumTerm}'. Read the section again and use the returned token.");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_ThrowsWhenTokenIsStaleAfterSuccessfulWrite()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        await service.WriteForMaintenanceAsync(ShortTerm, read.MaintenanceToken, [CreateMaintenanceEntry("first")]);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, read.MaintenanceToken, [CreateMaintenanceEntry("second") ]));

        exception.Failure.Category.Is("maintenance_token_stale");
        exception.Failure.Message.Is($"Maintenance token is stale for section '{ShortTerm}'. Read the section again before any further maintenance.");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsInvalidImportance()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [],
            [LongTerm] = []
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "invalid",
                    Importance = "urgent"
                }
            ]));

        exception.Failure.Category.Is("validation_failed");
        var detail = exception.Failure.Details!.Single();
        detail.Field.Is("entries[0].importance");
        detail.Message.Is("Importance 'urgent' is invalid. Supported values: low, normal, high.");
        memoryStore.Container.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["old short"]).IsTrue();
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsInvalidTimestamp()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [],
            [LongTerm] = []
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "not-a-timestamp",
                    Text = "invalid"
                }
            ]));

        exception.Failure.Category.Is("validation_failed");
        var detail = exception.Failure.Details!.Single();
        detail.Field.Is("entries[0].timestamp");
        detail.Message.Is("Timestamp 'not-a-timestamp' is invalid.");
        memoryStore.Container.Memories[ShortTerm].Select(entry => entry.Text).ToArray().SequenceEqual(["old short"]).IsTrue();
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsMissingTimestamp()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = " ",
                    Text = "invalid"
                }
            ]));

        exception.Failure.Category.Is("validation_failed");
        exception.Failure.Details!.Single().Field.Is("entries[0].timestamp");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsOmittedTimestampProperty()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);
        var entry = JsonSerializer.Deserialize<MaintenanceMemoryEntry>("""
            {
              "text": "invalid"
            }
            """)!;

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [entry]));

        exception.Failure.Category.Is("validation_failed");
        var detail = exception.Failure.Details!.Single();
        detail.Field.Is("entries[0].timestamp");
        detail.Message.Is("Timestamp is required and must be a valid round-trip datetime string.");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsMissingText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "   "
                }
            ]));

        exception.Failure.Category.Is("validation_failed");
        exception.Failure.Details!.Single().Field.Is("entries[0].text");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsEmptyReplacementList()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [],
            [LongTerm] = []
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, read.MaintenanceToken, []));

        exception.Failure.Category.Is("validation_failed");
        exception.Failure.Details!.Single().Field.Is("entries");
        memoryStore.Container.Memories[ShortTerm].Count.Is(1);
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsMissingMaintenanceToken()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, " ", [CreateMaintenanceEntry("entry") ]));

        exception.Failure.Category.Is("maintenance_token_missing");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsUnknownSection()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync("project-x", "bogus", [CreateMaintenanceEntry("entry") ]));

        exception.Failure.Category.Is("section_not_found");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RejectsBogusToken()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, "bogus", [CreateMaintenanceEntry("entry") ]));

        exception.Failure.Category.Is("maintenance_token_invalid");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_ThrowsWhenReplacementExceedsCapacity()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Small), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            Enumerable.Range(1, 6)
                .Select(index => new MaintenanceMemoryEntry
                {
                    Timestamp = $"2026-03-12T08:30:0{index}Z",
                    Text = $"entry-{index}"
                })
                .ToArray()));

        exception.Failure.Category.Is("validation_failed");
        exception.Failure.Details!.Single().Field.Is("entries");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RetainsTokenWhenOverCapacityWriteFails()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Small), new InMemoryStore(CreateContainer()));
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            Enumerable.Range(1, 6)
                .Select(index => new MaintenanceMemoryEntry
                {
                    Timestamp = $"2026-03-12T08:30:0{index}Z",
                    Text = $"entry-{index}"
                })
                .ToArray()));

        var retry = await service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "retry"
                }
            ]);

        retry.Entries.Count.Is(1);
        retry.Entries[0].Text.Is("retry");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_NormalizesTagsOnSuccessfulWrite()
    {
        var memoryStore = new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [],
            [LongTerm] = []
        }));
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), memoryStore);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var result = await service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "normalized",
                    Tags = ["Docker", "ops", "docker", "  ", null!]
                }
            ]);

        result.Section.Is(ShortTerm);
        result.Entries[0].Tags!.SequenceEqual(["docker", "ops"]).IsTrue();
        memoryStore.Container.Memories[ShortTerm][0].Tags.SequenceEqual(["docker", "ops"]).IsTrue();
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_SuccessfulNoOpWriteInvalidatesPriorTokens()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc), "old short")],
            [MediumTerm] = [],
            [LongTerm] = []
        })));
        var firstRead = await service.ReadForMaintenanceAsync(ShortTerm);

        await service.WriteForMaintenanceAsync(ShortTerm, firstRead.MaintenanceToken, [CreateMaintenanceEntry("old short", "2026-03-11T09:00:00.0000000Z")]);

        var exception = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, firstRead.MaintenanceToken, [CreateMaintenanceEntry("again") ]));

        exception.Failure.Category.Is("maintenance_token_stale");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_SuccessfulWriteMakesAllPreviouslyIssuedTokensStale()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));
        var firstRead = await service.ReadForMaintenanceAsync(ShortTerm);
        var secondRead = await service.ReadForMaintenanceAsync(ShortTerm);

        await service.WriteForMaintenanceAsync(ShortTerm, firstRead.MaintenanceToken, [CreateMaintenanceEntry("first")]);

        var secondException = await Assert.ThrowsAsync<MaintenanceSectionWriteException>(() => service.WriteForMaintenanceAsync(ShortTerm, secondRead.MaintenanceToken, [CreateMaintenanceEntry("second") ]));

        secondException.Failure.Category.Is("maintenance_token_stale");
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_AllowsOnlyOneConcurrentWriteForSingleUseToken()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var filePath = Path.Combine(rootPath, "memory.json");
            var catalog = new CodeMemoryCatalog(MemorySize.Normal);
            var memoryStore = new JsonMemoryStore(filePath, catalog);
            var service = new MemoryService(catalog, memoryStore);
            var read = await service.ReadForMaintenanceAsync(ShortTerm);
            MaintenanceMemoryEntry[] firstEntries =
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "first"
                }
            ];
            MaintenanceMemoryEntry[] secondEntries =
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:31:00.0000000Z",
                    Text = "second"
                }
            ];

            var firstTask = AttemptMaintenanceWriteAsync(service, read.MaintenanceToken, firstEntries);
            var secondTask = AttemptMaintenanceWriteAsync(service, read.MaintenanceToken, secondEntries);

            var results = await Task.WhenAll(firstTask, secondTask);
            var failureCategory = results.Single(result => !result.Succeeded).Exception!.Failure.Category;

            results.Count(result => result.Succeeded).Is(1);
            results.Count(result => !result.Succeeded).Is(1);
            new[] { "maintenance_token_invalid", "maintenance_token_stale" }.Contains(failureCategory).IsTrue();

            var storedTexts = (await service.ReadAsync(ShortTerm)).Memories[ShortTerm].Select(entry => entry.Text).ToArray();
            storedTexts.Length.Is(1);
            storedTexts[0].Is(results.Single(result => result.Succeeded).StoredText!);
        }
        finally
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task WriteForMaintenanceAsync_RetainsTokenWhenSaveFails()
    {
        var catalog = new CodeMemoryCatalog(MemorySize.Normal);
        var tokenProvider = new InMemoryMaintenanceTokenProvider();
        var memoryStore = new FaultInjectingInMemoryStore(CreateContainer())
        {
            FailAfterUpdate = true
        };
        var service = new MemoryService(catalog, memoryStore, tokenProvider);
        var read = await service.ReadForMaintenanceAsync(ShortTerm);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:30:00.0000000Z",
                    Text = "first"
                }
            ]));

        exception.Message.Is("Simulated save failure.");
        memoryStore.FailAfterUpdate = false;

        var retry = await service.WriteForMaintenanceAsync(
            ShortTerm,
            read.MaintenanceToken,
            [
                new MaintenanceMemoryEntry
                {
                    Timestamp = "2026-03-12T08:31:00.0000000Z",
                    Text = "retry"
                }
            ]);

        retry.Entries.Count.Is(1);
        retry.Entries[0].Text.Is("retry");
    }

    [Fact]
    public async Task RecallAsync_ReturnsConfiguredSectionsSeparatedByName()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
            [MediumTerm] = [],
            [LongTerm] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "long")]
        })));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
        recalled.Memories[ShortTerm][0].Text.Is("short");
        recalled.Memories[LongTerm][0].Text.Is("long");
    }

    [Fact]
    public async Task RecallAsync_ReturnsOnlyFixedBucketsInStableOrder()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            ["z-last"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "z")],
            [ShortTerm] = [],
            [MediumTerm] = [],
            ["a-first"] = [new(new DateTime(2026, 3, 11, 13, 0, 0), "a")],
            [LongTerm] = []
        })));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
        recalled.Memories.ContainsKey("a-first").IsFalse();
        recalled.Memories.ContainsKey("z-last").IsFalse();
    }

    [Fact]
    public async Task SearchAsync_MatchesSectionNameAcrossBuiltInAndCustomSections()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "first short")
            ],
            [MediumTerm] = [],
            [LongTerm] = [],
            ["project-shortcuts"] =
            [
                new(new DateTime(2026, 3, 11, 11, 0, 0), "custom short")
            ]
        })));

        var results = await service.SearchAsync("short");

        results.Select(result => result.Section).ToArray().SequenceEqual(["project-shortcuts", ShortTerm]).IsTrue();
    }

    [Fact]
    public async Task SearchAsync_MatchesTagsCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "entry", ["Docker"])
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("DOCK");

        results.Count.Is(1);
        results[0].Entry.Text.Is("entry");
    }

    [Fact]
    public async Task SearchAsync_MatchesEntryTextCaseInsensitively()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "Workspace drift is happening")
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("workspace DRIFT");

        results.Count.Is(1);
        results[0].Section.Is(ShortTerm);
    }

    [Fact]
    public async Task SearchAsync_MatchesAnyQueryTokenAcrossSectionsTextAndTags()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 9, 0, 0), "workspace planning")
            ],
            [MediumTerm] =
            [
                new(new DateTime(2026, 3, 11, 10, 0, 0), "general note", ["docker"])
            ],
            [LongTerm] = [],
            ["project-ops"] =
            [
                new(new DateTime(2026, 3, 11, 11, 0, 0), "custom entry")
            ]
        })));

        var results = await service.SearchAsync("docker workspace ops");

        results.Select(result => result.Section).ToArray().SequenceEqual(["project-ops", MediumTerm, ShortTerm]).IsTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchAsync_RejectsEmptyOrWhitespaceQuery(string query)
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(query));

        exception.Message.Is("Search query must not be null, empty, or whitespace. (Parameter 'query')");
    }

    [Fact]
    public async Task SearchAsync_SortsByImportanceDescendingThenTimestampDescending()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 8, 0, 0), "match", importance: MemoryImportance.High),
                new(new DateTime(2026, 3, 11, 12, 0, 0), "match", importance: MemoryImportance.Normal),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "match", importance: MemoryImportance.High)
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("match");

        results.Select(result => result.Entry.Timestamp).ToArray().SequenceEqual(
            [
                new DateTime(2026, 3, 11, 10, 0, 0),
                new DateTime(2026, 3, 11, 8, 0, 0),
                new DateTime(2026, 3, 11, 12, 0, 0)
            ]).IsTrue();
    }

    [Fact]
    public async Task SearchAsync_RanksByDistinctMatchedTokenCountDescending()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 9, 0, 0), "docker workspace note", ["ops"]),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "docker note"),
                new(new DateTime(2026, 3, 11, 11, 0, 0), "workspace note")
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("docker workspace ops");

        results.Select(result => result.Entry.Text).ToArray().SequenceEqual([
            "docker workspace note",
            "workspace note",
            "docker note"
        ]).IsTrue();
    }

    [Fact]
    public async Task SearchAsync_DoesNotIncreaseScoreForDuplicateQueryTokens()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 9, 0, 0), "docker workspace note"),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "docker note")
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("docker docker workspace");

        results.Select(result => result.Entry.Text).ToArray().SequenceEqual([
            "docker workspace note",
            "docker note"
        ]).IsTrue();
    }

    [Fact]
    public async Task SearchAsync_UsesImportanceAndTimestampTieBreakersWhenTokenCountsMatch()
    {
        var service = new MemoryService(new CodeMemoryCatalog(MemorySize.Normal), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
        {
            [ShortTerm] =
            [
                new(new DateTime(2026, 3, 11, 8, 0, 0), "docker workspace", importance: MemoryImportance.High),
                new(new DateTime(2026, 3, 11, 10, 0, 0), "docker workspace", importance: MemoryImportance.High),
                new(new DateTime(2026, 3, 11, 12, 0, 0), "docker workspace", importance: MemoryImportance.Normal)
            ],
            [MediumTerm] = [],
            [LongTerm] = []
        })));

        var results = await service.SearchAsync("docker workspace");

        results.Select(result => result.Entry.Timestamp).ToArray().SequenceEqual(
            [
                new DateTime(2026, 3, 11, 10, 0, 0),
                new DateTime(2026, 3, 11, 8, 0, 0),
                new DateTime(2026, 3, 11, 12, 0, 0)
            ]).IsTrue();
    }

    private static MemoryContainer CreateContainer(Dictionary<string, List<MemoryEntry>>? memories = null)
    {
        return new MemoryContainer
        {
            Memories = memories ?? new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        };
    }

    private static async Task<MaintenanceWriteAttempt> AttemptMaintenanceWriteAsync(
        MemoryService service,
        string maintenanceToken,
        IReadOnlyList<MaintenanceMemoryEntry> entries)
    {
        try
        {
            var result = await service.WriteForMaintenanceAsync(ShortTerm, maintenanceToken, entries);
            return new MaintenanceWriteAttempt(true, result.Entries.SingleOrDefault()?.Text, null);
        }
        catch (MaintenanceSectionWriteException exception)
        {
            return new MaintenanceWriteAttempt(false, null, exception);
        }
    }

    private static MaintenanceMemoryEntry CreateMaintenanceEntry(string text, string timestamp = "2026-03-12T08:30:00.0000000Z")
    {
        return new MaintenanceMemoryEntry
        {
            Timestamp = timestamp,
            Text = text
        };
    }

    private sealed record MaintenanceWriteAttempt(bool Succeeded, string? StoredText, MaintenanceSectionWriteException? Exception);
}
