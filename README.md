# AutoTag <br/>[![GitHub release](https://img.shields.io/github/release/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub release](https://img.shields.io/github/downloads/jamerst/AutoTag/total.svg)](https://github.com/jamerst/AutoTag/releases) [![GitHub issues](https://img.shields.io/github/issues/jamerst/AutoTag.svg)](https://github.com/jamerst/AutoTag/issues)

### Automatic tagging and renaming of TV show episodes

Inspired by [Auto TV Tagger](https://sourceforge.net/projects/autotvtagger/), AutoTag is a small utility to make it very easy to organise your <sup>completely legitimate</sup> TV show collection.

AutoTag interprets the file name to find the specific series, season and episode, then fetches the relevant information from TheTVDB, adds the information to the file and renames it to a set format.

## Features
- Information fetched from thetvdb.com's API
- Configurable renaming and full metadata tagging, including cover art
- Command-line argument support (`AutoTag.exe file1 file2 folder1 etc`)
- Tested to work under Mono (woo, Linux support!)
- Supports mp4, m4v and mkv\* extensions (I can add more, but should you really be using other formats at this point?)

## Known Issues
- Matroska files cannot be tagged. taglib-sharp *supposedly* supports Matroska files, but for some reason I can't get tags to write. The renaming feature works fine however.

## Download
Downloads can be found [here](https://github.com/jamerst/AutoTag/releases)

## Attributions
- File name parsing provided by [SubtitleFetcher](https://github.com/pheiberg/SubtitleFetcher)
- File tagging provided by [taglib-sharp](https://github.com/mono/taglib-sharp)
- TheTVDB API support provided by [TvDbSharper](https://github.com/HristoKolev/TvDbSharper)
- Data sourced from [thetvdb.com](https://www.thetvdb.com/) using their free API
