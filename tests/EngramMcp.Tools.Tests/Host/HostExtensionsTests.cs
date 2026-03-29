using System.Reflection;
using EngramMcp.Host;
using EngramMcp.Tools.Memory.Storage;
using Is.Assertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace EngramMcp.Tools.Tests.Host;

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

        services.Compose(new MemoryFileOptions { FilePath = "memory.json" });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        var serverInfo = options.ServerInfo;

        serverInfo.IsNotNull();
        serverInfo!.Name.Is("EngramMcp");
        serverInfo.Version.Is(HostExtensions.ServerVersion);
    }

    [Fact]
    public void Compose_RegistersJsonMemoryStore()
    {
        var services = new ServiceCollection();

        services.Compose(new MemoryFileOptions { FilePath = "memory.json" });

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<EngramMcp.Tools.Memory.Storage.IMemoryStore>().Is<JsonMemoryStore>();
    }
}
