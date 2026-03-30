using EngramMcp.Tools.Memory;

namespace EngramMcp.Tools.Tools.Recall;

public sealed record RecallResponse(IReadOnlyList<RecallMemory> Memories);
