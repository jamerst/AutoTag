# AutoTag <br/>[![GitHub release](https://img.shields.io/github/release/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub release](https://img.shields.io/github/downloads/jamerst/AutoTag/total.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub issues](https://img.shields.io/github/issues/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/issues)

### Automatic tagging and renaming of TV show episodes and movies

Inspired by [Auto TV Tagger](https://sourceforge.net/projects/autotvtagger/), AutoTag is a command-line utility to make
it very easy to organise your <sup>completely legitimate</sup> TV show and movie collection.

AutoTag interprets the file name to find the specific series, season and episode, or movie title, then fetches the
relevant information from TheMovieDB, adds the information to the file and renames it to a set format.

## Features

- Information fetched from [themoviedb.org](https://www.themoviedb.org/)
- Configurable renaming and full metadata tagging, including cover art
- Manual tagging mode
- Supports tagging mp4 and mkv containers
- Subtitle file renaming for .srt, .vtt, .sub, .ssa and .ass files

## Usage

```
USAGE:
    autotag [paths] [OPTIONS]

ARGUMENTS:
    [paths]    Files or directories to process

OPTIONS:
    -h, --help                             Prints help information                                            
    -c, --config <PATH>                    Config file path                                                   
    -p, --pattern <PATTERN>                Custom regex to parse TV episode information                       
    -v, --verbose                          Enable verbose output mode                                         
        --set-default                      Set the current arguments as the default                           
        --print-config                     Print loaded configuration and exit                                
        --version                          Print version and exit                                             
        --no-rename                        Disable file and subtitle renaming                                 
        --tv-pattern <PATTERN>             Rename pattern for TV episodes                                     
        --movie-pattern <PATTERN>          Rename pattern for movies                                          
        --windows-safe                     Remove invalid Windows file name characters when renaming          
        --rename-subs                      Rename subtitle files                                              
        --replace <REPLACE=REPLACEMENT>    Replace <REPLACE> with <REPLACEMENT> in file names                 
    -a, --auto                             Auto tagging mode                                                  
    -t, --tv                               TV tagging mode                                                    
    -m, --movie                            Movie tagging mode                                                 
        --no-tag                           Disable file tagging                                               
        --no-cover                         Disable cover art tagging                                          
        --manual                           Manually choose the TV series/movie for a file from search results 
        --extended-tagging                 Add more information to Matroska file tags. Reduces tagging speed  
        --apple-tagging                    Add extra tags to mp4 files for use with Apple devices and software
    -l, --language <LANGUAGE>              Metadata language (default: en)                                    
        --search-language <LANGUAGE>       Additional languages to use when searching TMDB                    
    -g, --episode-group                    Manually choose alternate episode orderings for a TV show          
        --include-adult                    Include adult titles in TMDB searches                              
        --remove-empty-folders             Remove source folders after moving files if they are empty  
```

## Parsing

AutoTag should be able to parse most common naming schemes for TV and movie files.

AutoTag defaults to auto tagging mode, which will try to parse and tag files as a TV episode then a movie. You can use
the `-t/--tv` and `-m/--movie` options to specify the media type and force it to only parse and tag that type.

For TV both season-episode and absolute episode numbering is supported (though absolute numbering may incur a
performance penalty due to the extra data lookups required). **Absolute numbering is only supported for parsing, not
renaming**. An absolute numbered file will be renamed to season-episode numbering.

If a year is detected in the filename this will be used to improve the chance of selecting the correct result.
Multi-episode and multi-part files are also supported (though this is only used in renaming and has no effect on
tagging).

### Custom Parsing Regex

If AutoTag is not able to parse your file structure (e.g. if the series name is not in the file name like
`Series/Season 1/S01E01 Title.mkv`), then you can provide a custom parsing regex using the `-p` or `--pattern` option.

The custom regex pattern is used on the full file path, not just the file name.

The regex pattern should have the following named capturing groups:

- `SeriesName`
- `Season` and `Episode` OR `AbsoluteEpisode`
- `Year` (optional)
- `EndEpisode` (optional)
- `Part` (optional)

For the example given above, a pattern could be `.*/(?<SeriesName>.+)/Season (?<Season>\d+)/S\d+E(?<Episode>\d+)`.

Note that on Windows all directory separators (`\`) must be escaped as `\\`.

## Renaming

AutoTag supports both relative and absolute rename patterns. An absolute rename pattern is an absolute path (i.e.
contains directories), so this can be used to automatically move files into a particular directory.

When using an absolute rename pattern the `--remove-empty-folders` option can be enabled to automatically delete the
directory a file was sourced from after moving to the new directory.

### Rename Fields

AutoTag supports the following rename fields for TV and movie files. I recommend using the new specifiers as they are
more flexible (see [Formatting](#formatting) section below), but the legacy specifiers are still supported. New and
legacy specifiers can be mixed in the same rename pattern.

#### TV

| Specifier      | Legacy Specifier | Description                                                          |
|----------------|------------------|----------------------------------------------------------------------|
| `{Series}`     | `%1`             | TV series/show name                                                  |
| `{Season}`     | `%2`             | TV season number                                                     |
| `{Episode}`    | `%3`             | TV episode number                                                    |
| `{Title}`      | `%4`             | TV episode title                                                     |
| `{Year}`       | -                | TV show year<sup>1</sup>                                             |
| `{EndEpisode}` | -                | TV end episode (for multi-episode files)<sup>1</sup>                 |
| `{Part}`       | -                | TV episode part (for episodes split into multiple files)<sup>1</sup> |

<sup>1</sup>**The `Year`, `EndEpisode` and `Part` fields will only be present if the original file had them in the
name.**
These fields are provided so you can persist this information and avoid the files being renamed back every time AutoTag
is run on them.

#### Movies

| Specifier | Legacy Specifier | Description |
|-----------|------------------|-------------|
| `{Title}` | `%1`             | Movie title |
| `{Year}`  | `%2`             | Movie year  |

#### Formatting

Numeric variables (TV season/episode and movie year) also support a format string to specify the format of the number.
They support the standard numeric format specifiers of `0` and `#`.

Example: to get the name "Series S01E01 Title.mkv", use the format `{Series} {Season:S00}{Episode:E00} {Title}`.

Other characters can be included in the format and the `0` and `#` format strings will be replaced with the value for
that field.

A fallback can also be provided for when the value is `null` or `0` by adding a pipe (`|`) in the format followed by the
fallback value. For example, to use "Specials" instead of "Season 0" use `{Season:Season 0|Specials}`. To omit the field
entirely when the value is missing simply provide an empty fallback (e.g. `{Year: (0)|}`).

Note: legacy specifiers only support `0` and `#` in the format - to embed other characters in the format you must use
the new specifiers.

### Examples

| Output                                  | Pattern                                                                                         |
|-----------------------------------------|-------------------------------------------------------------------------------------------------|
| Series S01E02                           | `{Series} {Season:S00}{Episode:E00}`                                                            |
| Series 1x02 Title                       | `{Series} {Season}x{Episode:00} {Title}`                                                        |
| Series (2005) S01E02                    | `{Series}{Year: (0)\|} {Season:S00}{Episode:E00}`                                               |
| Series S01E02-03                        | `{Series} {Season:S00}{Episode:E00}{EndEpisode:-00\|}`                                          |
| Series S01E02 pt1                       | `{Series} {Season:S00}{Episode:E00}{Part: pt0\|}`                                               |
| /TV/Series/Season 1/Series 1x02 Title   | `/TV/{Series}/{Season:Season 0\|Specials}/{Series} {Season}x{Episode:00} {Title}`               |
| C:\TV\Series\Season 1\Series 1x02 Title | `C:\TV\{Series}\{Season:Season 0\|Specials}\{Series} {Season}x{Episode:00} {Title}`<sup>2</sup> |

<sup>2</sup>Directory separators in Windows paths (`\`) will need to be escaped as `\\` if editing the config file
manually.

### Subtitles

The `--rename-subs` option can be enabled to rename separate subtitle files. These will be renamed alongside video files
using the same rename pattern. If there are multiple subtitle files for the same episode/movie they will have a number
appended to each file name.

### Windows Safe

The `--windows-safe` option is for use on Linux/macOS where the files written may be accessed by a Windows host, or are
being written to an NTFS filesystem. It automatically removes any invalid NTFS file name characters.

### File Name Replacements

The `--replace` option allows specific characters or strings in a file name to be replaced, e.g. `--replace a=b` will
replace all the `a` characters in the file name with `b`. This option can be used multiple times for multiple
replacements, e.g. `--replace a=b --replace foo=bar --replace c=''`. **Note: the arguments for this option are case
sensitive.**

Any values for the replace option containing an equals (`=`) cannot be set via command line arguments currently. To use
such values you can add them to the config file manually using a text editor.

## Tagging

### Manual Mode

AutoTag may not always select the correct TV series or movie, especially if there are multiple search results with the
same title. To work around this you can enable manual mode with the `--manual` option. This will display an interactive
menu for you to select the correct search result when searching for a match. Once manually selected that result will
be used for all subsequent files parsed with the same series name/movie title.

### Extended Tagging

The `--extended-tagging` option adds additional information to Matroska video files such as actors and their characters.
This option is not enabled by default because it may reduce tagging speed significantly due to the additional API
requests needed.

### Language

The language of the metadata can be set using the `-l` or `--language` option. This accepts
a [ISO 639-1 language code](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes) with
optional [ISO 3166 alpha-2 country code](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2) for regional variants. E.g.,
to get metadata in German use `-l de`, or for Brazilian Portuguese use `-l pt-BR`. Note that the data for other
languages is probably less complete than it is for English. If data in a given language is not available it will fall
back to some alternative, likely English.

Additional fallback languages for searching can be specified via the `--search-languages` option. For example,
with `-l pt-BR` and `--search-languages en-US` AutoTag will write metadata in Brazilian Portuguese,
but will retry searches in English if the Portuguese search fails.

### Alternate Episode Orderings (Episode Groups)

The `--episode-group` option allows you to choose one of the additional episodes group collections created on TMDB as
source for the episode ordering. All contained episode groups must follow the naming scheme `<NAME> XX`. Episode groups
whose names begin with `special` in their names are also valid and will be treated as `Season 0`.

Enabling this option will prompt you to select the episode ordering for each show manually.

| Group Name      | Valid |
|-----------------|-------|
| Season 01       | ✅     |
| Staffel 02      | ✅     |
| Volume 9        | ✅     |
| Special         | ✅     |
| Season 3 Part 1 | ❌     |
| Volume Part 1   | ❌     |

## Config

AutoTag creates a config file to store default preferences at `~/.config/autotag/conf.json` or
`%APPDATA%\Roaming\autotag\conf.json`. A different config file can be specified using the `-c` option. If the file does
not exist, a file will be created with the default settings:

```
"configVer": 14,                          // Internal use
"mode": 0,                                // Default tagging mode, 0 = TV, 1 = Movie, 2 = Auto
"manualMode": false,                      // Manual tagging mode
"verbose": false,                         // Verbose output
"addCoverArt": true,                      // Add cover art to files
"tagFiles": true,                         // Write tags to files
"renameFiles": true,                      // Rename files
"organizeFolders": false,                 // Move files into TV season folders or movie folders
"removeEmptyFolders": false,              // Remove source folders after moving files if they are empty
"tvRenamePattern": "%1 - %2x%3:00 - %4",  // Pattern to rename TV files, %1 = Series Name, %2 = Season, %3 = Episode, %4 = Episode Title
"movieRenamePattern": "%1 (%2)",          // Pattern to rename movie files, %1 = Title, %2 = Year
"parsePattern": "",                       // Custom regex to parse TV episode information
"windowsSafe": false,                     // Remove any invalid Windows file name characters
"extendedTagging": false,                 // Add more information to Matroska file tags
"appleTagging": false,                    // Add extra tags to mp4 files for use with Apple devices and software
"renameSubtitles": false,                 // Rename subtitle files
"language": "en",                         // Metadata language,
"searchLanguages": [],                    // Additional fallback languages to use when searching movies on TMDB
"includeAdult": false,                    // Include adult titles in TMDB searches
"episodeGroup": false,                    // Enable alternate episode ordering selection
"fileNameReplaces": []                    // File name character replacements. Array of objects of the form { "replace": "", "replacement": "" }
```

## Known Issues

- Some files will refuse to tag with an error such as "File not writeable" or "Invalid EBML format read". This is caused
  by the tagging library taglib-sharp, which sometimes refuses to tag certain files. The cause of this isn't immediately
  clear, but a workaround is to simply remux the file using ffmpeg (`ffmepg -i in.mkv -c copy out.mkv`), after which the
  file should tag successfully.

## Download

Downloads for Linux, macOS and Windows can be found [here](https://github.com/jamerst/AutoTag/releases).

The macOS build is untested, I don't own any Apple devices so I can't easily test it. Please report any issues and I'll
try to investigate them.

Build file sizes are quite large due to bundled .NET runtimes.

## Development

To run AutoTag from source, install the .NET 10 SDK and set your TMDB API key with the `TMDB_API_KEY` environment
variable:

PowerShell:

```powershell
$env:TMDB_API_KEY="your_tmdb_api_key"
```

Linux/macOS:

```sh
export TMDB_API_KEY="your_tmdb_api_key"
```

You should also set the environment variable within your IDE to allow debugging. If using Jetbrains Rider you will need
to set it in the run configuration for each project and also as an MSBuild Global Property under Settings > Build,
Execution, Deployment > Toolset and Build.

Note that the API key only needs to be set at build-time, it is inlined into the output so doesn't need to be set at
runtime.

To run the CLI run `dotnet run --project AutoTag.CLI -- [arguments]`.

### Testing

#### Unit Tests

Unit tests should be implemented for any new features. AutoTag uses xUnit and AwesomeAssertions for tests. Unit tests
can be executed by running `dotnet test AutoTag.Core.Test`.

**Note: unit tests should avoid side effects (e.g. writing files to disk) and should work cross-platform.**

#### Integration Tests

Integration tests for the command-line interface should also be implemented for any large/main path features, but they
don't need to have full coverage.

Integration tests can be executed by running `dotnet test AutoTag.CLI.Test`. Tests are run against a production build so
you will need to set your TMDB API key as above.

Integration test guidelines:

- Integration tests should provide assurance that the main key features of AutoTag are functioning correctly.
- Unit tests are preferred for complex or lesser-used features as they are easier to implement and faster to execute.
- Integration tests can have side effects (e.g. write files to disk), but these should be cleaned up automatically and
  avoid conflicts with other tests to allow parallel execution.
- **Integration tests must work cross-platform** - the GitHub workflows run them under Linux, macOS and Windows.

## Attributions

- File tagging provided by [taglib-sharp](https://github.com/mono/taglib-sharp)
- TheMovieDB API support provided by [TMDbLib](https://github.com/LordMike/TMDbLib)
- Data sourced from [themoviedb.org](https://www.themoviedb.org/) using their free API
