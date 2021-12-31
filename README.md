# AutoTag <br/>[![GitHub release](https://img.shields.io/github/release/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub release](https://img.shields.io/github/downloads/jamerst/AutoTag/total.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub issues](https://img.shields.io/github/issues/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/issues)

### Automatic tagging and renaming of TV show episodes and movies

Inspired by [Auto TV Tagger](https://sourceforge.net/projects/autotvtagger/), AutoTag is a command-line utility to make it very easy to organise your <sup>completely legitimate</sup> TV show and movie collection.

AutoTag interprets the file name to find the specific series, season and episode, or movie title, then fetches the relevant information from TheMovieDB, adds the information to the file and renames it to a set format.

AutoTag v3 is a rewrite of v2 in .NET Core. This means that binaries can now be run natively on Linux without Mono! It also has a proper fully-functional command-line interface, however, **v3 is currently a command-line only application**.

This is because building cross-platform user interfaces with .NET Core is still quite difficult, and the documentation of current frameworks for this leave *a lot* to be desired. I personally use AutoTag over SSH to my server, so I have little motivation to develop a GUI that I will never use.

## Features
- Information fetched from [themoviedb.org](https://www.themoviedb.org/)
- Configurable renaming and full metadata tagging, including cover art
- Manual tagging mode
- Full Linux support (and presumably macOS?)
- Supports mp4 and mkv containers

## Usage
```
Usage: autotag [options] [paths]

Options:
  -t|--tv                         TV tagging mode
  -m|--movie                      Movie tagging mode
  -c|--config-path <CONFIG_PATH>  Specify config file to load
  --no-rename                     Disable file renaming
  --no-tag                        Disable file tagging
  --no-cover                      Disable cover art tagging
  --manual                        Manually choose the series to tag from search results
  --tv-pattern <PATTERN>          Rename pattern for TV episodes
  --movie-pattern <PATTERN>       Rename pattern for movies
  -p|--pattern <PATTERN>          Custom regex to parse TV episode information
  --windows-safe                  Remove invalid Windows file name characters when renaming
  --extended-tagging              Add more information to Matroska file tags. Reduces tagging speed.
  -v|--verbose                    Enable verbose output mode
  --set-default                   Set the current arguments as the default
  --version                       Print version number and exit
  -?|-h|--help                    Show help information

```

### Rename Patterns
The TV and movie rename patterns are strings used to create the new file name when renaming is enabled. They can use the following variables:

- `%1`: TV Series Name/Movie Title
- `%2`: TV Season Number/Movie Year
- `%3`: TV Episode Number
- `%4`: TV Episode Title

#### Numeric Format Strings
Numeric variables (TV season/episode and movie year) also support a format string to specify the format of the number. They support the standard numeric format specifiers of `0` and `#`.

Example: to get the name "Series S01E01 Title.mkv", use the format `%1 S%2:00E%3:00 %4`.

### Regex Pattern
The custom regex pattern is used on the full file path, not just the file name. This allows AutoTag to tag file structures where the series name is not in the file name, e.g. for the structure `Series/Season 1/S01E01 Title.mkv`.

The regex pattern should have 3 named capturing groups: `SeriesName`, `Season` and `Episode`. For the example given above, a pattern could be `.*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)`.

Note that on Windows all directory separators (`\`) must be escaped as `\\`.

### Windows Safe
The `--windows-safe` option is for use on Linux where the files written may be accessed by a Windows host, or are being written to an NTFS filesystem.

### Extended Tagging
The `--extended-tagging` option adds additional information to Matroska video files such as actors and their characters. This option is not enabled by default because it may reduce tagging speed significantly due to the additional API requests needed.

## Config
AutoTag creates a config file to store default preferences at `~/.config/autotag/conf.json` or `%APPDATA%\Roaming\autotag\conf.json`. A different config file can be specified using the `-c` option. If the file does not exist, a file will be created with the default settings:
```
"configVer": 5,                           // Internal use
"mode": 0,                                // Default tagging mode, 0 = TV, 1 = Movie
"manualMode": false,                      // Manual tagging mode
"verbose": false,                         // Verbose output
"addCoverArt": true,                      // Add cover art to files
"tagFiles": true,                         // Write tags to files
"renameFiles": true,                      // Rename files
"tvRenamePattern": "%1 - %2x%3:00 - %4",  // Pattern to rename TV files, %1 = Series Name, %2 = Season, %3 = Episode, %4 = Episode Title
"movieRenamePattern": "%1 (%2)",          // Pattern to rename movie files, %1 = Title, %2 = Year
"parsePattern": "",                       // Custom regex to parse TV episode information
"windowsSafe": false                      // Remove any invalid Windows file name characters
"extendedTagging": false                  // Add more information to Matroska file tags
```

## Moving away from TheTVDB
**v3.1.0 and above use TheMovieDB as the TV metadata source instead of TheTVDB.** This is due to the declining quality of metadata, and TheTVDB's free API being deprecated in favour of a paid model.

Unfortunately there are many differences in the episode numbering between TheTVDB and TheMovieDB, so you may have to manually rename some files in order for them to be found on TheMovieDB. In the long term this is a good thing as the numbering on TheMovieDB generally makes much more sense than TheTVDB, and is a much friendlier community.

## Known Issues
- Some files will refuse to tag with an error such as "File not writeable" or "Invalid EBML format read". This is caused by the tagging library taglib-sharp, which sometimes refuses to tag certain files. The cause of this isn't immediately clear, but a workaround is to simply remux the file using ffmpeg (`ffmepg -i in.mkv -c copy out.mkv`), after which the file should tag successfully.

## Download
Downloads for Linux, macOS and Windows can be found [here](https://github.com/jamerst/AutoTag/releases).

The macOS build is untested, I don't own any Apple devices so I can't easily test it. Please report any issues and I'll try to investigate them.

Build file sizes are quite large due to bundled .NET runtimes.

## Attributions
- TV filename parsing based on [SubtitleFetcher](https://github.com/pheiberg/SubtitleFetcher)
- File tagging provided by [taglib-sharp](https://github.com/mono/taglib-sharp)
- TheMovieDB API support provided by [TMDbLib](https://github.com/LordMike/TMDbLib)
- Data sourced from [themoviedb.org](https://www.themoviedb.org/) using their free API
- Command-line interface built using [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)
