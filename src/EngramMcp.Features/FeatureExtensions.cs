using EngramMcp.Core;
using EngramMcp.Features.Tools;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EngramMcp.Features;

public static class FeatureExtensions
{
    extension(MemoryContainer container)
    {
        internal RecallResponse ToRecallResponse()
        {
            return new RecallResponse
            {
                Memories = container.Memories.ToVisibleMemories(),
                CustomSections = container.CustomSections.Count == 0
                    ? null
                    : [.. container.CustomSections
                        .OrderByDescending(summary => summary.EntryCount)
                        .ThenBy(summary => summary.Name, StringComparer.Ordinal)
                        .Select(summary => new MemorySectionSummaryResponse
                        {
                            Name = summary.Name,
                            EntryCount = summary.EntryCount
                        })]
            };
        }

        internal ReadSectionResponse ToReadSectionResponse()
        {
            return new ReadSectionResponse
            {
                Memories = container.Memories.ToVisibleMemories()
            };
        }
    }

    extension(MaintenanceSectionReadResult result)
    {
        internal MaintainSectionResponse ToMaintainSectionResponse()
        {
            return new MaintainSectionResponse
            {
                Section = result.Section,
                Entries = result.Entries,
                MaintenanceToken = result.MaintenanceToken
            };
        }
    }

    extension(MaintenanceSectionWriteResult result)
    {
        internal MaintainSectionResponse ToMaintainSectionResponse()
        {
            return new MaintainSectionResponse
            {
                Section = result.Section,
                Entries = result.Entries
            };
        }
    }

    extension(IReadOnlyList<MemorySearchResult> results)
    {
        internal SearchResponse ToSearchResponse()
        {
            return new SearchResponse
            {
                Results = [.. results.Select(result => result.ToSearchItemResponse())]
            };
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<MemoryVisibleItemResponse>> ToVisibleMemories(
        this IReadOnlyDictionary<string, List<MemoryEntry>> memories)
    {
        var visibleMemories = new Dictionary<string, IReadOnlyList<MemoryVisibleItemResponse>>(StringComparer.Ordinal);

        foreach (var memoryBlock in memories)
            visibleMemories[memoryBlock.Key] = [.. memoryBlock.Value.Select(entry => entry.ToVisibleItemResponse())];

        return visibleMemories;
    }

    private static MemoryVisibleItemResponse ToVisibleItemResponse(this MemoryEntry memory)
    {
        return new MemoryVisibleItemResponse
        {
            Text = memory.Text,
            Tags = memory.Tags.Count == 0 ? null : memory.Tags,
            Importance = memory.Importance.ToVisibleImportance()
        };
    }

    private static SearchItemResponse ToSearchItemResponse(this MemorySearchResult result)
    {
        return new SearchItemResponse
        {
            Text = result.Entry.Text,
            Section = result.Section,
            Tags = result.Entry.Tags.Count == 0 ? null : result.Entry.Tags,
            Importance = result.Entry.Importance.ToVisibleImportance()
        };
    }

    private static string? ToVisibleImportance(this MemoryImportance importance)
    {
        return importance == MemoryImportance.High ? importance.ToSerializedValue() : null;
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
