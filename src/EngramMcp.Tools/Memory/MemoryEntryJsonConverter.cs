using System.Text.Json;
using System.Text.Json.Serialization;

namespace EngramMcp.Tools.Memory;

public sealed class MemoryEntryJsonConverter : JsonConverter<MemoryEntry>
{
    public override MemoryEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Memory entry must be a JSON object.");

        if (!root.TryGetProperty("timestamp", out var timestampElement) || timestampElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Memory entry property 'timestamp' must be a string.");

        if (!root.TryGetProperty("text", out var textElement) || textElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Memory entry property 'text' must be a string.");

        MemoryImportance? importance = null;

        if (root.TryGetProperty("importance", out var importanceElement))
        {
            if (importanceElement.ValueKind != JsonValueKind.String)
                throw new JsonException("Memory entry property 'importance' must be a string.");

            if (!importanceElement.GetString().TryParseSerializedValue(out var parsedImportance))
                throw new JsonException("Memory entry property 'importance' must be one of: low, normal, high.");

            importance = parsedImportance;
        }

        if (!DateTime.TryParse(timestampElement.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
            throw new JsonException("Memory entry property 'timestamp' must be a valid round-trip datetime string.");

        return new MemoryEntry(timestamp, textElement.GetString()!, importance);
    }

    public override void Write(Utf8JsonWriter writer, MemoryEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("timestamp", value.Timestamp.ToString("O"));
        writer.WriteString("text", value.Text);

        if (value.Importance != MemoryImportance.Normal)
            writer.WriteString("importance", value.Importance.ToSerializedValue());

        writer.WriteEndObject();
    }
}
