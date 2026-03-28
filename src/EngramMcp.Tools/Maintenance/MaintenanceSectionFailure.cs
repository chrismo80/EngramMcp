using System.Text.Json.Serialization;

namespace EngramMcp.Tools.Maintenance;

public sealed record MaintenanceSectionFailure
{
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MaintenanceSectionFailureDetail>? Details { get; init; }
}

public sealed record MaintenanceSectionFailureDetail
{
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
