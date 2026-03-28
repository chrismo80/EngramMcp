using EngramMcp.Tools.Extensions;
using Xunit;

namespace EngramMcp.Tools.Tests;

public class ToolRegistrationTests
{
    [Fact]
    public void GetTools_returns_migrated_tools()
    {
        Assert.NotEmpty(ServiceExtensions.GetTools());
    }
}
