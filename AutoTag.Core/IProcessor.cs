namespace AutoTag.Core;
public interface IProcessor : IDisposable
{
    Task<bool> ProcessAsync(
        TaggingFile file,
        Action<string> setPath,
        Action<string, MessageType> setStatus,
        Func<List<(string, string)>, int?> selectResult,
        AutoTagConfig config,
        FileWriter writer
    );
}