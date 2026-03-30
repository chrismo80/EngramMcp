using EngramMcp.Tools.Extensions;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Extensions;

public sealed class ServiceExtensionsTests
{
    [Fact]
    public void GetTools_returns_the_complete_tool_surface()
    {
        ServiceExtensions.GetTools()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Is([
                "RecallTool",
                "ReinforceTool",
                "RememberLongTool",
                "RememberMediumTool",
                "RememberShortTool"
            ]);
    }
}
