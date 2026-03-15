using EngramMcp.Infrastructure.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EngramMcp.Host;

public static class McpServerHost
{
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var options = ParseOptions(args, Directory.GetCurrentDirectory());
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();

        builder.Services.Compose(options);

        var host = builder.Build();
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static MemoryFileOptions ParseOptions(string[] args, string startupDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(startupDirectory);

        string? filePath = null;
        MemorySize? size = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--file":
                    if (filePath is not null)
                        throw new ArgumentException("The '--file' option may only be specified once.", nameof(args));

                    if (index + 1 >= args.Length)
                        throw new ArgumentException("Missing value for '--file'. Expected '--file <path>'.", nameof(args));

                    filePath = args[++index];

                    if (string.IsNullOrWhiteSpace(filePath))
                        throw new ArgumentException("The '--file' value must not be empty or whitespace.", nameof(args));

                    break;

                case "--size":
                    if (size is not null)
                        throw new ArgumentException("The '--size' option may only be specified once.", nameof(args));

                    if (index + 1 >= args.Length)
                        throw new ArgumentException("Missing value for '--size'. Expected '--size <small|normal|big>'.", nameof(args));

                    var sizeValue = args[++index];
                    if (string.IsNullOrWhiteSpace(sizeValue))
                        throw new ArgumentException("The '--size' value must not be empty or whitespace.", nameof(args));

                    size = sizeValue switch
                    {
                        "small" => MemorySize.Small,
                        "normal" => MemorySize.Normal,
                        "big" => MemorySize.Big,
                        _ => throw new ArgumentException($"Invalid value '{sizeValue}' for '--size'. Expected one of: small, normal, big.", nameof(args))
                    };

                    break;

                default:
                    throw new ArgumentException($"Unknown argument '{argument}'. Expected '--file <path>' or '--size <small|normal|big>'.", nameof(args));
            }
        }

        filePath ??= Path.Combine(startupDirectory, ".engram", "memory.json");

        return new MemoryFileOptions
        {
            FilePath = filePath,
            Size = size ?? MemorySize.Small
        };
    }
}