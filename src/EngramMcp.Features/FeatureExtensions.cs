using EngramMcp.Core;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace EngramMcp.Features;

public static class FeatureExtensions
{
    extension(MemoryDocument document)
    {
        internal string ToMarkdown()
        {
            var sb = new StringBuilder("# Memory").AppendLine();

            foreach (var block in document.Memories.OrderBy(kvp => kvp.Key))
            {
                sb.AppendLine().AppendLine($"## {block.Key}");
            
                foreach(var memory in block.Value)
                    sb.AppendLine($"- {memory.Text}");
            }

            return sb.ToString();
        }
    }
    
    public static IEnumerable<Type> GetImplementations<T>() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(type => type.Implements<T>())
        .Distinct();
    
    extension(IServiceCollection services)
    {
        public IServiceCollection AddImplementations<T>()
        {
            foreach (var type in GetImplementations<T>())
                services.AddSingleton(type);
        
            return services;
        }
    }

    extension(Type type)
    {
        private bool Implements<T>() => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(T));
    }
}
