namespace EngramMcp.Tools.Tests.Memory;

internal sealed class TemporaryMemoryFile : IDisposable
{
    private readonly string _directoryPath;

    public TemporaryMemoryFile()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directoryPath);
        FilePath = Path.Combine(_directoryPath, "memory.json");
    }

    public string FilePath { get; }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }
}
