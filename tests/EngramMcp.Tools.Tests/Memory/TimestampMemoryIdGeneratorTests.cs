using EngramMcp.Tools.Memory.Identity;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class TimestampMemoryIdGeneratorTests
{
    [Fact]
    public void CreateId_adds_suffix_when_base_id_already_exists()
    {
        var generator = new TimestampMemoryIdGenerator();
        var now = new DateTime(2026, 3, 29, 14, 25, 1);

        var id = generator.CreateId(["260329142501", "260329142501-2"], now);

        id.Is("260329142501-3");
    }
}
