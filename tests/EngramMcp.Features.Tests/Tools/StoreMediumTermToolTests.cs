using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class StoreMediumTermToolTests
{
    [Fact]
    public async Task StoreMediumTermTool_DelegatesToSharedServiceWithMediumTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMediumTermTool(service);

        await tool.ExecuteAsync("remember this", cancellationToken: CancellationToken.None);

        service.StoredName.Is(MediumTerm);
    }
}
