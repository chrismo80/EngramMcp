using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using System.Text.Json;

namespace EngramMcp.Infrastructure.Memory;

public sealed class JsonMemoryFileStore : IMemoryFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly string[] _expectedMemoryNames;

    public JsonMemoryFileStore(string filePath, IMemoryCatalog memoryCatalog)
    {
        ArgumentNullException.ThrowIfNull(memoryCatalog);

        _filePath = ResolvePath(filePath);
        _expectedMemoryNames = [.. memoryCatalog.GetAll().Select(memory => memory.Name)];
    }

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(_filePath);

        try
        {
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(_filePath))
            {
                await SaveAsync(CreateDefaultDocument(), cancellationToken).ConfigureAwait(false);
                return;
            }

            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
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

    public async Task<MemoryDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
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
            return new MemoryDocument { Memories = new Dictionary<string, List<MemoryEntry>>(memories, StringComparer.Ordinal) };
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

    public async Task SaveAsync(MemoryDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateStructure(document.Memories);

        try
        {
            var json = JsonSerializer.Serialize(document.Memories, SerializerOptions);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new InvalidOperationException($"Memory file '{_filePath}' is inaccessible.", exception);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException($"Memory file '{_filePath}' could not be written.", exception);
        }
    }

    private MemoryDocument CreateDefaultDocument()
    {
        var memories = new Dictionary<string, List<MemoryEntry>>(StringComparer.Ordinal);

        foreach (var memoryName in _expectedMemoryNames)
        {
            memories[memoryName] = [];
        }

        return new MemoryDocument { Memories = memories };
    }

    private static string ResolvePath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new ArgumentException("The configured memory file path must not be empty or whitespace.", nameof(configuredPath));
        }

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
        var actualNames = memories.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expectedNames = _expectedMemoryNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();

        if (!actualNames.SequenceEqual(expectedNames, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Memory file has invalid structure. Expected sections: {string.Join(", ", expectedNames)}.");
        }

        foreach (var memoryName in _expectedMemoryNames)
        {
            if (memories[memoryName] is null)
            {
                throw new InvalidOperationException($"Memory file has invalid structure. Section '{memoryName}' must be an array.");
            }
        }
    }
}
