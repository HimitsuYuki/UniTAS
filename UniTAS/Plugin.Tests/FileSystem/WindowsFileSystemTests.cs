using FluentAssertions;
using UniTAS.Plugin.Services.VirtualEnvironment.InnerState.FileSystem.OsFileSystems;

namespace UniTAS.Plugin.Tests.FileSystem;

public class WindowsFileSystemTests
{
    [Fact]
    public void InitFileSystem()
    {
        var fileSystem = new WindowsFileSystem();

        fileSystem.DirectoryExists("C:").Should().BeTrue();
        fileSystem.PathIsAbsolute("C:").Should().BeTrue();
        // fileSystem.GetTempPath().Should().BeEquivalentTo("C:/Windows/Temp");
    }

    [Fact]
    public void GetDirectoryName()
    {
        var fileSystem = new WindowsFileSystem();
        // fileSystem.GetDirectoryName("C:\\Windows/test/something/path").Should()
        //     .BeEquivalentTo("C:\\Windows/test/something");
        fileSystem.GetDirectoryName("C:\\Windows").Should().BeEquivalentTo("C:");
    }

    [Fact]
    public void GetFileName()
    {
        var fileSystem = new WindowsFileSystem();
        fileSystem.GetFileName("C:\\Windows/test/something/path").Should().BeEquivalentTo("path");
        fileSystem.GetFileName("C:\\Windows").Should().BeEquivalentTo("Windows");
    }
}