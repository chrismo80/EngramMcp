using EngramMcp.Core;
using EngramMcp.Core.Abstractions;
using EngramMcp.Host;
using Is.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.Reflection;
using Xunit;
using static EngramMcp.Core.BuiltInMemorySections;

namespace EngramMcp.Features.Tests.Host;

public sealed class HostExtensionsTests
{
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
