using EngramMcp.Core.Abstractions;
using EngramMcp.Infrastructure.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EngramMcp.Infrastructure;

public static class InfrastructureExtensions
{
    extension(IServiceCollection services)
    {
	    public IServiceCollection AddInfrastructure(string memoryFilePath) => services
	        .AddSingleton<IMemoryCatalog, CodeMemoryCatalog>()
	        .AddSingleton<IMemoryStore>(provider => new JsonMemoryStore(memoryFilePath, provider.GetRequiredService<IMemoryCatalog>()))
	        .AddSingleton<IMemoryService, MemoryService>();

	    public IServiceCollection AddInterfacesOf<T>() where T : class
	    {
		    services.AddSingleton(provider => ActivatorUtilities.CreateInstance<T>(provider));

		    foreach (var service in typeof(T).GetInterfaces())
		    {
			    if (services.Any(s => s.ServiceType == service))
				    throw new ArgumentException($"{service} already registered!");

			    services.AddSingleton(service, provider => provider.GetRequiredService<T>());
		    }

		    return services;
	    }
    }
}
