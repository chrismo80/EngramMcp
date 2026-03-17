using System.Collections.Concurrent;
using System.Security.Cryptography;
using EngramMcp.Core.Abstractions;

namespace EngramMcp.Infrastructure.Memory;

public sealed class InMemoryMaintenanceTokenProvider : IMaintenanceTokenProvider
{
    private readonly ConcurrentDictionary<string, TokenState> _tokens = new(StringComparer.Ordinal);

    public string Issue(string section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        if (!_tokens.TryAdd(token, new TokenState(section, IsReserved: false)))
            throw new InvalidOperationException("Failed to issue a unique maintenance token.");

        return token;
    }

    public bool TryReserveForSection(string token, string section, out MaintenanceTokenReservation reservation)
    {
        reservation = default;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(section))
            return false;

        while (_tokens.TryGetValue(token, out var state))
        {
            if (!string.Equals(state.Section, section, StringComparison.Ordinal) || state.IsReserved)
                return false;

            var reservedState = state with { IsReserved = true };

            if (_tokens.TryUpdate(token, reservedState, state))
            {
                reservation = new MaintenanceTokenReservation(token, section);
                return true;
            }
        }

        return false;
    }

    public void Complete(MaintenanceTokenReservation reservation)
    {
        var reservedState = new TokenState(reservation.Section, IsReserved: true);

        if (!_tokens.TryRemove(new KeyValuePair<string, TokenState>(reservation.Token, reservedState)))
            throw new InvalidOperationException("Maintenance token could not be completed after a successful write.");
    }

    public void Release(MaintenanceTokenReservation reservation)
    {
        var reservedState = new TokenState(reservation.Section, IsReserved: true);
        var availableState = reservedState with { IsReserved = false };

        if (!_tokens.TryUpdate(reservation.Token, availableState, reservedState))
            throw new InvalidOperationException("Maintenance token could not be released after a failed write.");
    }

    private sealed record TokenState(string Section, bool IsReserved);
}
