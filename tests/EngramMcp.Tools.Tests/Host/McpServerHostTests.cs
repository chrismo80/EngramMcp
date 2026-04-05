using EngramMcp.Host;
using Is.Assertions;
using Xunit;

namespace EngramMcp.Tools.Tests.Host;

public sealed class McpServerHostTests
{
    [Fact]
    public void ParseOptions_UsesWorkspaceDefault_WhenFileArgumentIsNotProvided()
    {
        var startupDirectory = Path.Combine(Path.GetTempPath(), "EngramMcp.Tests", Guid.NewGuid().ToString("N"));

        var options = McpServerHost.ParseOptions([], startupDirectory);

        options.ProjectFilePath.Is(Path.Combine(startupDirectory, ".engram", "memory.json"));
        options.GlobalFilePath.Is(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".engram", "global.json"));
    }

    [Fact]
    public void ParseOptions_UsesExplicitFilePath_WhenFileArgumentIsProvided()
    {
        const string filePath = "some/path.json";

        var options = McpServerHost.ParseOptions(["--file", filePath], "/workspace");

        options.GlobalFilePath.Is(filePath);
        options.ProjectFilePath.Is(Path.Combine("/workspace", ".engram", "memory.json"));
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_WhenFileValueIsMissing()
    {
        var exception = Record.Exception(() => McpServerHost.ParseOptions(["--file"], "/workspace"));

        exception.IsNotNull();
        exception.Is<ArgumentException>();
        exception.Message.Contains("Missing value for '--file'", StringComparison.Ordinal).IsTrue();
    }

    [Fact]
    public void ParseOptions_ThrowsClearError_ForUnknownArguments()
    {
        var exception = Record.Exception(() => McpServerHost.ParseOptions(["--wat"], "/workspace"));

        exception.IsNotNull();
        exception.Is<ArgumentException>();
        exception.Message.Contains("Unknown argument '--wat'", StringComparison.Ordinal).IsTrue();
    }
}
