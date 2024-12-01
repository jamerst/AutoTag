namespace AutoTag.Core;
public interface IProcessor : IDisposable
{
    Task<bool> ProcessAsync(
        TaggingFile file,
        FileWriter writer,
        IUserInterface ui
    );
}