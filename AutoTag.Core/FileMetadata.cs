using AutoTag.Core.Config;
using AutoTag.Core.Files;
using TagLib;
using File = TagLib.File;

namespace AutoTag.Core;

public abstract class FileMetadata
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public string? Overview { get; init; }
    public string? CoverURL { get; set; }
    public bool Complete { get; set; } = true;
    public string? Director { get; set; }
    public IEnumerable<string>? Actors { get; set; }
    public IEnumerable<string>? Characters { get; set; }
    public IEnumerable<string>? Genres { get; init; }

    public virtual void WriteToFile(File file, AutoTagConfig config, IUserInterface ui)
    {
        file.Tag.Title = Title;
        file.Tag.Description = Overview;

        if (Genres != null && Genres.Any())
        {
            file.Tag.Genres = Genres.ToArray();
        }

        if (config.ExtendedTagging && (file.TagTypes & TagTypes.Matroska) == TagTypes.Matroska)
        {
            file.Tag.Conductor = Director;
            file.Tag.Performers = Actors?.ToArray();
            file.Tag.PerformersRole = Characters?.ToArray();
        }
    }

    public abstract string GetRenamePattern(AutoTagConfig config);

    public abstract IEnumerable<IFileNameField> GetRenameFields();

    public abstract override string ToString();
}