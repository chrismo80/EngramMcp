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
        // TODO(code-monkey): Replace this placeholder parser with robust CLI handling for `--file`.
        // Expected behavior: require `--file <path>` and fail clearly when the argument is missing or invalid.
        throw new NotImplementedException();
    }
}
