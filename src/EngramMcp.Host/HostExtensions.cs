using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using EngramMcp.Features;
using EngramMcp.Infrastructure;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace EngramMcp.Host;

public static class HostExtensions
{
    internal static string ServerVersion => Assembly.GetExecutingAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(HostExtensions).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    extension(IServiceCollection services)
    {
        public void Compose(MemoryFileOptions options) => services
            .AddSingleton(options)
            .AddInfrastructure(options.FilePath, options.Size)
            .AddImplementations<Features.Tool>()
            .AddHostedService<StartupValidationService>()
            .AddMcpRuntime();

        private void AddMcpRuntime()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                WriteIndented = true
            };

            var builder = services.AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "EngramMcp",
                    Version = ServerVersion
                };
            });

            builder.WithStdioServerTransport();
            builder.WithTools(FeatureExtensions.GetImplementations<Features.Tool>(), serializerOptions);
        }
    }
}
