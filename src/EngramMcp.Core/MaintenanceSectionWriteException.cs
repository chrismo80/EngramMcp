namespace EngramMcp.Core;

public sealed class MaintenanceSectionWriteException(MaintenanceSectionFailure failure)
    : Exception(failure.Message)
{
    public MaintenanceSectionFailure Failure { get; } = failure;

    public static MaintenanceSectionWriteException ValidationFailed(string message, IReadOnlyList<MaintenanceSectionFailureDetail>? details = null)
    {
        return new MaintenanceSectionWriteException(new MaintenanceSectionFailure
        {
            Category = "validation_failed",
            Message = message,
            Details = details is null || details.Count == 0 ? null : details
        });
    }

    public static MaintenanceSectionWriteException MaintenanceTokenMissing(string message)
    {
        return new MaintenanceSectionWriteException(new MaintenanceSectionFailure
        {
            Category = "maintenance_token_missing",
            Message = message
        });
    }

    public static MaintenanceSectionWriteException MaintenanceTokenInvalid(string message)
    {
        return new MaintenanceSectionWriteException(new MaintenanceSectionFailure
        {
            Category = "maintenance_token_invalid",
            Message = message
        });
    }

    public static MaintenanceSectionWriteException MaintenanceTokenStale(string message)
    {
        return new MaintenanceSectionWriteException(new MaintenanceSectionFailure
        {
            Category = "maintenance_token_stale",
            Message = message
        });
    }

    public static MaintenanceSectionWriteException SectionNotFound(string message)
    {
        return new MaintenanceSectionWriteException(new MaintenanceSectionFailure
        {
            Category = "section_not_found",
            Message = message
        });
    }
}
