using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task StoreAsync_RejectsWhitespaceOnlyText()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryFileStore(new MemoryDocument
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] = [],
                ["mediumTerm"] = [],
                ["longTerm"] = []
            }
        }));

        await Assert.ThrowsAsync<ArgumentException>(() => service.StoreAsync("shortTerm", "   "));
    }

    [Fact]
    public async Task StoreAsync_AllowsDuplicates_AndUsesTargetMemoryOnly()
    {
        var fileStore = new InMemoryFileStore(new MemoryDocument
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] = [],
                ["mediumTerm"] = [new(new DateTime(2026, 3, 11, 9, 0, 0), "existing")],
                ["longTerm"] = []
            }
        });

        var service = new MemoryService(new CodeMemoryCatalog(), fileStore);

        await service.StoreAsync("shortTerm", "duplicate");
        await service.StoreAsync("shortTerm", "duplicate");

        fileStore.Document.Memories["shortTerm"].Count.Is(2);
        fileStore.Document.Memories["shortTerm"][0].Text.Is("duplicate");
        fileStore.Document.Memories["shortTerm"][1].Text.Is("duplicate");
        fileStore.Document.Memories["mediumTerm"].Count.Is(1);
    }

    [Fact]
    public async Task StoreAsync_SerializesConcurrentUpdatesAgainstJsonFileStore()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            var filePath = Path.Combine(rootPath, "memory.json");
            var catalog = new CodeMemoryCatalog();
            var fileStore = new JsonMemoryFileStore(filePath, catalog);
            var service = new MemoryService(catalog, fileStore);
            var operations = Enumerable.Range(1, 10)
                .SelectMany(index => new[]
                {
                    service.StoreAsync("shortTerm", $"short-{index}"),
                    service.StoreAsync("mediumTerm", $"medium-{index}"),
                    service.StoreAsync("longTerm", $"long-{index}")
                })
                .ToArray();

            await Task.WhenAll(operations);

            var recalled = await service.RecallAsync();

            recalled.Memories["shortTerm"].Count.Is(10);
            recalled.Memories["mediumTerm"].Count.Is(10);
            recalled.Memories["longTerm"].Count.Is(10);
            recalled.Memories["shortTerm"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"short-{index}").OrderBy(text => text)).IsTrue();
            recalled.Memories["mediumTerm"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"medium-{index}").OrderBy(text => text)).IsTrue();
            recalled.Memories["longTerm"].Select(entry => entry.Text).OrderBy(text => text).ToArray()
                .SequenceEqual(Enumerable.Range(1, 10).Select(index => $"long-{index}").OrderBy(text => text)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RecallAsync_ReturnsConfiguredSectionsSeparatedByName()
    {
        var service = new MemoryService(new CodeMemoryCatalog(), new InMemoryFileStore(new MemoryDocument
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] = [new(new DateTime(2026, 3, 11, 10, 0, 0), "short")],
                ["mediumTerm"] = [],
                ["longTerm"] = [new(new DateTime(2026, 3, 11, 11, 0, 0), "long")]
            }
        }));

        var recalled = await service.RecallAsync();

        recalled.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["longTerm", "mediumTerm", "shortTerm"]).IsTrue();
        recalled.Memories["shortTerm"][0].Text.Is("short");
        recalled.Memories["longTerm"][0].Text.Is("long");
    }

    private sealed class InMemoryFileStore(MemoryDocument document) : IMemoryFileStore
    {
        public MemoryDocument Document { get; private set; } = document;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(Action<MemoryDocument> update, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);

            var document = Clone(Document);
            update(document);
            Document = document;
            return Task.CompletedTask;
        }

        public Task<MemoryDocument> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Clone(Document));
        }

        public Task SaveAsync(MemoryDocument document, CancellationToken cancellationToken = default)
        {
            Document = Clone(document);
            return Task.CompletedTask;
        }

        private static MemoryDocument Clone(MemoryDocument document)
        {
            return new MemoryDocument
            {
                Memories = document.Memories.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Select(entry => new MemoryEntry(entry.Timestamp, entry.Text)).ToList(),
                    StringComparer.Ordinal)
            };
        }
    }
}
