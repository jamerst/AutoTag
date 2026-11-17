namespace AutoTag.Core.Tagging;

public class Tags
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? Album { get; set; }

    public int? Disc { get; set; }

    public int? Track { get; set; }

    public int? TrackCount { get; set; }

    public int? Year { get; set; }

    public IEnumerable<string>? Genres { get; set; }

    public string? Conductor { get; set; }

    public IEnumerable<string>? Performers { get; set; }

    public IEnumerable<string>? PerformerRoles { get; set; }

    public IEnumerable<(string, string)>? Custom { get; set; }

    public byte AppleMediaType { get; set; }
}