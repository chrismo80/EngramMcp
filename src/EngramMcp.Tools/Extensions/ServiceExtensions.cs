using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Maintenance;

namespace EngramMcp.Tools.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection WithEngramMcp(string memoryFilePath, MemorySize memorySize) => services
            .AddSingleton<IMemoryCatalog>(_ => new MemoryCatalog(memorySize))
            .AddSingleton<IMemoryStore>(provider => new JsonMemoryStore(memoryFilePath, provider.GetRequiredService<IMemoryCatalog>()))
            .AddSingleton<IMaintenanceTokenProvider, MaintenanceTokenProvider>()
            .AddSingleton<IMemoryService, MemoryService>()
            .AddImplementations<Tool>();

        private IServiceCollection AddImplementations<T>()
        {
            foreach (var type in GetImplementations<T>())
                services.AddSingleton(type);

            return services;
        }
    }

    public static IEnumerable<Type> GetTools() => GetImplementations<Tool>();

    private static IEnumerable<Type> GetImplementations<T>() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(type => type.Implements<T>())
        .Distinct();

    private static bool Implements<T>(this Type type) =>
        type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(T));
}
