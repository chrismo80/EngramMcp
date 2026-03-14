using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class MemoryToolTests
{
    private static readonly string NewLine = Environment.NewLine;

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
    public async Task StoreTool_OmittedImportance_PreservesServiceDefault()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        await tool.ExecuteAsync("project-x", "remember this", cancellationToken: CancellationToken.None);

        service.StoredImportance.IsNull();
    }

    [Fact]
    public async Task StoreTool_InvalidImportance_ThrowsHelpfulValidationError()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => tool.ExecuteAsync("project-x", "remember this", importance: "urgent", cancellationToken: CancellationToken.None));

        exception.Message.Is("Invalid importance 'urgent'. Allowed values: low, normal, high. (Parameter 'importance')");
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
            $"# Memory{NewLine}" +
            $"## {LongTerm}{NewLine}" +
            NewLine +
            $"## {MediumTerm}{NewLine}" +
            NewLine +
            $"## {ShortTerm}{NewLine}" +
            NewLine +
            $"## Custom Sections{NewLine}" +
            $"- project-large (4){NewLine}" +
            $"- project-medium (2){NewLine}" +
            $"- project-small (1){NewLine}");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsMarkdownForBuiltInSectionOnly()
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

        service.ReadSection.Is(ShortTerm);
        result.Is($"# Memory{NewLine}## {ShortTerm}{NewLine}- short [tags: ops, todo]{NewLine}");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsMarkdownForCustomSectionOnly()
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

        service.ReadSection.Is("project-x");
        result.Is($"# Memory{NewLine}## project-x{NewLine}- custom{NewLine}");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsHumanReadableMissingSectionFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new KeyNotFoundException($"Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.")
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("project-x", CancellationToken.None);

        result.Is($"# Memory Section Error\r\nSection not found. Memory section 'project-x' was not found. Available sections: {LongTerm}, {MediumTerm}, {ShortTerm}, project-a.\r\n");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsHumanReadableInvalidSectionFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new ArgumentException("Memory section identifier must not be null, empty, or whitespace.", "section")
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("   ", CancellationToken.None);

        result.Is("# Memory Section Error\r\nInvalid section identifier. Provide a non-empty section name.\r\n");
    }

    [Fact]
    public async Task ReadSectionTool_ReturnsHumanReadableInternalFailure()
    {
        var service = new SpyMemoryService
        {
            ReadException = new InvalidOperationException("disk unavailable")
        };
        var tool = new ReadSectionTool(service);

        var result = await tool.ExecuteAsync("project-x", CancellationToken.None);

        result.Is("# Memory Section Error\r\nInternal failure. Unable to read the requested memory section right now.\r\n");
    }

    [Fact]
    public async Task SearchTool_ReturnsHumanReadableResultsWithSectionContext()
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
        result.Is(
            $"# Memory Search Results{NewLine}" +
            $"- docker reminder (`project-x`) [tags: ops]{NewLine}");
    }

    [Fact]
    public async Task SearchTool_ReturnsNoMatchesMessage()
    {
        var service = new SpyMemoryService();
        var tool = new SearchTool(service);

        var result = await tool.ExecuteAsync("missing", CancellationToken.None);

        result.Is($"# Memory Search Results{NewLine}No matches found.{NewLine}");
    }

    [Fact]
    public async Task SearchTool_EmitsOneLinePerResultWithoutTextFlatteningLogic()
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

        result.Is(
            $"# Memory Search Results{NewLine}" +
            $"- docker reminder (`project-x`) [tags: ops]{NewLine}" +
            $"- workspace note (`project-y`) [tags: dev]{NewLine}");
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

        result.Is($"# Memory{NewLine}## {LongTerm}{NewLine}{NewLine}## {MediumTerm}{NewLine}{NewLine}## {ShortTerm}{NewLine}- short{NewLine}");
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
