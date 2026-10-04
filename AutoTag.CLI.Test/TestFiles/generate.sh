#!/usr/bin/env bash
# Regenerates the media files used by the tests (requires ffmpeg).
#
#   attachments.mkv  one second of black 64x64 video with a font attachment
#                    (application/x-truetype-font) and an old cover (image/jpeg)
#   cover.jpg        16x16 image used as new cover art
set -euo pipefail
cd "$(dirname "$0")"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

ffmpeg -v error -y -f lavfi -i color=c=blue:s=16x16 -frames:v 1 cover.jpg
ffmpeg -v error -y -f lavfi -i color=c=red:s=16x16 -frames:v 1 "$tmp/old-cover.jpg"
printf 'not a real font, only an attachment for the tests' > "$tmp/Font.ttf"
ffmpeg -v error -y -f lavfi -i color=c=black:s=64x64:d=1:r=1 -c:v libx264 -preset ultrafast -t 1 \
  -attach "$tmp/Font.ttf" -metadata:s:t:0 mimetype=application/x-truetype-font -metadata:s:t:0 filename=Font.ttf \
  -attach "$tmp/old-cover.jpg" -metadata:s:t:1 mimetype=image/jpeg -metadata:s:t:1 filename=cover.jpg \
  -map_metadata -1 -fflags +bitexact -flags:v +bitexact attachments.mkv
