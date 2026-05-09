using AutoTag.Core.Files;

namespace AutoTag.Core;
public interface IProcessor
{
    Task<ProcessResult> ProcessAsync(
        TaggingFile file
    );
}