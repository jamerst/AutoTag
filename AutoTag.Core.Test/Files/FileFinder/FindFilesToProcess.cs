using AutoTag.Core.Config;
using AutoTag.Core.Files;

namespace AutoTag.Core.Test.Files.FileFinder;

public class FindFilesToProcess
{
    [Fact]
    public void Should_FindCommonVideoContainers_AndOnlyTagKnownSafeFormats()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "episode.AVI"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "movie.mkv"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "clip.mov"), string.Empty);
            File.WriteAllText(Path.Combine(tempDirectory.FullName, "notes.txt"), string.Empty);

            var finder = new AutoTag.Core.Files.FileFinder(
                new AutoTagConfig { RenameSubtitles = false },
                new AutoTag.Core.Files.FileSystem(),
                new Mock<IUserInterface>().Object
            );

            var result = finder.FindFilesToProcess([tempDirectory]);

            result.Should().ContainSingle(file => file.Path.EndsWith("episode.AVI") && !file.Taggable);
            result.Should().ContainSingle(file => file.Path.EndsWith("movie.mkv") && file.Taggable);
            result.Should().ContainSingle(file => file.Path.EndsWith("clip.mov") && !file.Taggable);
            result.Should().NotContain(file => file.Path.EndsWith("notes.txt"));
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
