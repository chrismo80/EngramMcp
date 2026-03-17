using System.Text.Json;
using System.Text.Json.Serialization;

namespace EngramMcp.Core;

[JsonConverter(typeof(MaintenanceMemoryEntryJsonConverter))]
public sealed record MaintenanceMemoryEntry
{
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Tags { get; init; }

    [JsonPropertyName("importance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Importance { get; init; }
}

internal sealed class MaintenanceMemoryEntryJsonConverter : JsonConverter<MaintenanceMemoryEntry>
{
    public override MaintenanceMemoryEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Maintenance memory entry must be a JSON object.");

        string? timestamp = null;

        if (root.TryGetProperty("timestamp", out var timestampElement))
        {
            if (timestampElement.ValueKind == JsonValueKind.String)
                timestamp = timestampElement.GetString();
            else if (timestampElement.ValueKind != JsonValueKind.Null)
                throw new JsonException("Maintenance memory entry property 'timestamp' must be a string or null.");
        }

        if (!root.TryGetProperty("text", out var textElement))
            throw new JsonException("Maintenance memory entry is missing required property 'text'.");

        if (textElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Maintenance memory entry property 'text' must be a string.");

        List<string>? tags = null;

        if (root.TryGetProperty("tags", out var tagsElement))
        {
            if (tagsElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Maintenance memory entry property 'tags' must be an array.");

            tags = [];

            foreach (var tagElement in tagsElement.EnumerateArray())
            {
                if (tagElement.ValueKind != JsonValueKind.String)
                    throw new JsonException("Maintenance memory entry tags must be strings.");

                tags.Add(tagElement.GetString()!);
            }
        }

        string? importance = null;

        if (root.TryGetProperty("importance", out var importanceElement))
        {
            if (importanceElement.ValueKind == JsonValueKind.String)
                importance = importanceElement.GetString();
            else if (importanceElement.ValueKind != JsonValueKind.Null)
                throw new JsonException("Maintenance memory entry property 'importance' must be a string or null.");
        }

        return new MaintenanceMemoryEntry
        {
            Timestamp = timestamp!,
            Text = textElement.GetString()!,
            Tags = tags,
            Importance = importance
        };
    }

    public override void Write(Utf8JsonWriter writer, MaintenanceMemoryEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("timestamp", value.Timestamp);
        writer.WriteString("text", value.Text);

        if (value.Tags is { Count: > 0 })
        {
            writer.WritePropertyName("tags");
            writer.WriteStartArray();

            foreach (var tag in value.Tags)
                writer.WriteStringValue(tag);

            writer.WriteEndArray();
        }

        if (value.Importance is not null)
            writer.WriteString("importance", value.Importance);

        writer.WriteEndObject();
    }
}
