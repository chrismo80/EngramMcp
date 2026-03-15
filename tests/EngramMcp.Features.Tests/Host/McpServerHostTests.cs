using EngramMcp.Host;
using EngramMcp.Infrastructure.Memory;
using Is.Assertions;
using EngramMcp.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.Reflection;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Host;

public sealed class McpServerHostTests
{
    [Fact]
    public void ParseOptions_UsesWorkspaceDefault_WhenFileArgumentIsNotProvided()
    {
        var startupDirectory = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        var options = McpServerHost.ParseOptions([], startupDirectory);

        options.FilePath.Is(Path.Combine(startupDirectory, ".engram", "memory.json"));
        options.Size.Is(MemorySize.Small);
    }

    [Fact]
    public void ParseOptions_UsesExplicitFilePath_WhenFileArgumentIsProvided()
    {
        const string filePath = "some/path.json";

        var options = McpServerHost.ParseOptions(["--file", filePath], "/workspace");

        options.FilePath.Is(filePath);
        options.Size.Is(MemorySize.Small);
    }

    [Fact]
    public void ParseOptions_UsesExplicitSize_WhenSizeArgumentIsProvided()
    {
        var options = McpServerHost.ParseOptions(["--size", "big"], "/workspace");

        options.Size.Is(MemorySize.Big);
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenFileValueIsMissing()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--file"], "/workspace"));

        exception.Message.Contains("Missing value for '--file'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenSizeValueIsMissing()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--size"], "/workspace"));

        exception.Message.Contains("Missing value for '--size'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenSizeValueIsInvalid()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--size", "huge"], "/workspace"));

        exception.Message.Contains("Invalid value 'huge' for '--size'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenSizeOptionIsDuplicated()
    {
        var exception = Assert.Throws<ArgumentException>(() => McpServerHost.ParseOptions(["--size", "small", "--size", "big"], "/workspace"));

        exception.Message.Contains("The '--size' option may only be specified once.", StringComparison.Ordinal).IsTrue();
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

        services.Compose(new MemoryFileOptions { FilePath = "memory.json", Size = MemorySize.Normal });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        var serverInfo = options.ServerInfo;

        serverInfo.IsNotNull();
        serverInfo!.Name.Is("EngramMcp");
        serverInfo.Version.Is(HostExtensions.ServerVersion);
    }

    [Fact]
    public void Compose_ConfiguresMemoryCatalogWithSelectedSize()
    {
        var services = new ServiceCollection();

        services.Compose(new MemoryFileOptions { FilePath = "memory.json", Size = MemorySize.Small });

        using var serviceProvider = services.BuildServiceProvider();
        var catalog = serviceProvider.GetRequiredService<IMemoryCatalog>();

        catalog.GetByName(ShortTerm).Capacity.Is(5);
        catalog.GetByName(MediumTerm).Capacity.Is(10);
        catalog.GetByName(LongTerm).Capacity.Is(20);
        catalog.GetByName("project-x").Capacity.Is(20);
    }
}