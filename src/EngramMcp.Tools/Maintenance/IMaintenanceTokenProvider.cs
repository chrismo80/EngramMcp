namespace EngramMcp.Tools.Maintenance;

public interface IMaintenanceTokenProvider
{
    string Issue(string section);

    MaintenanceTokenReservationStatus TryReserveForSection(string token, string section, out MaintenanceTokenReservation reservation);

    void Complete(MaintenanceTokenReservation reservation);

    void Release(MaintenanceTokenReservation reservation);
}
