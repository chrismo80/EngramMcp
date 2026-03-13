using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Tools;

public sealed class MemoryToolTests
{
    [Fact]
    public async Task StoreShortTermTool_DelegatesToSharedServiceWithShortTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreShortTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("short-term");
        service.StoredText.Is("remember this");
    }

    [Fact]
    public async Task StoreMediumTermTool_DelegatesToSharedServiceWithMediumTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMediumTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("medium-term");
    }

    [Fact]
    public async Task StoreLongTermTool_DelegatesToSharedServiceWithLongTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreLongTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("long-term");
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

        await tool.ExecuteAsync("long-term", "remember this", CancellationToken.None);

        service.StoredName.Is("long-term");
    }

    [Fact]
    public async Task RecallTool_ReturnsMarkdownWithOrderedSectionsAndNoTimestamps()
    {
        var expected = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["long-term"] = [new MemoryEntry(new DateTime(2026, 3, 11, 13, 0, 0), "long")],
                ["medium-term"] = [],
                ["short-term"] = [new MemoryEntry(new DateTime(2026, 3, 11, 12, 0, 0), "short")],
            }
        };

        var service = new SpyMemoryService { RecallResult = expected };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        result.IsNotEmpty();

        Assert.True(
            result.IndexOf("## long-term", StringComparison.Ordinal) < result.IndexOf("## short-term", StringComparison.Ordinal),
            "Expected long-term section to appear before short-term section.");
    }

    private sealed class SpyMemoryService : IMemoryService
    {
        public string? StoredName { get; private set; }

        public string? StoredText { get; private set; }

        public MemoryContainer RecallResult { get; init; } = new();

        public Task StoreAsync(string section, string text, CancellationToken cancellationToken = default)
        {
            StoredName = section;
            StoredText = text;
            return Task.CompletedTask;
        }

        public Task<MemoryContainer> RecallAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecallResult);
        }
    }
}
