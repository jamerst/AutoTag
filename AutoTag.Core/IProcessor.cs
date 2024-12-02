using AutoTag.Core.Files;

namespace AutoTag.Core;
public interface IProcessor
{
    Task<bool> ProcessAsync(
        TaggingFile file
    );
}