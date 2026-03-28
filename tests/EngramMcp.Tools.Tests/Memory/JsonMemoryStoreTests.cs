using System.Text.Json;
using EngramMcp.Tools.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class JsonMemoryStoreTests
{
    [Fact]
    public async Task EnsureInitializedAsync_creates_memory_file_with_built_in_sections()
    {
        using var memoryFile = new TemporaryMemoryFile();
        var store = new JsonMemoryStore(memoryFile.FilePath, new MemoryCatalog(MemorySize.Small));

        await store.EnsureInitializedAsync();

        File.Exists(memoryFile.FilePath).IsTrue();

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(memoryFile.FilePath));
        document.RootElement.TryGetProperty(BuiltInMemorySections.LongTerm, out _).IsTrue();
        document.RootElement.TryGetProperty(BuiltInMemorySections.MediumTerm, out _).IsTrue();
        document.RootElement.TryGetProperty(BuiltInMemorySections.ShortTerm, out _).IsTrue();
    }

    [Fact]
    public async Task UpdateAsync_persists_memory_entries_to_json_file()
    {
        using var memoryFile = new TemporaryMemoryFile();
        var store = new JsonMemoryStore(memoryFile.FilePath, new MemoryCatalog(MemorySize.Small));

        await store.UpdateAsync(container =>
        {
            container.Memories[BuiltInMemorySections.LongTerm].Add(new MemoryEntry(
                new DateTime(2026, 3, 28, 10, 15, 30, DateTimeKind.Local),
                "Durable fact",
                MemoryImportance.High));
        });

        var json = await File.ReadAllTextAsync(memoryFile.FilePath);

        json.Contains("Durable fact", StringComparison.Ordinal).IsTrue();
        json.Contains("\"importance\": \"high\"", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public async Task LoadAsync_reads_existing_custom_section_from_json_file()
    {
        using var memoryFile = new TemporaryMemoryFile();

        await File.WriteAllTextAsync(memoryFile.FilePath, """
        {
          "long-term": [],
          "medium-term": [],
          "short-term": [],
          "project-x": [
            {
              "timestamp": "2026-03-28T10:15:30.0000000+01:00",
              "text": "Remember project detail",
              "importance": "high"
            }
          ]
        }
        """);

        var store = new JsonMemoryStore(memoryFile.FilePath, new MemoryCatalog(MemorySize.Small));

        var container = await store.LoadAsync();

        container.Memories.ContainsKey("project-x").IsTrue();
        container.Memories["project-x"].Count.Is(1);
        container.Memories["project-x"][0].Text.Is("Remember project detail");
        container.Memories["project-x"][0].Importance.Is(MemoryImportance.High);
    }
}
