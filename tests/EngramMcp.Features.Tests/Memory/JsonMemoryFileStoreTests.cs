using EngramMcp.Core;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Features.Tests.Memory;

public sealed class JsonMemoryStoreTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

    private static JsonMemoryStore CreateStore(string filePath)
    {
        return new JsonMemoryStore(filePath, new CodeMemoryCatalog());
    }

    [Fact]
    public async Task EnsureInitializedAsync_CreatesMissingFileWithDefaultStructure()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);

        await store.EnsureInitializedAsync();
        var container = await store.LoadAsync();

        File.Exists(filePath).IsTrue();
        container.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["long-term", "medium-term", "short-term"]).IsTrue();
        container.Memories["short-term"].Count.Is(0);
        container.Memories["medium-term"].Count.Is(0);
        container.Memories["long-term"].Count.Is(0);
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

        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                ["short-term"] = [new(new DateTime(2026, 3, 11, 15, 4, 5), "hello")],
                ["medium-term"] = [],
                ["long-term"] = []
            }
        };

        await store.SaveAsync(container);

        var json = await File.ReadAllTextAsync(filePath);
        json.Contains("\"short-term\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"memories\"", StringComparison.Ordinal).IsFalse();
        json.Contains("\"timestamp\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"text\"", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public async Task EnsureInitializedAsync_AndLoadAsync_AreSafeUnderConcurrentAccess()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);
        var operations = Enumerable.Range(0, 20)
            .Select(index => index % 2 == 0
                ? store.EnsureInitializedAsync()
                : (Task)store.LoadAsync())
            .ToArray();

        await Task.WhenAll(operations);

        var container = await store.LoadAsync();

        File.Exists(filePath).IsTrue();
        container.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual(["long-term", "medium-term", "short-term"]).IsTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
