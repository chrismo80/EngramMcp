using EngramMcp.Tools.Memory.Storage;
using EngramMcp.Tools.Tools;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Tools;

public sealed class ForgetToolTests : ToolTests<ForgetTool>
{
    [Fact]
    public async Task ExecuteAsync_deletes_requested_memories()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "id-1", Text = "First memory", Retention = 10 },
                new PersistedMemory { Id = "id-2", Text = "Second memory", Retention = 10 }
            ]
        });

        var response = await Sut.ExecuteAsync(["id-1"]);

        response.IsNull();
        Store.Document.Memories.Count.Is(1);
        Store.Document.Memories[0].Id.Is("id-2");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_for_invalid_input()
    {
        var response = await Sut.ExecuteAsync([]);

        response.Is("At least one memory id is required.");
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_message_for_unknown_memory()
    {
        Store.Replace(new PersistedMemoryDocument
        {
            Memories =
            [
                new PersistedMemory { Id = "id-1", Text = "First memory", Retention = 10 }
            ]
        });

        var response = await Sut.ExecuteAsync(["id-2"]);

        response.Is("Unknown memory 'id-2'.");
    }
}

