namespace AutoTag.CLI.Test.Helpers;

public abstract class CLITestBase : IDisposable
{
    protected CLITestBase()
    {
        ConfigPath = Path.Combine(".", $"test-config.{Random.Shared.Next()}.json");
    }

    public string ConfigPath { get; }

    public void Dispose() => File.Delete(ConfigPath);
}