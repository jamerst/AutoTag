namespace AutoTag.Core;
public class TaggingFile
{
    public string Path { get; set; } = null!;
    public string? SubtitlePath { get; set; }
    public bool Taggable { get; set; } = true;
    public string Status { get; set; } = "";
    public bool Success { get; set; } = true;

    public override string ToString()
    {
        return $"{System.IO.Path.GetFileName(Path)}: {Status}";
    }
}