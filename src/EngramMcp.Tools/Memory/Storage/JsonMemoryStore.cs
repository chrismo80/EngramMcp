using System.Text.Encodings.Web;
using System.Text.Json;

namespace EngramMcp.Tools.Memory.Storage;

public sealed class JsonMemoryStore(string filePath) : IMemoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath = ResolvePath(filePath);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
        ExecuteExclusiveAsync(EnsureInitializedCoreAsync, cancellationToken);

    public Task<PersistedMemoryDocument> LoadAsync(CancellationToken cancellationToken = default) =>
        ExecuteExclusiveAsync(LoadCoreAsync, cancellationToken);

    public Task SaveAsync(PersistedMemoryDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        return ExecuteExclusiveAsync(innerCancellationToken => SaveCoreAsync(document, innerCancellationToken), cancellationToken);
    }

    private async Task EnsureInitializedCoreAsync(CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_filePath);

        try
        {
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(_filePath))
                await SaveCoreAsync(new PersistedMemoryDocument(), cancellationToken).ConfigureAwait(false);
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

    private async Task<PersistedMemoryDocument> LoadCoreAsync(CancellationToken cancellationToken)
    {
        await EnsureInitializedCoreAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
            PersistedMemoryDocument? document;

            try
            {
                document = JsonSerializer.Deserialize<PersistedMemoryDocument>(json, SerializerOptions);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"Memory file '{_filePath}' contains malformed JSON.", exception);
            }

            if (document is null)
                throw new InvalidOperationException($"Memory file '{_filePath}' contains an invalid JSON document.");

            ValidateDocument(document);
            return document;
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

    private async Task SaveCoreAsync(PersistedMemoryDocument document, CancellationToken cancellationToken)
    {
        ValidateDocument(document);

        try
        {
            var directoryPath = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var json = JsonSerializer.Serialize(document, SerializerOptions);
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

    private static void ValidateDocument(PersistedMemoryDocument document)
    {
        if (document.Memories is null)
            throw new InvalidOperationException("Memory file has invalid structure. 'memories' must be an array.");

        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var memory in document.Memories)
        {
            if (memory is null)
                throw new InvalidOperationException("Memory file has invalid structure. Memory entries must not be null.");

            if (!ids.Add(memory.Id))
                throw new InvalidOperationException($"Memory file has invalid structure. Duplicate memory id '{memory.Id}'.");
        }
    }

    private async Task ExecuteExclusiveAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
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
}
