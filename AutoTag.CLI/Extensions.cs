namespace AutoTag.CLI;

public static class Extensions
{
    public static void AddOptions<T>(this System.CommandLine.Command cmd)
        where T : IOptionsBase<T>
    {
        foreach (var option in T.GetOptions())
        {
            cmd.AddOption(option);
        }
    }
}