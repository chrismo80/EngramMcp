using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Tools;

public sealed class SearchToolTests
{
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
}
