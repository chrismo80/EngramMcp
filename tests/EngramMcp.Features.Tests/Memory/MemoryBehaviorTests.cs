using EngramMcp.Core;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryBehaviorTests
{
    [Fact]
    public void Store_AppendsEntry_AndEvictsOldestWhenCapacityIsExceeded()
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
    public void Read_CreatesMissingSection_AndReturnsEmptyList()
    {
        var section = new Core.MemorySection("mediumTerm", 3);
        var container = new MemoryContainer();

        var entries = section.Read(container);

        entries.Count.Is(0);
        container.Memories.ContainsKey("mediumTerm").IsTrue();
    }
}
