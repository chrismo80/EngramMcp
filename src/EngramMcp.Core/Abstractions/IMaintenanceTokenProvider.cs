namespace EngramMcp.Core.Abstractions;

public interface IMaintenanceTokenProvider
{
    string Issue(string section);

    bool TryReserveForSection(string token, string section, out MaintenanceTokenReservation reservation);

    void Complete(MaintenanceTokenReservation reservation);

    void Release(MaintenanceTokenReservation reservation);
}
