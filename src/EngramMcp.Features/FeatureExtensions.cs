using EngramMcp.Core;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace EngramMcp.Features;

public static class FeatureExtensions
{
    extension(MemoryContainer container)
    {
        internal string ToMarkdown()
        {
            var sb = new StringBuilder("# Memory");

            foreach (var block in container.Memories)
            {
                sb.AppendLine().AppendLine($"## {block.Key}");

                foreach (var memory in block.Value)
                    sb.AppendLine($"- {memory.Text}");
            }

            if (container.CustomSections.Count > 0)
            {
                sb.AppendLine().AppendLine("## Custom Sections");

                foreach (var section in container.CustomSections
                    .OrderByDescending(summary => summary.EntryCount)
                    .ThenBy(summary => summary.Name, StringComparer.Ordinal))
                    sb.AppendLine($"- {section.Name} ({section.EntryCount})");
            }

            return sb.ToString();
        }
    }

    extension(IReadOnlyList<MemorySearchResult> results)
    {
        internal string ToMarkdown()
        {
            var sb = new StringBuilder("# Memory Search Results");

            if (results.Count == 0)
                return sb.AppendLine().AppendLine("No matches found.").ToString();

            sb.AppendLine();

            foreach (var result in results)
                sb.Append("- ")
                    .Append(result.Entry.Text)
                    .Append(" (`")
                    .Append(result.Section)
                    .AppendLine("`)");

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
