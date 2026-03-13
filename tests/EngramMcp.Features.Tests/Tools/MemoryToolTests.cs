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

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is(ShortTerm);
        service.StoredText.Is("remember this");
    }

    [Fact]
    public async Task StoreMediumTermTool_DelegatesToSharedServiceWithMediumTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMediumTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is(MediumTerm);
    }

    [Fact]
    public async Task StoreLongTermTool_DelegatesToSharedServiceWithLongTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreLongTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is(LongTerm);
    }

    [Fact]
    public async Task StoreMemoryTool_DelegatesToSharedServiceWithProvidedBucket()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMemoryTool(service);

        await tool.ExecuteAsync("project-x", "remember this", CancellationToken.None);

        service.StoredName.Is("project-x");
        service.StoredText.Is("remember this");
    }

    [Fact]
    public async Task StoreMemoryTool_AllowsBuiltInBucketNames()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMemoryTool(service);

        await tool.ExecuteAsync(LongTerm, "remember this", CancellationToken.None);

        service.StoredName.Is(LongTerm);
    }

    [Fact]
    public async Task RecallTool_ReturnsMarkdownWithOrderedSectionsAndNoTimestamps()
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

        result.IsNotEmpty();

        Assert.True(
            result.IndexOf($"## {LongTerm}", StringComparison.Ordinal) < result.IndexOf($"## {ShortTerm}", StringComparison.Ordinal),
            $"Expected {LongTerm} section to appear before {ShortTerm} section.");
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

        result.Contains("## Custom Sections", StringComparison.Ordinal).IsFalse();
    }

    [Fact]
    public async Task RecallTool_AppendsCustomSectionListingSortedByDescendingEntryCount()
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

        result.Is(
            "# Memory\r\n" +
            $"## {LongTerm}\r\n" +
            "\r\n" +
            $"## {MediumTerm}\r\n" +
            "\r\n" +
            $"## {ShortTerm}\r\n" +
            "\r\n" +
            "## Custom Sections\r\n" +
            "- project-large (4)\r\n" +
            "- project-medium (2)\r\n" +
            "- project-small (1)\r\n");
    }

    [Fact]
    public async Task ReadMemoryTool_ReturnsMarkdownForBuiltInSectionOnly()
    {
        var service = new SpyMemoryService
        {
            ReadResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [ShortTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short")]
                }
            }
        };
        var tool = new ReadMemoryTool(service);

        var result = await tool.ExecuteAsync(ShortTerm, CancellationToken.None);

        service.ReadSection.Is(ShortTerm);
        result.Is($"# Memory\r\n## {ShortTerm}\r\n- short\r\n");
    }

    [Fact]
    public async Task ReadMemoryTool_ReturnsMarkdownForCustomSectionOnly()
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
        var tool = new ReadMemoryTool(service);

        var result = await tool.ExecuteAsync("project-x", CancellationToken.None);

        service.ReadSection.Is("project-x");
        result.Is("# Memory\r\n## project-x\r\n- custom\r\n");
    }

    [Fact]
    public async Task ReadMemoryTool_PropagatesMissingSectionFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new KeyNotFoundException($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.")
        };
        var tool = new ReadMemoryTool(service);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => tool.ExecuteAsync("project-x", CancellationToken.None));

        exception.Message.Is($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.");
    }

    [Fact]
    public async Task SearchMemoriesTool_ReturnsHumanReadableResultsWithSectionContext()
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
        var tool = new SearchMemoriesTool(service);

        var result = await tool.ExecuteAsync("docker", CancellationToken.None);

        service.SearchQuery.Is("docker");
        result.Is(
            "# Memory Search Results\r\n" +
            "- docker reminder (`project-x`)\r\n");
    }

    [Fact]
    public async Task SearchMemoriesTool_ReturnsNoMatchesMessage()
    {
        var service = new SpyMemoryService();
        var tool = new SearchMemoriesTool(service);

        var result = await tool.ExecuteAsync("missing", CancellationToken.None);

        result.Is("# Memory Search Results\r\nNo matches found.\r\n");
    }

    [Fact]
    public async Task SearchMemoriesTool_EmitsOneLinePerResultWithoutTextFlatteningLogic()
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
        var tool = new SearchMemoriesTool(service);

        var result = await tool.ExecuteAsync("docker", CancellationToken.None);

        result.Is(
            "# Memory Search Results\r\n" +
            "- docker reminder (`project-x`)\r\n" +
            "- workspace note (`project-y`)\r\n");
    }

    [Fact]
    public async Task SearchMemoriesTool_PropagatesInvalidQueryFailure()
    {
        var service = new SpyMemoryService
        {
            SearchException = new ArgumentException("Search query must not be null, empty, or whitespace.", "query")
        };
        var tool = new SearchMemoriesTool(service);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("   ", CancellationToken.None));

        exception.Message.Is("Search query must not be null, empty, or whitespace. (Parameter 'query')");
    }

    [Fact]
    public async Task RecallTool_RemainsUnchanged()
    {
        var service = new SpyMemoryService
        {
            RecallResult = new MemoryContainer
            {
                Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
                {
                    [LongTerm] = [],
                    [MediumTerm] = [],
                    [ShortTerm] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short")]
                }
            }
        };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.Is($"# Memory\r\n## {LongTerm}\r\n\r\n## {MediumTerm}\r\n\r\n## {ShortTerm}\r\n- short\r\n");
    }

    private sealed class SpyMemoryService : IMemoryService
    {
        public string? StoredName { get; private set; }

        public string? StoredText { get; private set; }

        public string? ReadSection { get; private set; }

        public string? SearchQuery { get; private set; }

        public MemoryContainer RecallResult { get; init; } = new();

        public MemoryContainer ReadResult { get; init; } = new();

        public IReadOnlyList<MemorySearchResult> SearchResult { get; init; } = [];

        public Exception? ReadException { get; init; }

        public Exception? SearchException { get; init; }

        public Task StoreAsync(string section, string text, CancellationToken cancellationToken = default)
        {
            StoredName = section;
            StoredText = text;
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
