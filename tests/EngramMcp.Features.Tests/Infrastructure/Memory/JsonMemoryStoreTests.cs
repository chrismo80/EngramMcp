using EngramMcp.Core;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using System.Text.Json;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Infrastructure.Memory;

public sealed class JsonMemoryStoreTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

    private static JsonMemoryStore CreateStore(string filePath)
    {
        return new JsonMemoryStore(filePath, new CodeMemoryCatalog(MemorySize.Normal));
    }

    [Fact]
    public async Task EnsureInitializedAsync_CreatesMissingFileWithDefaultStructure()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);

        await store.EnsureInitializedAsync();
        var container = await store.LoadAsync();

        File.Exists(filePath).IsTrue();
        container.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
        container.Memories[ShortTerm].Count.Is(0);
        container.Memories[MediumTerm].Count.Is(0);
        container.Memories[LongTerm].Count.Is(0);
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
    public async Task LoadAsync_LoadsLegacyThreeBucketFiles()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [],
              "{{MediumTerm}}": [],
              "{{ShortTerm}}": []
            }
            """);

        var store = CreateStore(filePath);
        var container = await store.LoadAsync();

        container.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
    }

    [Fact]
    public async Task LoadAsync_LoadsLegacyEntriesWithoutMetadata()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [
                {
                  "timestamp": "2026-03-11T15:04:05",
                  "text": "hello"
                }
              ],
              "{{MediumTerm}}": [],
              "{{ShortTerm}}": []
            }
            """);

        var store = CreateStore(filePath);

        var container = await store.LoadAsync();

        var entry = container.Memories[LongTerm][0];
        entry.Text.Is("hello");
        entry.Importance.Is(MemoryImportance.Normal);
    }

    [Fact]
    public async Task LoadAsync_IgnoresLegacyTagsMetadata()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [
                {
                  "timestamp": "2026-03-11T15:04:05",
                  "text": "hello",
                  "tags": ["project-x", "research"],
                  "importance": "high"
                }
              ],
              "{{MediumTerm}}": [],
              "{{ShortTerm}}": []
            }
            """);

        var store = CreateStore(filePath);

        var container = await store.LoadAsync();

        var entry = container.Memories[LongTerm][0];
        entry.Text.Is("hello");
        entry.Importance.Is(MemoryImportance.High);
    }

    [Fact]
    public async Task LoadAsync_ThrowsWhenRequiredSectionIsMissing()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [],
              "{{MediumTerm}}": []
            }
            """);

        var store = CreateStore(filePath);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());

        exception.Message.Contains($"Missing required section '{ShortTerm}'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public async Task LoadAsync_ThrowsWhenSectionIsNotAnArray()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [],
              "{{MediumTerm}}": null,
              "{{ShortTerm}}": []
            }
            """);

        var store = CreateStore(filePath);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());

        exception.Message.Contains($"Section '{MediumTerm}' must be an array", StringComparison.Ordinal).IsTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadAsync_ThrowsWhenCustomSectionNameIsEmptyOrWhitespace(string sectionName)
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "memory.json");
        var escapedSectionName = JsonSerializer.Serialize(sectionName);
        await File.WriteAllTextAsync(filePath, $$"""
            {
              "{{LongTerm}}": [],
              "{{MediumTerm}}": [],
              "{{ShortTerm}}": [],
              {{escapedSectionName}}: []
            }
            """);

        var store = CreateStore(filePath);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadAsync());

        exception.Message.Contains("Section names must not be empty or whitespace", StringComparison.Ordinal).IsTrue();
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
                [ShortTerm] = [new(new DateTime(2026, 3, 11, 15, 4, 5), "hello")],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        };

        await store.SaveAsync(container);

        var json = await File.ReadAllTextAsync(filePath);
        json.Contains($"\"{ShortTerm}\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"memories\"", StringComparison.Ordinal).IsFalse();
        json.Contains("\"timestamp\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"text\"", StringComparison.Ordinal).IsTrue();
        json.Contains("\"tags\"", StringComparison.Ordinal).IsFalse();
        json.Contains("\"importance\"", StringComparison.Ordinal).IsFalse();
    }

    [Fact]
    public async Task SaveAsync_AllowsAdditionalCustomSections()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);
        await store.EnsureInitializedAsync();

        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = [],
                ["project-x"] = [new(new DateTime(2026, 3, 11, 15, 4, 5), "hello")]
            }
        };

        await store.SaveAsync(container);
        var loaded = await store.LoadAsync();

        loaded.Memories.ContainsKey("project-x").IsTrue();
        loaded.Memories["project-x"][0].Text.Is("hello");
    }

    [Fact]
    public async Task SaveAsync_PersistsImportanceWithoutTags()
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);
        await store.EnsureInitializedAsync();

        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] =
                [
                    new(
                        new DateTime(2026, 3, 11, 15, 4, 5),
                        "hello",
                        MemoryImportance.High)
                ],
                [MediumTerm] = [],
                [LongTerm] = []
            }
        };

        await store.SaveAsync(container);

        var loaded = await store.LoadAsync();
        var json = await File.ReadAllTextAsync(filePath);
        var entry = loaded.Memories[ShortTerm][0];

        entry.Importance.Is(MemoryImportance.High);
        json.Contains("\"tags\"", StringComparison.Ordinal).IsFalse();
        json.Contains("\"importance\": \"high\"", StringComparison.Ordinal).IsTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_ThrowsWhenCustomSectionNameIsEmptyOrWhitespace(string sectionName)
    {
        var filePath = Path.Combine(_rootPath, "memory.json");
        var store = CreateStore(filePath);
        await store.EnsureInitializedAsync();

        var container = new MemoryContainer
        {
            Memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal)
            {
                [ShortTerm] = [],
                [MediumTerm] = [],
                [LongTerm] = [],
                [sectionName] = []
            }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(container));

        exception.Message.Contains("Section names must not be empty or whitespace", StringComparison.Ordinal).IsTrue();
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
        container.Memories.Keys.OrderBy(key => key).ToArray().SequenceEqual([LongTerm, MediumTerm, ShortTerm]).IsTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
