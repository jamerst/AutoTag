using AutoTag.Core.Config;
using AutoTag.Core.Files;
using AutoTag.Core.Tagging;

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

    public virtual Tags GetTags(AutoTagConfig config)
    {
        var tags = new Tags
        {
            Title = Title,
            Description = Overview
        };

        if (Genres != null && Genres.Any())
        {
            tags.Genres = Genres.ToArray();
        }

        if (config.ExtendedTagging)
        {
            tags.Conductor = Director;
            tags.Performers = Actors;
            tags.PerformerRoles = Characters;
        }

        return tags;
    }

    public abstract string GetRenamePattern(AutoTagConfig config);

    public abstract IEnumerable<IFileNameField> GetRenameFields();

    public abstract override string ToString();
}