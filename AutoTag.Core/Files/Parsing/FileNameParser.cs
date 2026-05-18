using AutoTag.Core.Config;

namespace AutoTag.Core.Files.Parsing;

public interface IFileNameParser
{
    (ParsedTVFileName? TVResult, ParsedMovieFileName? MovieResult) ParseFileName(string filePath);
}

public class FileNameParser(AutoTagConfig config, TVFileNameParser tvParser, MovieFileNameParser movieParser) : IFileNameParser
{
    public (ParsedTVFileName? TVResult, ParsedMovieFileName? MovieResult) ParseFileName(string filePath)
    {
        ParsedTVFileName? tvResult = null;
        if (config.Mode != Mode.Movie && tvParser.TryParse(filePath, out var tv))
        {
            tvResult = tv;
        }

        ParsedMovieFileName? movieResult = null;
        if (config.Mode != Mode.TV && movieParser.TryParse(filePath, out var movie))
        {
            movieResult = movie;
        }

        return (tvResult, movieResult);
    }
}
