using AutoTag.Core.Files;

namespace AutoTag.Core.Tagging;

public interface ITagger
{
    Task<bool> TagFileAsync(TaggingFile taggingFile, FileMetadata metadata);
}