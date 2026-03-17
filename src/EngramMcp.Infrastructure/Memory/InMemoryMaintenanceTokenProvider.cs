using System.Collections.Concurrent;
using System.Security.Cryptography;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class InMemoryMaintenanceTokenProvider : IMaintenanceTokenProvider
{
    private readonly ConcurrentDictionary<string, int> _sectionVersions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TokenState> _tokens = new(StringComparer.Ordinal);

    public string Issue(string section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var version = _sectionVersions.GetOrAdd(section, 0);

        if (!_tokens.TryAdd(token, new TokenState(section, version, TokenLifecycleState.Available)))
            throw new InvalidOperationException("Failed to issue a unique maintenance token.");

        return token;
    }

    public MaintenanceTokenReservationStatus TryReserveForSection(string token, string section, out MaintenanceTokenReservation reservation)
    {
        reservation = default;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(section))
            return MaintenanceTokenReservationStatus.Invalid;

        while (_tokens.TryGetValue(token, out var state))
        {
            if (!string.Equals(state.Section, section, StringComparison.Ordinal))
                return MaintenanceTokenReservationStatus.Invalid;

            if (state.Version != _sectionVersions.GetOrAdd(section, 0))
                return MaintenanceTokenReservationStatus.Stale;

            if (state.LifecycleState != TokenLifecycleState.Available)
                return MaintenanceTokenReservationStatus.Invalid;

            var reservedState = state with { LifecycleState = TokenLifecycleState.Reserved };

            if (_tokens.TryUpdate(token, reservedState, state))
            {
                reservation = new MaintenanceTokenReservation(token, section);
                return MaintenanceTokenReservationStatus.Reserved;
            }
        }

        return MaintenanceTokenReservationStatus.Invalid;
    }

    public void Complete(MaintenanceTokenReservation reservation)
    {
        var currentVersion = _sectionVersions.AddOrUpdate(reservation.Section, 1, static (_, version) => checked(version + 1));
        var reservedState = new TokenState(reservation.Section, currentVersion - 1, TokenLifecycleState.Reserved);
        var consumedState = reservedState with { LifecycleState = TokenLifecycleState.Consumed };

        if (!_tokens.TryUpdate(reservation.Token, consumedState, reservedState))
            throw new InvalidOperationException("Maintenance token could not be completed after a successful write.");
    }

    public void Release(MaintenanceTokenReservation reservation)
    {
        var version = _sectionVersions.GetOrAdd(reservation.Section, 0);
        var reservedState = new TokenState(reservation.Section, version, TokenLifecycleState.Reserved);
        var availableState = reservedState with { LifecycleState = TokenLifecycleState.Available };

        if (!_tokens.TryUpdate(reservation.Token, availableState, reservedState))
            throw new InvalidOperationException("Maintenance token could not be released after a failed write.");
    }

    private sealed record TokenState(string Section, int Version, TokenLifecycleState LifecycleState);

    private enum TokenLifecycleState
    {
        Available,
        Reserved,
        Consumed
    }
}
