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

    public static bool TryParse(string? value, out MemoryImportance importance)
    {
        switch (value)
        {
            case "low":
                importance = MemoryImportance.Low;
                return true;
            case "normal":
                importance = MemoryImportance.Normal;
                return true;
            case "high":
                importance = MemoryImportance.High;
                return true;
            default:
                importance = default;
                return false;
        }
    }
}
