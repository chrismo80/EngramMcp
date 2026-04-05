using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Storage;
using EngramMcp.Tools.Tests.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EngramMcp.Tools.Tests;

public abstract class ToolTests<TTool> : IDisposable where TTool : notnull
{
    protected ServiceProvider ServiceProvider { get; }

    protected TTool Sut { get; }

    protected InMemoryMemoryStore Store { get; }

    protected InMemoryMemoryStore GlobalStore { get; }

    protected ToolTests()
    {
        // Backwards-compat: existing tests expect to be able to poke a single Store.
        // Route that Store to the project scope (short memories).
        Store = new InMemoryMemoryStore(new PersistedMemoryDocument());

        GlobalStore = new InMemoryMemoryStore(new PersistedMemoryDocument());
        var globalStore = GlobalStore;
        var projectStore = Store;

        ServiceProvider = new ServiceCollection()
            // Tool tests use a single in-memory store for both scopes.
            .WithEngramMcp(
                Path.Combine(Path.GetTempPath(), "engram-mcp-tools-tests", "global.json"),
                Path.Combine(Path.GetTempPath(), "engram-mcp-tools-tests", "project.json"))
            .AddSingleton<GlobalJsonMemoryStore>(_ => new TestGlobalStore(globalStore))
            .AddSingleton<ProjectJsonMemoryStore>(_ => new TestProjectStore(projectStore))
            .BuildServiceProvider();

        Sut = ServiceProvider.GetRequiredService<TTool>();
    }

    private sealed class TestGlobalStore(InMemoryMemoryStore inner) : GlobalJsonMemoryStore("/dev/null")
    {
        public override Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => inner.EnsureInitializedAsync(cancellationToken);

        public override Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default) => inner.LoadAsync(cancellationToken);

        public override Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default) => inner.SaveAsync(document, cancellationToken);
    }

    private sealed class TestProjectStore(InMemoryMemoryStore inner) : ProjectJsonMemoryStore("/dev/null")
    {
        public override Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => inner.EnsureInitializedAsync(cancellationToken);

        public override Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default) => inner.LoadAsync(cancellationToken);

        public override Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default) => inner.SaveAsync(document, cancellationToken);
    }

    public void Dispose() => ServiceProvider.Dispose();
}
