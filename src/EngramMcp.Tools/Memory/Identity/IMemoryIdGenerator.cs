namespace EngramMcp.Tools.Memory.Identity;

public interface IMemoryIdGenerator
{
    string CreateId(IReadOnlyCollection<string> existingIds, DateTime now);
}
