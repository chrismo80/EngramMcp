using EngramMcp.Core;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryBehaviorTests
{
    [Fact]
    public void Store_AppendsEntry_AndEvictsOldestWhenCapacityIsExceeded()
    {
        var memory = new Core.Memory("shortTerm", 2);
        var document = new MemoryDocument
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

        memory.Store(document, new MemoryEntry(new DateTime(2026, 3, 11, 10, 0, 0), "third"));

        var entries = memory.Read(document);
        entries.Count.Is(2);
        entries[0].Text.Is("second");
        entries[1].Text.Is("third");
    }

    [Fact]
    public void Read_CreatesMissingSection_AndReturnsEmptyList()
    {
        var memory = new Core.Memory("mediumTerm", 3);
        var document = new MemoryDocument();

        var entries = memory.Read(document);

        entries.Count.Is(0);
        document.Memories.ContainsKey("mediumTerm").IsTrue();
    }
}
