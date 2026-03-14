namespace EngramMcp.Core;

internal static class MemoryRetention
{
	public static MemoryEntry GetEntryToEvict(this IReadOnlyList<MemoryEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);
		if (entries.Count == 0)
			throw new ArgumentException("At least one memory entry is required to select an eviction victim.", nameof(entries));
		return entries
			.OrderByDescending(e => e.GetEvictionScore())
			.ThenBy(e => e.Timestamp)
			.First();
	}

	extension(MemoryEntry entry)
	{
		private double GetEvictionScore()
			=> entry.AgeInDays() / entry.Importance.ToRetentionWeight();

		private double AgeInDays()
			=> (DateTime.Now - entry.Timestamp).TotalDays;
	}

	extension(MemoryImportance importance)
	{
		private double ToRetentionWeight() => importance switch
		{
			MemoryImportance.Low => 1d,
			MemoryImportance.Normal => 3d,
			MemoryImportance.High => 8d,
			_ => 3d
		};
	}
}