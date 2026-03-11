using EngramMcp.Core.Abstractions;

namespace EngramMcp.Features;

public abstract class StoreMemoryToolBase(IMemoryService memoryService) : Tool
{
    protected IMemoryService MemoryService { get; } = memoryService;
}
