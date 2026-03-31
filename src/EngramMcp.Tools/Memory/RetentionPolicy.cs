namespace EngramMcp.Tools.Memory;

public enum RetentionTier
{
    Short,
    Medium,
    Long
}

public sealed class RetentionPolicy
{
    public double CreateInitialRetention(RetentionTier retentionTier) => retentionTier switch
    {
        RetentionTier.Short => 5,
        RetentionTier.Medium => 25,
        RetentionTier.Long => 100,
        _ => throw new ArgumentOutOfRangeException(nameof(retentionTier), retentionTier, "Unsupported retention tier.")
    };

    public double Decay(double retention) => retention - 1;

    public double Reinforce(double retention) => retention * 1.1;

    public bool ShouldDelete(double retention) => retention < 1;
}
