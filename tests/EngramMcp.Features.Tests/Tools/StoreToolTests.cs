using EngramMcp.Core;
using EngramMcp.Features.Tests.TestDoubles;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Tools;

public sealed class StoreToolTests
{
    [Fact]
    public async Task StoreTool_DelegatesToSharedServiceWithProvidedSection()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        await tool.ExecuteAsync("project-x", "remember this", "low", CancellationToken.None);

        service.StoredName.Is("project-x");
        service.StoredText.Is("remember this");
        service.StoredImportance.Is(MemoryImportance.Low);
    }

    [Fact]
    public async Task StoreTool_AllowsBuiltInSectionNames()
    {
        var service = new SpyMemoryService();
        var tool = new StoreTool(service);

        await tool.ExecuteAsync(LongTerm, "remember this", cancellationToken: CancellationToken.None);

        service.StoredName.Is(LongTerm);
    }
}
