using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EngramMcp.Tools.Memory;
using EngramMcp.Tools.Memory.Storage;

namespace EngramMcp.Tools;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection WithEngramMcp(string globalMemoryFilePath, string projectMemoryFilePath) => services
            .AddInfrastructure(globalMemoryFilePath, projectMemoryFilePath)
            .AddImplementations<Tool>();

        private IServiceCollection AddInfrastructure(string globalMemoryFilePath, string projectMemoryFilePath) => services
            .AddSingleton<RetentionPolicy>()
            .AddSingleton<Tracker>()
            .AddSingleton<GlobalJsonMemoryStore>(_ => new GlobalJsonMemoryStore(globalMemoryFilePath))
            .AddSingleton<ProjectJsonMemoryStore>(_ => new ProjectJsonMemoryStore(projectMemoryFilePath))
            .AddSingleton<MemoryService>();
        
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
