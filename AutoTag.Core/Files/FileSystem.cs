namespace AutoTag.Core.Files;

public interface IFileSystem
{
    IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directoryInfo);

    bool Exists(FileSystemInfo info);

    bool Exists([NotNullWhen(true)] string? path);

    void Move(string sourceFileName, string destFileName);

    void CreateDirectory(DirectoryInfo directory);

    void CreateDirectory(string path);

    bool DirectoryExists(string path);

    bool DirectoryIsEmpty(string path);

    void DeleteDirectory(string path);

    bool PathContainsDirectory(string path);

    string? GetDirectoryPath(string path);

    Stream OpenReadStream(string path);

    Stream OpenWriteStream(string path);
}

public class FileSystem : IFileSystem
{
    public IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directoryInfo)
        => directoryInfo.GetFileSystemInfos();

    public bool Exists(FileSystemInfo info) => info.Exists;

    public bool Exists([NotNullWhen(true)] string? path) => File.Exists(path);

    public void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);

    public void CreateDirectory(DirectoryInfo directory) => directory.Create();

    public void CreateDirectory(string path) => CreateDirectory(new DirectoryInfo(path));

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool DirectoryIsEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();

    public void DeleteDirectory(string path) => Directory.Delete(path);

    public bool PathContainsDirectory(string path) => path.Contains(Path.DirectorySeparatorChar);

    public string? GetDirectoryPath(string path) => Path.GetDirectoryName(path);

    public Stream OpenReadStream(string path) => new FileStream(path, FileMode.Open, FileAccess.Read);

    public Stream OpenWriteStream(string path) => new FileStream(path, FileMode.Create, FileAccess.Write);
}