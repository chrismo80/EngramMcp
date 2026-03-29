namespace EngramMcp.Tools.Memory.Retention;

public interface IRetentionPolicy
{
    double CreateInitialRetention(RetentionTier retentionTier);
    double Decay(double retention);
    double Reinforce(double retention);
    bool ShouldDelete(double retention);
}
