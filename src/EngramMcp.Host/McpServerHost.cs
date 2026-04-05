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

        string? globalFilePath = null;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "--file":
                    if (globalFilePath is not null)
                        throw new ArgumentException("The '--file' option may only be specified once.", nameof(args));

                    if (index + 1 >= args.Length)
                        throw new ArgumentException("Missing value for '--file'. Expected '--file <path>'.", nameof(args));

                    globalFilePath = args[++index];

                    if (string.IsNullOrWhiteSpace(globalFilePath))
                        throw new ArgumentException("The '--file' value must not be empty or whitespace.", nameof(args));

                    break;

                default:
                    throw new ArgumentException($"Unknown argument '{argument}'. Expected '--file <path>'.", nameof(args));
            }
        }

        // Project scope always defaults to the current working directory.
        var projectFilePath = Path.Combine(startupDirectory, ".engram", "memory.json");

        // Global scope is configured via --file, otherwise defaults to the user profile.
        globalFilePath ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".engram", "global.json");

        return new MemoryFileOptions
        {
            GlobalFilePath = globalFilePath,
            ProjectFilePath = projectFilePath
        };
    }
}
