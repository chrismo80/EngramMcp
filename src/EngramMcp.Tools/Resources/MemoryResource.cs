using System.ComponentModel;
using System.Text.Json;
using EngramMcp.Tools.Tools;
using ModelContextProtocol.Server;

namespace EngramMcp.Tools.Resources;

[McpServerResourceType]
public sealed class MemoryResource(RecallTool recall)
{
    [McpServerResource(UriTemplate = "memory://recall", Name = "recall", MimeType = "application/json")]
    [Description("The strongest current memories delivered as a resource; reading advances the retention lifecycle exactly like the recall tool.")]
    public async Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await recall.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(response, EngramJson.Options);
    }
}
