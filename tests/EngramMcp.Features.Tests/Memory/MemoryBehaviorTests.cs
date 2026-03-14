using EngramMcp.Core;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryBehaviorTests
{
    [Fact]
    public void MemoryEntry_RejectsMultilineText()
    {
        var exception = Assert.Throws<ArgumentException>(() => new MemoryEntry(new DateTime(2026, 3, 11, 8, 0, 0), "first\r\nsecond"));

        exception.Message.Is("Memory text must be a single line without carriage returns or line feeds. (Parameter 'text')");
    }

    [Fact]
    public void MemoryEntry_RejectsOverlyLongText()
    {
        var tooLongText = new string('a', 581);

        var exception = Assert.Throws<ArgumentException>(() => new MemoryEntry(new DateTime(2026, 3, 11, 8, 0, 0), tooLongText));

        exception.Message.Is("Memory text must be 500 characters or fewer. (Parameter 'text')");
    }

    [Fact]
    public void MemoryEntry_AcceptsValidSingleLineText()
    {
        var entry = new MemoryEntry(new DateTime(2026, 3, 11, 8, 0, 0), "valid memory");

        entry.Text.Is("valid memory");
    }

    [Fact]
    public void Store_AppendsEntry_AndEvictsOldestWithinSameImportanceWhenCapacityIsExceeded()
    {
        var section = new Core.MemorySection("shortTerm", 2);
        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] =
                [
                    new(new DateTime(2026, 3, 11, 8, 0, 0), "first"),
                    new(new DateTime(2026, 3, 11, 9, 0, 0), "second")
                ]
            }
        };

        section.Store(container, new MemoryEntry(new DateTime(2026, 3, 11, 10, 0, 0), "third"));

        var entries = section.Read(container);
        entries.Count.Is(2);
        entries[0].Text.Is("second");
        entries[1].Text.Is("third");
    }

    [Fact]
    public void Store_EvictsLowestImportanceBeforeOlderHigherImportance()
    {
        var section = new Core.MemorySection("shortTerm", 2);
        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] =
                [
                    new(new DateTime(2026, 3, 11, 8, 0, 0), "high", importance: MemoryImportance.High),
                    new(new DateTime(2026, 3, 11, 9, 0, 0), "low", importance: MemoryImportance.Low)
                ]
            }
        };

        section.Store(container, new MemoryEntry(new DateTime(2026, 3, 11, 10, 0, 0), "normal", importance: MemoryImportance.Normal));

        var entries = section.Read(container);
        entries.Count.Is(2);
        entries.Select(entry => entry.Text).ToArray().SequenceEqual(["high", "normal"]).IsTrue();
    }

    [Fact]
    public void Read_CreatesMissingSection_AndReturnsEmptyList()
    {
        var section = new Core.MemorySection("mediumTerm", 3);
        var container = new MemoryContainer();

        var entries = section.Read(container);

        entries.Count.Is(0);
        container.Memories.ContainsKey("mediumTerm").IsTrue();
    }
}
