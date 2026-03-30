namespace EngramMcp.Tools.Memory.Identity;

public sealed class TimestampMemoryIdGenerator : IMemoryIdGenerator
{
    private static readonly Lock Sync = new ();
    private static long _lastTimestamp;
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    public string GetUniqueId()
    {
        using (Sync.EnterScope())
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _lastTimestamp = Math.Max(timestamp, _lastTimestamp + 1);

            return ToBase36(_lastTimestamp);
        }
    }

    private static string ToBase36(long value)
    {
        Span<char> buffer = stackalloc char[13];

        int pos = 12;

        while (value > 0)
        {
            value = Math.DivRem(value, 36, out long remainder);

            buffer[pos--] = Alphabet[(int)remainder];
        }

        return new string(buffer[(pos + 1)..]);
    }
}