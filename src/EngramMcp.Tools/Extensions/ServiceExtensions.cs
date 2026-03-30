using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Identity;
using EngramMcp.Tools.Memory.Retention;
using EngramMcp.Tools.Memory.Session;
using EngramMcp.Tools.Memory.Storage;

namespace EngramMcp.Tools.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection WithEngramMcp(string memoryFilePath) => services
            .AddSingleton<IMemoryIdGenerator, TimestampMemoryIdGenerator>()
            .AddSingleton<IRetentionPolicy, DefaultRetentionPolicy>()
            .AddSingleton<SessionReinforcementTracker>()
            .AddSingleton<EngramMcp.Tools.Memory.Storage.IMemoryStore>(_ => new EngramMcp.Tools.Memory.Storage.JsonMemoryStore(memoryFilePath))
            .AddSingleton<IMemoryService, CachedMemoryService>()
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
