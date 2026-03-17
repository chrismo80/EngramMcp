using EngramMcp.Core;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Infrastructure.Memory;

public sealed class CodeMemoryCatalogTests
{
    [Theory]
    [InlineData(MemorySize.Small, 5)]
    [InlineData(MemorySize.Normal, 10)]
    [InlineData(MemorySize.Big, 20)]
    public void CodeMemoryCatalog_UsesConfiguredBaseCapacity(MemorySize size, int baseCapacity)
    {
        var catalog = new CodeMemoryCatalog(size);

        catalog.GetByName(ShortTerm).Capacity.Is(baseCapacity);
        catalog.GetByName(MediumTerm).Capacity.Is(baseCapacity * 2);
        catalog.GetByName(LongTerm).Capacity.Is(baseCapacity * 4);
        catalog.GetByName("project-x").Capacity.Is(baseCapacity * 4);
    }
}
