using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class RecallToolTests
{
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
}
