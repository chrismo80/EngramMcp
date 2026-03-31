namespace EngramMcp.Tools.Memory;

public sealed class SessionReinforcementTracker
{
    private readonly HashSet<string> _reinforcedIds = new(StringComparer.Ordinal);

    public bool MarkIfFirstTime(string memoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryId);
        return _reinforcedIds.Add(memoryId);
    }
}
