using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class StoreShortTermToolTests
{
    [Fact]
    public async Task StoreShortTermTool_DelegatesToSharedServiceWithShortTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreShortTermTool(service);

        await tool.ExecuteAsync("remember this", importance: "high", cancellationToken: CancellationToken.None);

        service.StoredName.Is(ShortTerm);
        service.StoredText.Is("remember this");
        service.StoredImportance.Is(MemoryImportance.High);
    }
}
