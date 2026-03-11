using EngramMcp.Core;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class JsonMemoryFileStoreTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

    private static JsonMemoryFileStore CreateStore(string filePath)
    {
        return new JsonMemoryFileStore(filePath, new CodeMemoryCatalog());
    }

    [Fact]
    public async Task EnsureInitializedAsync_CreatesMissingFileWithDefaultStructure()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);

        await store.EnsureInitializedAsync();
        var document = await store.LoadAsync();

        File.Exists(filePath).IsTrue();
        document.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["longTerm", "mediumTerm", "shortTerm"]).IsTrue();
        document.Memories["shortTerm"].Count.Is(0);
        document.Memories["mediumTerm"].Count.Is(0);
        document.Memories["longTerm"].Count.Is(0);
    }

    [Fact]
    public async Task LoadAsync_ThrowsForMalformedJson()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, "not json");
        var store = CreateStore(filePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());

        exception.Message.Contains("malformed JSON", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public async Task SaveAsync_PersistsTopLevelNameKeyedSections()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);
        await store.EnsureInitializedAsync();

        var document = new MemoryDocument
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["shortTerm"] = [new(new DateTime(2026, 3, 11, 15, 4, 5), "hello")],
                ["mediumTerm"] = [],
                ["longTerm"] = []
            }
        };

        await store.SaveAsync(document);

        var json = await File.ReadAllTextAsync(filePath);
        json.Contains("\"shortTerm\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"memories\"", StringComparison.Ordinal).IsFalse();
        json.Contains("\"timestamp\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"text\"", StringComparison.Ordinal).IsTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
