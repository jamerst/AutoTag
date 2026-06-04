namespace AutoTag.CLI.Test.Helpers;

public abstract class CLITestBase : IDisposable
{
    private readonly string _testDirectory;

    protected CLITestBase()
    {
        _testDirectory = Path.Combine(".", $"test-files-{Random.Shared.Next()}");
        Directory.CreateDirectory(_testDirectory);

        ConfigPath = Path.Combine(_testDirectory, "conf.json");
        FileSystem = new FileSystemBuilder(Path.Combine(_testDirectory, "filesystem"));
    }

    public string ConfigPath { get; }

    protected FileSystemBuilder FileSystem { get; }

    public void Dispose() => Directory.Delete(_testDirectory, true);
}