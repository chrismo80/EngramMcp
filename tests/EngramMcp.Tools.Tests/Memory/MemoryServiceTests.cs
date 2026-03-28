using EngramMcp.Tools.Memory;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task StoreAsync_persists_entry_in_requested_custom_section()
    {
        using var memoryFile = new TemporaryMemoryFile();
        var catalog = new MemoryCatalog(MemorySize.Small);
        var store = new JsonMemoryStore(memoryFile.FilePath, catalog);
        var service = new MemoryService(catalog, store);

        await service.StoreAsync("project-x", "Remember project detail", MemoryImportance.High);

        var section = await service.ReadAsync("project-x");

        section.Memories["project-x"].Count.Is(1);
        section.Memories["project-x"][0].Text.Is("Remember project detail");
        section.Memories["project-x"][0].Importance.Is(MemoryImportance.High);
    }

    [Fact]
    public async Task RecallAsync_returns_built_in_sections_and_custom_section_summary()
    {
        using var memoryFile = new TemporaryMemoryFile();
        var catalog = new MemoryCatalog(MemorySize.Small);
        var store = new JsonMemoryStore(memoryFile.FilePath, catalog);
        var service = new MemoryService(catalog, store);

        await service.StoreAsync(BuiltInMemorySections.LongTerm, "Durable fact", MemoryImportance.High);
        await service.StoreAsync("project-x", "Project fact", MemoryImportance.Normal);

        var recalled = await service.RecallAsync();

        recalled.Memories.ContainsKey(BuiltInMemorySections.LongTerm).IsTrue();
        recalled.Memories[BuiltInMemorySections.LongTerm].Count.Is(1);
        recalled.Memories[BuiltInMemorySections.LongTerm][0].Text.Is("Durable fact");
        recalled.CustomSections.Count.Is(1);
        recalled.CustomSections[0].Name.Is("project-x");
        recalled.CustomSections[0].EntryCount.Is(1);
    }

    [Fact]
    public async Task StoreAsync_treats_built_in_section_names_case_insensitively()
    {
        using var memoryFile = new TemporaryMemoryFile();
        var catalog = new MemoryCatalog(MemorySize.Small);
        var store = new JsonMemoryStore(memoryFile.FilePath, catalog);
        var service = new MemoryService(catalog, store);

        await service.StoreAsync("LONG-TERM", "Durable fact", MemoryImportance.Normal);

        var section = await service.ReadAsync(BuiltInMemorySections.LongTerm);

        section.Memories.ContainsKey(BuiltInMemorySections.LongTerm).IsTrue();
        section.Memories[BuiltInMemorySections.LongTerm].Count.Is(1);
        section.Memories[BuiltInMemorySections.LongTerm][0].Text.Is("Durable fact");
    }
}
