using AutoTag.Core.Files.Parsing;

namespace AutoTag.Core.Files;

public record TaggingFile
{
    public required string Path { get; init; }
    public List<string> SubtitlePaths { get; init; } = [];
    public bool Taggable { get; init; } = true;
    public string Status { get; set; } = "";
    public bool Success { get; set; } = true;

    public ParsedTVFileName? TVDetails { get; init; }
    public ParsedMovieFileName? MovieDetails { get; init; }

    public override string ToString() => $"{System.IO.Path.GetFileName(Path)}: {Status}";
}