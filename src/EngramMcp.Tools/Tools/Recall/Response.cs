using EngramMcp.Tools.Memory;

namespace EngramMcp.Tools.Tools.Recall;

public sealed record Response(IReadOnlyList<RecallMemory> Memories);
