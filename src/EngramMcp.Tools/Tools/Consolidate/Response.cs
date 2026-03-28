using System.Text.Json.Serialization;
using EngramMcp.Tools.Maintenance;

namespace EngramMcp.Tools.Tools.Consolidate;

public sealed record ConsolidateResponse
{
    [JsonPropertyName("section")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Section { get; init; }

    [JsonPropertyName("entries")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<MaintenanceMemoryEntry>? Entries { get; init; }

    [JsonPropertyName("consolidationToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConsolidationToken { get; init; }

    [JsonPropertyName("failure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MaintenanceSectionFailure? Failure { get; init; }
}

internal static class ConsolidateResponseFactory
{
    public static ConsolidateResponse Create(MaintenanceSectionReadResult result)
    {
        return new ConsolidateResponse
        {
            Section = result.Section,
            Entries = result.Entries,
            ConsolidationToken = result.ConsolidationToken
        };
    }

    public static ConsolidateResponse Create(MaintenanceSectionWriteResult result)
    {
        return new ConsolidateResponse
        {
            Section = result.Section,
            Entries = result.Entries
        };
    }
}
