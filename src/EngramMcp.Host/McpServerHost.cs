using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EngramMcp.Host;

public static class McpServerHost
{
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var options = ParseOptions(args);
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(consoleOptions =>
        {
            consoleOptions.SingleLine = true;
            consoleOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });

        builder.Services.Compose(options);

        var host = builder.Build();
        await host.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    private static MemoryFileOptions ParseOptions(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? filePath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (!string.Equals(argument, "--file", StringComparison.Ordinal))
                throw new ArgumentException($"Unknown argument '{argument}'. Expected '--file <path>'.", nameof(args));

            if (filePath is not null)
                throw new ArgumentException("The '--file' option may only be specified once.", nameof(args));

            if (index + 1 >= args.Length)
                throw new ArgumentException("Missing value for '--file'. Expected '--file <path>'.", nameof(args));

            filePath = args[++index];

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("The '--file' value must not be empty or whitespace.", nameof(args));
        }

        if (filePath is null)
            throw new ArgumentException("Missing required '--file <path>' argument.", nameof(args));

        return new MemoryFileOptions
        {
            FilePath = filePath
        };
    }
}
