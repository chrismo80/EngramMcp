using System.Globalization;

namespace EngramMcp.Tools.Memory.Identity;

public sealed class TimestampMemoryIdGenerator : IMemoryIdGenerator
{
    public string CreateId(IReadOnlyCollection<string> existingIds, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(existingIds);

        var baseId = now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);

        if (!existingIds.Contains(baseId, StringComparer.Ordinal))
            return baseId;

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseId}-{suffix}";

            if (!existingIds.Contains(candidate, StringComparer.Ordinal))
                return candidate;
        }
    }
}
