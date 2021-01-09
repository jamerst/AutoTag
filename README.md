# AutoTag <br/>[![GitHub release](https://img.shields.io/github/release/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub release](https://img.shields.io/github/downloads/jamerst/AutoTag/total.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub issues](https://img.shields.io/github/issues/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/issues)

### Automatic tagging and renaming of TV show episodes and movies

Inspired by [Auto TV Tagger](https://sourceforge.net/projects/autotvtagger/), AutoTag is a command-line utility to make it very easy to organise your <sup>completely legitimate</sup> TV show and movie collection.

AutoTag interprets the file name to find the specific series, season and episode, or movie title, then fetches the relevant information from TheTVDB or TheMovieDB, adds the information to the file and renames it to a set format.

AutoTag v3 is a rewrite of v2 in .NET Core. This means that binaries can now be run natively on Linux without Mono! It also has a proper fully-functional command-line interface, however, **v3 is currently a command-line only application**.

This is because building cross-platform user interfaces with .NET Core is still quite difficult, and the documentation of current frameworks for this leave *a lot* to be desired. I personally use AutoTag over SSH to my server, so I have little motivation to develop a GUI that I will never use.

## Features
- Information fetched from [thetvdb.com](https://www.thetvdb.com/) and [themoviedb.org](https://www.themoviedb.org/)
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
  --manual                        Manually choose the series to tag
  -v|--verbose                    Enable verbose output mode
  --set-default                   Set the current arguments as the default
  --version                       Print version number and exit
  -?|-h|--help                    Show help information

```

## Config
AutoTag creates a config file to store default preferences at `~/.config/autotag/conf.json` or `%APPDATA%\Roaming\autotag\conf.json`. A different config file can be specified using the `-c` option. If the file does not exist, a file will be created with the default settings:
```
"configVer": 1,                         // Internal use
"mode": 0,                              // Default tagging mode, 0 = TV, 1 = Movie
"manualMode": false,                    // Manual tagging mode
"verbose": false,                       // Verbose output
"addCoverArt": true,                    // Add cover art to files
"tagFiles": true,                       // Write tags to files
"renameFiles": true,                    // Rename files
"tvRenamePattern": "%1 - %2x%3 - %4",   // Pattern to rename TV files, %1 = Series Name, %2 = Season, %3 = Episode, %4 = Episode Title
"movieRenamePattern": "%1 (%2)"         // Pattern to rename movie files, %1 = Title, %2 = Year
```

## Known Issues
- Some movie filenames may not parse correctly. To fix this you may have to remove extra information from the filename, keeping just the title and year should allow the name to successfully parse. **Please create an issue if you encounter problems, this will help to improve the parsing.**
- Matroska artwork thumbnails don't show up, but this is a problem with Windows (as per usual). <sup>1</sup>

<sup>1</sup> A 3rd party shell extension, [Icaros](http://shark007.net/tools.html), is available which allows the artwork to be shown in Windows Explorer (along with other useful file information).

## Download
Downloads for Linux, macOS and Windows can be found [here](https://github.com/jamerst/AutoTag/releases).

The macOS build is untested, I don't own any Apple devices so I can't easily test it. Please report any issues and I'll try to investigate them.

Build file sizes are quite large due to bundled .NET runtimes.

## Attributions
- TV filename parsing based on [SubtitleFetcher](https://github.com/pheiberg/SubtitleFetcher)
- File tagging provided by [taglib-sharp](https://github.com/mono/taglib-sharp)
- TheTVDB API support provided by [TvDbSharper](https://github.com/HristoKolev/TvDbSharper)
- TheMovieDB API support provided by [TMDbLib](https://github.com/LordMike/TMDbLib)
- Data sourced from [thetvdb.com](https://www.thetvdb.com/) and [themoviedb.org](https://www.themoviedb.org/) using their free APIs
- Command-line interface built using [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils)
