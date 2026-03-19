using EngramMcp.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EngramMcp.Infrastructure.Memory;

internal sealed class MemoryEntryJsonConverter : JsonConverter<MemoryEntry>
{
    public override MemoryEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Memory entry must be a JSON object.");

        if (!root.TryGetProperty("timestamp", out var timestampElement))
            throw new JsonException("Memory entry is missing required property 'timestamp'.");

        if (timestampElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Memory entry property 'timestamp' must be a string.");

        if (!root.TryGetProperty("text", out var textElement))
            throw new JsonException("Memory entry is missing required property 'text'.");

        if (textElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Memory entry property 'text' must be a string.");

        MemoryImportance? importance = null;

        if (root.TryGetProperty("importance", out var importanceElement))
        {
            if (importanceElement.ValueKind != JsonValueKind.String)
                throw new JsonException("Memory entry property 'importance' must be a string.");

            importance = importanceElement.GetString().Parse();
        }

        return new MemoryEntry(timestampElement.GetDateTime(), textElement.GetString()!, importance);
    }

    public override void Write(Utf8JsonWriter writer, MemoryEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("timestamp", value.Timestamp);
        writer.WriteString("text", value.Text);

        if (value.Importance != MemoryImportance.Normal)
            writer.WriteString("importance", value.Importance.ToSerializedValue());

        writer.WriteEndObject();
    }
}
