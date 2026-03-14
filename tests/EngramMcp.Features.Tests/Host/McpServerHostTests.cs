using EngramMcp.Host;
using Is.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.Reflection;
using Xunit;

namespace EngramMcp.Features.Tests.Host;

public sealed class McpServerHostTests
{
    [Fact]
    public void ParseOptions_UsesWorkspaceDefault_WhenFileArgumentIsNotProvided()
    {
        var startupDirectory = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        var options = McpServerHost.ParseOptions([], startupDirectory);

        options.FilePath.Is(Path.Combine(startupDirectory, ".engram", "memory.json"));
    }

    [Fact]
    public void ParseOptions_UsesExplicitFilePath_WhenFileArgumentIsProvided()
    {
        const string filePath = "some/path.json";

        var options = McpServerHost.ParseOptions(["--file", filePath], "/workspace");

        options.FilePath.Is(filePath);
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenFileValueIsMissing()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--file"], "/workspace"));

        exception.Message.Contains("Missing value for '--file'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_ForUnknownArguments()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--wat"], "/workspace"));

        exception.Message.Contains("Unknown argument '--wat'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ServerVersion_UsesHostAssemblyInformationalVersion()
    {
        var expectedVersion = typeof(HostExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        expectedVersion.IsNotNull();
        HostExtensions.ServerVersion.Is(expectedVersion);
    }

    [Fact]
    public void Compose_ConfiguresMcpServerInfoWithHostAssemblyVersion()
    {
        var services = new ServiceCollection();

        services.Compose(new MemoryFileOptions { FilePath = "memory.json" });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        var serverInfo = options.ServerInfo;

        serverInfo.IsNotNull();
        serverInfo!.Name.Is("EngramMcp");
        serverInfo.Version.Is(HostExtensions.ServerVersion);
    }
}
