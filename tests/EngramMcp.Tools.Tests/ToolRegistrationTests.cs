using EngramMcp.Tools.Extensions;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests;

public class ToolRegistrationTests
{
    [Fact]
    public void GetTools_returns_migrated_tools()
    {
        ServiceExtensions.GetTools().Any().IsTrue();
    }
}
