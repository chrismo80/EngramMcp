namespace EngramMcp.Core;

public enum MemoryImportance
{
    Low,
    Normal,
    High
}

public static class MemoryImportanceSerializer
{
    public static string ToSerializedValue(this MemoryImportance importance)
    {
        return importance switch
        {
            MemoryImportance.Low => "low",
            MemoryImportance.Normal => "normal",
            MemoryImportance.High => "high",
            _ => throw new ArgumentOutOfRangeException(nameof(importance), importance, "Memory importance must be low, normal, or high.")
        };
    }

    public static MemoryImportance Parse(this string? value) => value switch
    {
        "low" => MemoryImportance.Low,
        "normal" => MemoryImportance.Normal,
        "high" => MemoryImportance.High,
        _ => MemoryImportance.Normal,
    };
}