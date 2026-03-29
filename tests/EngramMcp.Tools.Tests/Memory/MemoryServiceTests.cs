using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Identity;
using EngramMcp.Tools.Memory.Retention;
using EngramMcp.Tools.Memory.Session;
using EngramMcp.Tools.Memory.Storage;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task RecallAsync_decays_and_deletes_before_returning_memories()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "keep", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "drop", Retention = 1 }
            ]
        });
        var service = CreateService(store);

        var memories = await service.RecallAsync();

        memories.Count.Is(1);
        memories[0].Id.Is("a");
        memories[0].Text.Is("keep");
        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Retention.Is(9d);
    }

    [Fact]
    public async Task RememberAsync_creates_memory_with_tier_specific_initial_retention()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var service = CreateService(store, new FixedMemoryIdGenerator("260329142501"));

        await service.RememberAsync(RetentionTier.Medium, "Remember this");

        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Id.Is("260329142501");
        store.Document.Memories[0].Text.Is("Remember this");
        store.Document.Memories[0].Retention.Is(10d);
    }

    [Fact]
    public async Task ReinforceAsync_rejects_unknown_ids_atomically()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "known", Text = "Known memory", Retention = 10 }
            ]
        });
        var service = CreateService(store);

        var exception = await Record.ExceptionAsync(() => service.ReinforceAsync(["known", "missing"]));

        exception.IsNotNull();
        exception.Is<KeyNotFoundException>();
        store.Document.Memories[0].Retention.Is(10d);
    }

    [Fact]
    public async Task ReinforceAsync_ignores_second_reinforcement_in_same_session()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "known", Text = "Known memory", Retention = 10 }
            ]
        });
        var service = CreateService(store);

        await service.ReinforceAsync(["known"]);
        await service.ReinforceAsync(["known"]);

        store.Document.Memories[0].Retention.Is(11d);
    }

    [Fact]
    public async Task RecallAsync_returns_only_id_and_text_shape()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "id-1", Text = "Known memory", Retention = 10 }
            ]
        });
        var service = CreateService(store);

        var memories = await service.RecallAsync();

        memories.Single().Is(new RecallMemory("id-1", "Known memory"));
    }

    private static MemoryService CreateService(InMemoryMemoryStore store, IMemoryIdGenerator? memoryIdGenerator = null)
    {
        return new MemoryService(
            store,
            memoryIdGenerator ?? new FixedMemoryIdGenerator("generated-id"),
            new DefaultRetentionPolicy(),
            new SessionReinforcementTracker());
    }

    private sealed class InMemoryMemoryStore(PersistedMemoryDocument document) : EngramMcp.Tools.Memory.Storage.IMemoryStore
    {
        public PersistedMemoryDocument Document { get; private set; } = document;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Document);

        public Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedMemoryIdGenerator(string id) : IMemoryIdGenerator
    {
        public string CreateId(IReadOnlyCollection<string> existingIds, DateTime now) => id;
    }
}
