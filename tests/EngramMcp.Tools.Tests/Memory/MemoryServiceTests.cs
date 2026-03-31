using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Storage;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Memory;

public sealed class MemoryServiceTests
{
    [Fact]
    public async Task RecallAsync_prunes_deleteable_memories_without_decay()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "keep", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "drop", Retention = 0.9 }
            ]
        });
        var service = CreateService(store);

        var memories = await service.RecallAsync();

        memories.Count.Is(1);
        memories[0].Id.Is("a");
        memories[0].Text.Is("keep");
        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Retention.Is(10d);
    }

    [Fact]
    public async Task RememberAsync_creates_memory_with_tier_specific_initial_retention()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var service = CreateService(store);

        var result = await service.RememberAsync(RetentionTier.Medium, "Remember this");

        result.Succeeded.IsTrue();
        result.Rejection.IsNull();
        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Text.Is("Remember this");
        store.Document.Memories[0].Retention.Is(25d);
    }

    [Fact]
    public async Task RememberAsync_returns_validation_message_for_invalid_text()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var service = CreateService(store);

        var result = await service.RememberAsync(RetentionTier.Short, "");

        result.Succeeded.IsFalse();
        result.Rejection.Is("Memory text must not be null, empty, or whitespace.");
        store.Document.Memories.IsEmpty();
    }

    [Fact]
    public async Task RecallAsync_keeps_the_loaded_document_for_the_service_lifetime()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var service = CreateService(store);

        (await service.RecallAsync()).IsEmpty();

        store.Replace(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "id-1", Text = "Updated outside the service", Retention = 10 }
            ]
        });

        var memories = await service.RecallAsync();

        memories.IsEmpty();
    }

    [Fact]
    public async Task ReinforceAsync_rejects_unknown_ids_atomically_without_consuming_cycle_weakening()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "known", Text = "Known memory", Retention = 10 }
            ]
        });
        var service = CreateService(store);

        var rejected = await service.ReinforceAsync(["known", "missing"]);
        var accepted = await service.ReinforceAsync(["known"]);

        rejected.Succeeded.IsFalse();
        rejected.Rejection.Is("Unknown memory 'missing'.");
        accepted.Succeeded.IsTrue();
        accepted.Rejection.IsNull();
        store.Document.Memories[0].Retention.Is(9.9d);
    }

    [Fact]
    public async Task ReinforceAsync_first_successful_call_in_cycle_weakens_all_memories_once_and_strengthens_selected_ids()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "Selected", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "Other", Retention = 7 }
            ]
        });
        var service = CreateService(store);

        var result = await service.ReinforceAsync(["a"]);

        result.Succeeded.IsTrue();
        result.Rejection.IsNull();
        store.Document.Memories.Single(memory => memory.Id == "a").Retention.Is(9.9d);
        store.Document.Memories.Single(memory => memory.Id == "b").Retention.Is(6d);
    }

    [Fact]
    public async Task ReinforceAsync_second_successful_call_in_same_cycle_does_not_weaken_again()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "First", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "Second", Retention = 10 }
            ]
        });
        var service = CreateService(store);

        await service.ReinforceAsync(["a"]);
        await service.ReinforceAsync(["b"]);

        store.Document.Memories.Single(memory => memory.Id == "a").Retention.Is(9.9d);
        store.Document.Memories.Single(memory => memory.Id == "b").Retention.Is(9.9d);
    }

    [Fact]
    public async Task ReinforceAsync_ignores_second_reinforcement_of_same_memory_in_same_cycle()
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

        store.Document.Memories[0].Retention.Is(9.9d);
    }

    [Fact]
    public async Task RecallAsync_resets_reinforcement_cycle()
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
        await service.RecallAsync();
        await service.ReinforceAsync(["known"]);

        store.Document.Memories[0].Retention.Is(9.8d);
    }

    [Fact]
    public async Task Repeated_recalls_without_reinforcement_leave_memory_unchanged()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "short", Text = "Temporary note", Retention = 5 }
            ]
        });
        var service = CreateService(store);

        for (var recall = 1; recall <= 5; recall++)
            await service.RecallAsync();

        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Retention.Is(5d);
    }

    [Fact]
    public async Task Memory_weakened_below_delete_threshold_can_still_be_reinforced_before_next_recall()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "Anchor", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "Recoverable", Retention = 1.9 }
            ]
        });
        var service = CreateService(store);

        await service.ReinforceAsync(["a"]);
        var result = await service.ReinforceAsync(["b"]);

        result.Succeeded.IsTrue();
        result.Rejection.IsNull();
        store.Document.Memories.Count.Is(2);
        store.Document.Memories.Single(memory => memory.Id == "b").Retention.Is(1d);
    }

    [Fact]
    public async Task RecallAsync_prunes_memories_that_remain_below_delete_threshold_after_a_cycle()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "a", Text = "Anchor", Retention = 10 },
                new PersistedMemory { Id = "b", Text = "Expired", Retention = 1.2 }
            ]
        });
        var service = CreateService(store);

        await service.ReinforceAsync(["a"]);

        store.Document.Memories.Count.Is(2);
        store.Document.Memories.Single(memory => memory.Id == "b").Retention.Is(0.2d);

        var memories = await service.RecallAsync();

        memories.Count.Is(1);
        memories[0].Id.Is("a");
        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Retention.Is(9.9d);
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

    private static CachedMemoryService CreateService(InMemoryMemoryStore store)
    {
        return new CachedMemoryService(
            store,
            new RetentionPolicy(),
            new Tracker());
    }

    private sealed class InMemoryMemoryStore(PersistedMemoryDocument document) : IMemoryStore
    {
        public PersistedMemoryDocument Document { get; private set; } = document;

        public void Replace(PersistedMemoryDocument document) => Document = document;

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Document);

        public Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.CompletedTask;
        }
    }
}
