using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class ReadSectionToolTests
{
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
}
