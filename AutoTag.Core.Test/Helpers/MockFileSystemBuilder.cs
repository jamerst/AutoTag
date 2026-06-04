using AutoTag.Core.Files;
using IOPath = System.IO.Path;

namespace AutoTag.Core.Test.Helpers;

public class MockFileSystemBuilder
{
    private readonly List<MockFileSystemBuilder> _directories = [];
    private readonly List<string> _files = [];

    private MockFileSystemBuilder(string path)
    {
        Path = path;
    }

    public MockFileSystemBuilder()
    {
        Path = OperatingSystem.IsWindows() ? @"C:\" : "/";
    }

    private string Path { get; }

    public MockFileSystemBuilder WithFile(string name)
    {
        _files.Add(IOPath.Combine(Path, name));

        return this;
    }

    public MockFileSystemBuilder WithDirectory(string name, Action<MockFileSystemBuilder> build)
    {
        var directory = new MockFileSystemBuilder(IOPath.Combine(Path, name));
        build(directory);

        _directories.Add(directory);

        return this;
    }

    private void SetupMock(Mock<IFileSystem> mock)
    {
        mock.Setup(fs => fs.Exists(It.Is<DirectoryInfo>(f => f.FullName == Path)))
            .Returns(true);

        foreach (var file in _files)
        {
            mock.Setup(fs => fs.Exists(It.Is<FileInfo>(f => f.FullName == file)))
                .Returns(true);
        }

        var entries = _files.Select(FileSystemInfo (f) => new FileInfo(f))
            .Concat(_directories.Select(FileSystemInfo (d) => new DirectoryInfo(d.Path)))
            .ToList();

        mock.Setup(fs => fs.GetDirectoryContents(It.Is<DirectoryInfo>(d => d.FullName == Path)))
            .Returns(entries);


        foreach (var directory in _directories)
        {
            directory.SetupMock(mock);
        }
    }

    public (IFileSystem FileSystem, FileSystemInfo Root) Build()
    {
        var mock = new Mock<IFileSystem>();

        SetupMock(mock);

        return (mock.Object, new DirectoryInfo(Path));
    }
}