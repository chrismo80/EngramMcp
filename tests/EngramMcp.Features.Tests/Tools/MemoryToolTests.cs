using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class MemoryToolTests
{
    [Fact]
    public async Task StoreShortTermTool_DelegatesToSharedServiceWithShortTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreShortTermTool(service);

        await tool.ExecuteAsync("remember this", importance: "high", cancellationToken: CancellationToken.None);

        service.StoredName.Is(ShortTerm);
        service.StoredText.Is("remember this");
        service.StoredImportance.Is(MemoryImportance.High);
    }

    [Fact]
    public async Task StoreMediumTermTool_DelegatesToSharedServiceWithMediumTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMediumTermTool(service);

        await tool.ExecuteAsync("remember this", cancellationToken: CancellationToken.None);

        service.StoredName.Is(MediumTerm);
    }

    [Fact]
    public async Task StoreLongTermTool_DelegatesToSharedServiceWithLongTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreLongTermTool(service);

        await tool.ExecuteAsync("remember this", cancellationToken: CancellationToken.None);

        service.StoredName.Is(LongTerm);
    }

    [Fact]
    public async Task StoreTool_DelegatesToSharedServiceWithProvidedSectionAndTags()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        await tool.ExecuteAsync("project-x", "remember this", ["Docker", "ops", "docker", "   "], "low", CancellationToken.None);

        service.StoredName.Is("project-x");
        service.StoredText.Is("remember this");
        service.StoredTags!.SequenceEqual(["Docker", "ops", "docker", "   "]).IsTrue();
        service.StoredImportance.Is(MemoryImportance.Low);
    }

    [Fact]
    public async Task StoreTool_AllowsBuiltInSectionNames()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        await tool.ExecuteAsync(LongTerm, "remember this", cancellationToken: CancellationToken.None);

        service.StoredName.Is(LongTerm);
    }

    [Fact]
    public async Task RecallTool_ReturnsStructuredMemoriesWithOrderedSectionsAndNoTimestamps()
    {
        var expected = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [LongTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 13, 0, 0), "long")],
                [MediumTerm] = [],
                [ShortTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short")],
            }
        };

        var service = new SpyMemoryService { RecallResult = expected };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.Memories.Keys.ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
        result.Memories[LongTerm].Count.Is(1);
        result.Memories[LongTerm][0].Text.Is("long");
        result.Memories[LongTerm][0].Tags.Is(null);
        result.Memories[LongTerm][0].Importance.Is(null);
        result.Memories[MediumTerm].Count.Is(0);
        result.Memories[ShortTerm].Count.Is(1);
        result.Memories[ShortTerm][0].Text.Is("short");

        Assert.DoesNotContain(
            typeof(MemoryVisibleItemResponse).GetProperties(),
            property => string.Equals(property.Name, nameof(MemoryEntry.Timestamp), StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecallTool_OmitsCustomSectionListingBlockWhenNoCustomSectionsExist()
    {
        var service = new SpyMemoryService
        {
            RecallResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [LongTerm] = [],
                    [MediumTerm] = [],
                    [ShortTerm] = []
                }
            }
        };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.CustomSections.Is(null);
    }

    [Fact]
    public async Task RecallTool_ReturnsCustomSectionListingSortedByDescendingEntryCount()
    {
        var service = new SpyMemoryService
        {
            RecallResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [LongTerm] = [],
                    [MediumTerm] = [],
                    [ShortTerm] = []
                },
                CustomSections =
                [
                    new MemorySectionSummary("project-small", 1),
                    new MemorySectionSummary("project-large", 4),
                    new MemorySectionSummary("project-medium", 2)
                ]
            }
        };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.CustomSections!.Select(section => (section.Name, section.EntryCount)).ToArray().SequenceEqual(
            [("project-large", 4), ("project-medium", 2), ("project-small", 1)]).IsTrue();
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsStructuredResponseForBuiltInSectionOnly()
    {
        var service = new SpyMemoryService
        {
            ReadResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [ShortTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short", ["ops", "todo"])]
                }
            }
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync(ShortTerm, CancellationToken.None);
        var response = Assert.IsType<ReadSectionResponse>(result);

        service.ReadSection.Is(ShortTerm);
        response.Memories.Keys.ToArray().SequenceEqual([ShortTerm]).IsTrue();
        response.Memories[ShortTerm].Count.Is(1);
        response.Memories[ShortTerm][0].Text.Is("short");
        response.Memories[ShortTerm][0].Tags!.SequenceEqual(["ops", "todo"]).IsTrue();
        response.Memories[ShortTerm][0].Importance.Is(null);
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsStructuredResponseForCustomSectionOnly()
    {
        var service = new SpyMemoryService
        {
            ReadResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    ["project-x"] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "custom")]
                }
            }
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("project-x", CancellationToken.None);
        var response = Assert.IsType<ReadSectionResponse>(result);

        service.ReadSection.Is("project-x");
        response.Memories.Keys.ToArray().SequenceEqual(["project-x"]).IsTrue();
        response.Memories["project-x"][0].Text.Is("custom");
        response.Memories["project-x"][0].Tags.Is(null);
        response.Memories["project-x"][0].Importance.Is(null);
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsStructuredEmptyResponseForMissingSection()
    {
        var service = new SpyMemoryService
        {
            ReadException = new KeyNotFoundException($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.")
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("project-x", CancellationToken.None);

        result.Memories.Keys.ToArray().SequenceEqual(["project-x"]).IsTrue();
        result.Memories["project-x"].Count.Is(0);
    }

    [Fact]
    public async Task ReadSectionTool_PropagatesInvalidSectionFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new ArgumentException("Memory section identifier must not be null, empty, or whitespace.", "section")
        };
        var tool = new ReadSectionTool(service);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("   ", CancellationToken.None));

        exception.Message.Is("Memory section identifier must not be null, empty, or whitespace. (Parameter 'section')");
    }

    [Fact]
    public async Task ReadSectionTool_PropagatesInternalFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new InvalidOperationException("disk unavailable")
        };
        var tool = new ReadSectionTool(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => tool.ExecuteAsync("project-x", CancellationToken.None));

        exception.Message.Is("disk unavailable");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsStructuredEmptyResponseForExistingEmptySection()
    {
        var service = new SpyMemoryService
        {
            ReadResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    ["project-empty"] = []
                }
            }
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("project-empty", CancellationToken.None);

        result.Memories.Keys.ToArray().SequenceEqual(["project-empty"]).IsTrue();
        result.Memories["project-empty"].Count.Is(0);
    }

    [Fact]
    public async Task SearchTool_ReturnsStructuredResultsWithSectionContext()
    {
        var service = new SpyMemoryService
        {
            SearchResult =
            [
                new MemorySearchResult(
                    "project-x",
                    new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0, DateTimeKind.Utc), "docker reminder", ["ops"], MemoryImportance.High))
            ]
        };
        var tool = new SearchTool(service);

        var result = await tool.ExecuteAsync("docker", CancellationToken.None);

        service.SearchQuery.Is("docker");
        result.Results.Count.Is(1);
        result.Results[0].Text.Is("docker reminder");
        result.Results[0].Section.Is("project-x");
        result.Results[0].Tags!.SequenceEqual(["ops"]).IsTrue();
        result.Results[0].Importance.Is("high");
    }

    [Fact]
    public async Task SearchTool_ReturnsStructuredEmptyResults()
    {
        var service = new SpyMemoryService();
        var tool = new SearchTool(service);

        var result = await tool.ExecuteAsync("missing", CancellationToken.None);

        result.Results.Count.Is(0);
    }

    [Fact]
    public async Task SearchTool_ReturnsOneStructuredItemPerResultWithoutTextFlatteningLogic()
    {
        var service = new SpyMemoryService
        {
            SearchResult =
            [
                new MemorySearchResult(
                    "project-x",
                    new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0, DateTimeKind.Utc), "docker reminder", ["ops"], MemoryImportance.High)),
                new MemorySearchResult(
                    "project-y",
                    new MemoryEntry(new DateTime(2026, 3, 11, 13, 0, 0, DateTimeKind.Utc), "workspace note", ["dev"], MemoryImportance.Normal))
            ]
        };
        var tool = new SearchTool(service);

        var result = await tool.ExecuteAsync("docker", CancellationToken.None);

        result.Results.Count.Is(2);
        result.Results[0].Text.Is("docker reminder");
        result.Results[0].Importance.Is("high");
        result.Results[1].Text.Is("workspace note");
        result.Results[1].Importance.Is(null);
    }

    [Fact]
    public async Task SearchTool_PropagatesInvalidQueryFailure()
    {
        var service = new SpyMemoryService
        {
            SearchException = new ArgumentException("Search query must not be null, empty, or whitespace.", "query")
        };
        var tool = new SearchTool(service);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("   ", CancellationToken.None));

        exception.Message.Is("Search query must not be null, empty, or whitespace. (Parameter 'query')");
    }

    [Fact]
    public async Task RecallTool_PreservesVisibleFieldsOnly()
    {
        var service = new SpyMemoryService
        {
            RecallResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [LongTerm] = [],
                    [MediumTerm] = [],
                    [ShortTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short", ["ops"], MemoryImportance.High)]
                }
            }
        };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.Memories[LongTerm].Count.Is(0);
        result.Memories[MediumTerm].Count.Is(0);
        result.Memories[ShortTerm].Count.Is(1);
        result.Memories[ShortTerm][0].Text.Is("short");
        result.Memories[ShortTerm][0].Tags!.SequenceEqual(["ops"]).IsTrue();
        result.Memories[ShortTerm][0].Importance.Is("high");
    }

    private sealed class SpyMemoryService : IMemoryService
    {
        public string? StoredName { get; private set; }

        public string? StoredText { get; private set; }

        public IReadOnlyList<string>? StoredTags { get; private set; }

        public MemoryImportance? StoredImportance { get; private set; }

        public string? ReadSection { get; private set; }

        public string? SearchQuery { get; private set; }

        public MemoryContainer RecallResult { get; init; } = new();

        public MemoryContainer ReadResult { get; init; } = new();

        public IReadOnlyList<MemorySearchResult> SearchResult { get; init; } = [];

        public Exception? ReadException { get; init; }

        public Exception? SearchException { get; init; }

        public Task StoreAsync(
            string section,
            string text,
            IReadOnlyList<string>? tags = null,
            MemoryImportance? importance = null,
            CancellationToken cancellationToken = default)
        {
            StoredName = section;
            StoredText = text;
            StoredTags = tags;
            StoredImportance = importance;
            return Task.CompletedTask;
        }

        public Task<MemoryContainer> ReadAsync(string section, CancellationToken cancellationToken = default)
        {
            ReadSection = section;

            return ReadException is null
                ? Task.FromResult(ReadResult)
                : Task.FromException<MemoryContainer>(ReadException);
        }

        public Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecallResult);
        }

        public Task<IReadOnlyList<MemorySearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            SearchQuery = query;

            return SearchException is null
                ? Task.FromResult(SearchResult)
                : Task.FromException<IReadOnlyList<MemorySearchResult>>(SearchException);
        }
    }
}
