using EngramMcp.Tools.Memory;
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

        var result = await service.ReinforceAsync(["known", "missing"]);

        result.Succeeded.IsFalse();
        result.Rejection.Is("Unknown memory 'missing'.");
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
    public async Task Short_memory_without_reinforcement_disappears_after_five_recalls()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        await RememberMemoryAsync(store, RetentionTier.Short, "Temporary note");

        for (var session = 1; session <= 5; session++)
            await CreateService(store).RecallAsync();

        store.Document.Memories.IsEmpty();
    }

    [Fact]
    public async Task Frequently_reinforced_short_memory_can_survive_a_few_sessions()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var shortId = await RememberMemoryAsync(store, RetentionTier.Short, "Active working note");

        for (var session = 1; session <= 5; session++)
        {
            var service = CreateService(store);

            await service.RecallAsync();
            var reinforceResult = await service.ReinforceAsync([shortId]);

            reinforceResult.Succeeded.IsTrue();
            reinforceResult.Rejection.IsNull();
        }

        store.Document.Memories.Count.Is(1);
        store.Document.Memories[0].Id.Is(shortId);
        store.Document.Memories[0].Text.Is("Active working note");
        store.Document.Memories[0].Retention.Is(1.3d);
    }

    [Fact]
    public async Task Retention_model_example_shows_how_memories_diverge_over_twenty_sessions()
    {
        var store = new InMemoryMemoryStore(new PersistedMemoryDocument());

        var shortId = await RememberMemoryAsync(store, RetentionTier.Short, "Ephemeral detail");
        var mediumId = await RememberMemoryAsync(store, RetentionTier.Medium, "Useful preference");
        var longId = await RememberMemoryAsync(store, RetentionTier.Long, "Stable identity fact");

        for (var session = 1; session <= 20; session++)
        {
            var service = CreateService(store);
            await service.RecallAsync();

            if (session is 10 or 15)
            {
                var reinforceMediumResult = await service.ReinforceAsync([mediumId]);

                reinforceMediumResult.Succeeded.IsTrue();
                reinforceMediumResult.Rejection.IsNull();
            }

            if (session is 5 or 10 or 15)
            {
                var reinforceLongResult = await service.ReinforceAsync([longId]);

                reinforceLongResult.Succeeded.IsTrue();
                reinforceLongResult.Rejection.IsNull();
            }
        }

        store.Document.Memories.Count.Is(2);
        store.Document.Memories.Any(memory => memory.Id == shortId).IsFalse();

        var mediumMemory = store.Document.Memories.Single(memory => memory.Id == mediumId);
        var longMemory = store.Document.Memories.Single(memory => memory.Id == longId);

        mediumMemory.Text.Is("Useful preference");
        longMemory.Text.Is("Stable identity fact");
        mediumMemory.Retention.Is(7.6d);
        longMemory.Retention.Is(109.8d);
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

    private static CachedMemoryService CreateService(InMemoryMemoryStore store, IdGenerator? memoryIdGenerator = null)
    {
        return new CachedMemoryService(
            store,
            memoryIdGenerator ?? new IdGenerator(),
            new RetentionPolicy(),
            new SessionReinforcementTracker());
    }

    private static async Task<string> RememberMemoryAsync(InMemoryMemoryStore store, RetentionTier retentionTier, string text)
    {
        var memoryCount = store.Document.Memories.Count;
        var result = await CreateService(store).RememberAsync(retentionTier, text);

        result.Succeeded.IsTrue();
        result.Rejection.IsNull();

        return store.Document.Memories[memoryCount].Id;
    }

    private sealed class InMemoryMemoryStore(PersistedMemoryDocument document) : EngramMcp.Tools.Memory.Storage.IMemoryStore
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
