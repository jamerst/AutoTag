using System.Diagnostics.CodeAnalysis;

namespace AutoTag.Core.Files;

public interface IFileSystem
{
    IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directoryInfo);

    bool Exists([NotNullWhen(true)] string? path);
    
    void Move(string sourceFileName, string destFileName);

    void CreateDirectory(DirectoryInfo directoryInfo);

    Stream OpenReadStream(string path);

    Stream OpenWriteStream(string path);
}

public class FileSystem : IFileSystem
{
    public IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directoryInfo)
        => directoryInfo.GetFileSystemInfos();

    public bool Exists([NotNullWhen(true)] string? path) => File.Exists(path);

    public void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);

    public void CreateDirectory(DirectoryInfo directoryInfo) => directoryInfo.Create();

    public Stream OpenReadStream(string path) => new FileStream(path, FileMode.Open, FileAccess.Read);

    public Stream OpenWriteStream(string path) => new FileStream(path, FileMode.Create, FileAccess.Write);
}