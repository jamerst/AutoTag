namespace autotag.Core;
public interface IProcessor : IDisposable
{
    Task<bool> ProcessAsync(
        string filePath,
        Action<string> setPath,
        Action<string, MessageType> setStatus,
        Func<List<(string, string)>, int?> selectResult,
        AutoTagConfig config,
        FileWriter writer
    );
}