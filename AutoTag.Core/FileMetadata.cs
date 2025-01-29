using System.Text.RegularExpressions;
using AutoTag.Core.Config;

namespace AutoTag.Core;
public abstract class FileMetadata
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public string? CoverURL { get; set; }
    public bool Success { get; set; }
    public bool Complete { get; set; }
    public string? Director { get; set; }
    public IEnumerable<string>? Actors { get; set; }
    public IEnumerable<string>? Characters { get; set; }
    public IEnumerable<string>? Genres { get; set; }

    public FileMetadata()
    {
        Success = true;
        Complete = true;
    }

    public virtual void WriteToFile(TagLib.File file, AutoTagConfig config, IUserInterface ui)
    {
        file.Tag.Title = Title;
        file.Tag.Description = Overview;

        if (Genres != null && Genres.Any())
        {
            file.Tag.Genres = Genres.ToArray();
        }

        if (config.ExtendedTagging && (file.TagTypes & TagLib.TagTypes.Matroska) == TagLib.TagTypes.Matroska)
        {
            file.Tag.Conductor = Director;
            file.Tag.Performers = Actors?.ToArray();
            file.Tag.PerformersRole = Characters?.ToArray();
        }
    }

    public abstract string GetFileName(AutoTagConfig config);

    protected static readonly Regex RenameRegex = new(@"%(?<num>\d+)(?:\:(?<format>[0#]+))?");

    protected static string FormatRenameNumber(Match match, int value)
    {
        if (match.Groups.TryGetValue("format", out var format))
        {
            return value.ToString(format.Value);
        }
        else
        {
            return value.ToString();
        }
    }

    public abstract override string ToString();
}