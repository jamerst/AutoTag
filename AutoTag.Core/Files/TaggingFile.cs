using AutoTag.Core.Files.Parsing;

namespace AutoTag.Core.Files;
public record TaggingFile
{
    public required string Path { get; init; }
    public List<string> SubtitlePaths { get; init; } = [];
    public bool Taggable { get; init; } = true;
    public string Status { get; set; } = "";
    public bool Success { get; set; } = true;

    public ParsedTVFileName? TVDetails { get; set; }
    public ParsedMovieFileName? MovieDetails { get; set; }
    
    public override string ToString()
    {
        return $"{System.IO.Path.GetFileName(Path)}: {Status}";
    }
}
