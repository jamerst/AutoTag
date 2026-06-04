namespace AutoTag.CLI.Test.Helpers;

public class FileSystemBuilder(string basePath)
{
    public FileSystemBuilder CreateFile(string name)
    {
        var filePath = Path.Combine(basePath, name);
        var extension = Path.GetExtension(name);
        if (extension is ".mkv" or ".mp4")
        {
            File.Copy(Path.Combine("..", "..", "..", "TestFiles", $"test{extension}"), filePath);
        }
        else
        {
            using var _ = File.Create(filePath);
        }

        return this;
    }

    public FileSystemBuilder CreateDirectory(string name, Action<FileSystemBuilder> build)
    {
        var path = Path.Combine(basePath, name);

        Directory.CreateDirectory(path);
        var directory = new FileSystemBuilder(path);
        build(directory);

        return this;
    }

    public string GetPath(params string[] segments) => Path.Combine([basePath, ..segments]);
}