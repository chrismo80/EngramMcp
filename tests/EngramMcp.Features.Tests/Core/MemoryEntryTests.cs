using EngramMcp.Core;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Core;

public sealed class MemoryEntryTests
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
}
