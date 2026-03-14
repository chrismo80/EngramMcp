using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace EngramMcp.Infrastructure.Memory;

public sealed class JsonMemoryStore : IMemoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    static JsonMemoryStore()
    {
        SerializerOptions.Converters.Add(new MemoryEntryJsonConverter());
    }

    private readonly string _filePath;
    private readonly string[] _expectedMemoryNames;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonMemoryStore(string filePath, IMemoryCatalog memoryCatalog)
    {
        ArgumentNullException.ThrowIfNull(memoryCatalog);

        _filePath = ResolvePath(filePath);
        _expectedMemoryNames = [.. memoryCatalog.Memories.Select(memory => memory.Name)];
    }

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteExclusiveAsync(EnsureInitializedCoreAsync, cancellationToken);
    }

    public Task UpdateAsync(Action<MemoryContainer> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        return ExecuteExclusiveAsync(async innerCancellationToken =>
            {
                var container = await LoadCoreAsync(innerCancellationToken).ConfigureAwait(false);
                
                update(container);
                
                await SaveCoreAsync(container, innerCancellationToken).ConfigureAwait(false);
            }, cancellationToken);
    }

    public Task<MemoryContainer> LoadAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteExclusiveAsync(LoadCoreAsync, cancellationToken);
    }

    public Task SaveAsync(MemoryContainer container, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(container);

        return ExecuteExclusiveAsync(
            innerCancellationToken => SaveCoreAsync(container, innerCancellationToken),
            cancellationToken);
    }

    private async Task EnsureInitializedCoreAsync(CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_filePath);

        try
        {
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(_filePath))
            {
                await SaveCoreAsync(CreateDefaultContainer(), cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Memory file path '{_filePath}' is inaccessible.", exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Memory file path '{_filePath}' could not be initialized.", exception);
        }
    }

    private async Task<MemoryContainer> LoadCoreAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedCoreAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            Dictionary<string, List<MemoryEntry>>? memories;

            try
            {
                memories = JsonSerializer.Deserialize<Dictionary<string, List<MemoryEntry>>>(json, SerializerOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"Memory file '{_filePath}' contains malformed JSON.", exception);
            }

            if (memories is null)
            {
                throw new InvalidOperationException($"Memory file '{_filePath}' contains an invalid JSON document.");
            }

            ValidateStructure(memories);
            return new MemoryContainer { Memories = new Dictionary<string, List<MemoryEntry>>(memories, StringComparer.Ordinal) };
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Memory file '{_filePath}' is inaccessible.", exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Memory file '{_filePath}' could not be read.", exception);
        }
    }

    private async Task SaveCoreAsync(MemoryContainer container, CancellationToken cancellationToken)
    {
        ValidateStructure(container.Memories);

        try
        {
            var directoryPath = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var json = JsonSerializer.Serialize(container.Memories, SerializerOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Memory file path '{_filePath}' is inaccessible.", exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Memory file path '{_filePath}' could not be written.", exception);
        }
    }

    private async Task ExecuteExclusiveAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T> ExecuteExclusiveAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private MemoryContainer CreateDefaultContainer()
    {
        var memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memoryName in _expectedMemoryNames)
            memories[memoryName] = [];

        return new MemoryContainer { Memories = memories };
    }

    private static string ResolvePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new ArgumentException("The configured memory file path must not be empty or whitespace.", nameof(configuredPath));

        try
        {
            return Path.GetFullPath(configuredPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"Memory file path '{configuredPath}' is invalid.", exception);
        }
    }

    private void ValidateStructure(IReadOnlyDictionary<string, List<MemoryEntry>> memories)
    {
        foreach (var memoryName in _expectedMemoryNames)
        {
            if (!memories.ContainsKey(memoryName))
                throw new InvalidOperationException($"Memory file has invalid structure. Missing required section '{memoryName}'.");

            if (memories[memoryName] is null)
                throw new InvalidOperationException($"Memory file has invalid structure. Section '{memoryName}' must be an array.");
        }

        foreach (var (memoryName, entries) in memories)
        {
            if (string.IsNullOrWhiteSpace(memoryName))
                throw new InvalidOperationException("Memory file has invalid structure. Section names must not be empty or whitespace.");

            if (entries is null)
                throw new InvalidOperationException($"Memory file has invalid structure. Section '{memoryName}' must be an array.");
        }
    }
}
