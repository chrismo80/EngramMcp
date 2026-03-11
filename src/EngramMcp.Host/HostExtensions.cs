using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using EngramMcp.Features;
using EngramMcp.Infrastructure;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Tool = EngramMcp.Features.Tool;

namespace EngramMcp.Host;

public static class HostExtensions
{
    extension(IServiceCollection services)
    {
        public void Compose(MemoryFileOptions options) => services
            .AddSingleton(options)
            .AddInfrastructure(options.FilePath)
            .AddImplementations<Tool>()
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
                    Version = "0.1.0"
                };
            });

            builder.WithStdioServerTransport();
            builder.WithTools(FeatureExtensions.GetImplementations<Tool>(), serializerOptions);
        }
    }
}
