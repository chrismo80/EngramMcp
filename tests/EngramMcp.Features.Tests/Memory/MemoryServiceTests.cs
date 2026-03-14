using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task StoreAsync_RejectsWhitespaceOnlyText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(new MemoryContainer
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(new MemoryContainer
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(new MemoryContainer
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
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync(ShortTerm, "valid single line");

        memoryStore.Container.Memories[ShortTerm].Count.Is(1);
        memoryStore.Container.Memories[ShortTerm][0].Text.Is("valid single line");
    }

    [Fact]
    public async Task StoreAsync_PersistsNormalizedTags()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync(ShortTerm, "tagged", ["Docker", "ops", "docker", "   "]);

        memoryStore.Container.Memories[ShortTerm][0].Tags.SequenceEqual(["docker", "ops"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_PersistsExplicitImportance()
    {
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

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

        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

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
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

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
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

        await service.StoreAsync("PROJECT-X", "custom");

        memoryStore.Container.Memories.ContainsKey("PROJECT-X").IsFalse();
        memoryStore.Container.Memories["project-x"].Select(entry => entry.Text).ToArray().SequenceEqual(["existing", "custom"]).IsTrue();
    }

    [Fact]
    public async Task StoreAsync_UsesSharedCustomBucketCapacity()
    {
        var catalog = new CodeMemoryCatalog();
        var memoryStore = new InMemoryStore(CreateContainer());
        var service = new MemoryService(catalog, memoryStore);

        foreach (var index in Enumerable.Range(1, 25))
            await service.StoreAsync("project-x", $"entry-{index}");

        var entries = memoryStore.Container.Memories["project-x"];
        entries.Count.Is(catalog.GetByName("project-x").Capacity);
        entries[0].Text.Is("entry-6");
        entries[^1].Text.Is("entry-25");
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
        var service = new MemoryService(new CodeMemoryCatalog(), memoryStore);

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
            var catalog = new CodeMemoryCatalog();
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
    public async Task RecallAsync_ReturnsConfiguredSectionsSeparatedByName()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer()));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(query));

        exception.Message.Is("Search query must not be null, empty, or whitespace. (Parameter 'query')");
    }

    [Fact]
    public async Task SearchAsync_SortsByImportanceDescendingThenTimestampDescending()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryStore(CreateContainer(new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
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

    private sealed class InMemoryStore(MemoryContainer container) : IMemoryStore
    {
        public MemoryContainer Container { get; private set; } = container;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);

            var container = Clone(Container);
            update(container);
            Container = container;
            return Task.CompletedTask;
        }

        public Task<MemoryContainer> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Clone(Container));
        }

        public Task SaveAsync(MemoryContainer container, CancellationToken cancellationToken = default)
        {
            Container = Clone(container);
            return Task.CompletedTask;
        }

        private static MemoryContainer Clone(MemoryContainer container)
        {
            return new MemoryContainer
            {
                Memories = container.Memories.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text, entry.Tags, entry.Importance)).ToList(),
                    StringComparer.Ordinal),
                CustomSections = [.. container.CustomSections.Select(summary => new MemorySectionSummary(summary.Name, summary.EntryCount))]
            };
        }
    }
}
