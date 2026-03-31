namespace EngramMcp.Tools.Memory;

public sealed class SessionReinforcementTracker
{
    private bool _hasAppliedGlobalWeakening;
    private readonly HashSet<string> _reinforcedIds = new(StringComparer.Ordinal);

    public void Reset()
    {
        _hasAppliedGlobalWeakening = false;
        _reinforcedIds.Clear();
    }

    public bool MarkGlobalWeakeningAppliedIfFirstTime()
    {
        if (_hasAppliedGlobalWeakening)
            return false;

        _hasAppliedGlobalWeakening = true;
        return true;
    }

    public bool MarkReinforcedIfFirstTime(string memoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryId);
        return _reinforcedIds.Add(memoryId);
    }
}
