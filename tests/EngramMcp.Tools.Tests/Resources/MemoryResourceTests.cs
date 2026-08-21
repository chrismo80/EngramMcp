using System.Text.Json;
using System.Text.Json.Serialization;
using EngramMcp.Tools.Memory.Storage;
using EngramMcp.Tools.Resources;
using EngramMcp.Tools.Tools;
using Is.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EngramMcp.Tools.Tests.Resources;

public sealed class MemoryResourceTests : ToolTests<MemoryResource>
{
    [Fact]
    public async Task ReadAsync_returns_same_json_as_recall_tool()
    {
        var retentionValues = Enumerable.Range(1, 60).Select(index => (double)index).ToList();
        var shuffled = retentionValues.OrderBy(_ => Guid.NewGuid()).ToList();

        var memories = shuffled
            .Select((retention, index) => new PersistedMemory { Id = $"id-{index}", Text = $"Memory {index}", Retention = retention })
            .ToList();

        memories.Add(new PersistedMemory { Id = "obsolete", Text = "Obsolete memory", Retention = 0.5 });

        Store.Replace(memories);

        var tool = ServiceProvider.GetRequiredService<RecallTool>();

        var toolResult = await tool.ExecuteAsync();
        var resourceJson = await Sut.ReadAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var expected = JsonSerializer.Serialize(toolResult, options);

        resourceJson.Is(expected);
    }
}
