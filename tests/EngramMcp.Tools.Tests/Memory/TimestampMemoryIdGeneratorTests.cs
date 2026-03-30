using EngramMcp.Tools.Memory.Identity;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class TimestampMemoryIdGeneratorTests
{
    [Fact]
    public void GetUniqueId_returns_unique_ids_for_consecutive_calls()
    {
        var generator = new TimestampMemoryIdGenerator();
        var ids = Enumerable.Range(0, 100)
            .Select(_ => generator.GetUniqueId())
            .ToArray();

        ids.Distinct(StringComparer.Ordinal).Count().Is(100);
    }

    [Fact]
    public async Task GetUniqueId_returns_unique_ids_for_parallel_calls()
    {
        var generator = new TimestampMemoryIdGenerator();
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(generator.GetUniqueId))
            .ToArray();

        var ids = await Task.WhenAll(tasks);

        ids.Distinct(StringComparer.Ordinal).Count().Is(100);
    }
}
