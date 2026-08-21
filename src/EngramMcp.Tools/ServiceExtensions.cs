using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Storage;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection WithEngramMcp(string memoryFilePath) => services
            .AddInfrastructure(memoryFilePath)
            .AddTools()
            .AddResources();

        private IServiceCollection AddInfrastructure(string memoryFilePath) => services
            .AddSingleton<RetentionCycle>()
            .AddSingleton<MemoryService>()
            .AddSingleton<IMemoryStore>(_ => new JsonlMemoryStore(memoryFilePath));

        private IServiceCollection AddTools()
        {
            foreach (var type in GetTools())
                services.AddSingleton(type);

            return services;
        }

        private IServiceCollection AddResources()
        {
            foreach (var type in GetResources())
                services.AddSingleton(type);

            return services;
        }
    }

    public static IEnumerable<Type> GetTools() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(IsTool)
        .Distinct();

    public static IEnumerable<Type> GetResources() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(IsResource)
        .Distinct();

    private static bool IsTool(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        type.GetCustomAttribute<McpServerToolTypeAttribute>(false) is not null;

    private static bool IsResource(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        type.GetCustomAttribute<McpServerResourceTypeAttribute>(false) is not null;
}
