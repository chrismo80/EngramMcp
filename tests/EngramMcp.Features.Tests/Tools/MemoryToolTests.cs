using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Features.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Tools;

public sealed class MemoryToolTests
{
    [Fact]
    public async Task StoreShortTermTool_DelegatesToSharedServiceWithShortTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreShortTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("shortTerm");
        service.StoredText.Is("remember this");
    }

    [Fact]
    public async Task StoreMediumTermTool_DelegatesToSharedServiceWithMediumTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreMediumTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("mediumTerm");
    }

    [Fact]
    public async Task StoreLongTermTool_DelegatesToSharedServiceWithLongTermName()
    {
        var service = new SpyMemoryService();
        var tool = new StoreLongTermTool(service);

        await tool.ExecuteAsync("remember this", CancellationToken.None);

        service.StoredName.Is("longTerm");
    }

    [Fact]
    public async Task RecallTool_ReturnsRawStructuredDocument()
    {
        var expected = new MemoryDocument
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] = [new(new DateTime(2026, 3, 11, 12, 0, 0), "short")],
                ["mediumTerm"] = [],
                ["longTerm"] = []
            }
        };

        var service = new SpyMemoryService { RecallResult = expected };
        var tool = new RecallTool(service);

        var result = await tool.ExecuteAsync(CancellationToken.None);

        ReferenceEquals(result, expected).IsTrue();
    }

    private sealed class SpyMemoryService : IMemoryService
    {
        public string? StoredName { get; private set; }

        public string? StoredText { get; private set; }

        public MemoryDocument RecallResult { get; init; } = new();

        public Task StoreAsync(string memoryName, string text, CancellationToken cancellationToken = default)
        {
            StoredName = memoryName;
            StoredText = text;
            return Task.CompletedTask;
        }

        public Task<MemoryDocument> RecallAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RecallResult);
        }
    }
}
