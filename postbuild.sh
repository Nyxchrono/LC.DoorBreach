#!/bin/bash

# $1 = "$(TargetDir)$(TargetName).dll"
# $2 = "$(SolutionName)"
# $3 = "$(MSBuildProjectName)"
# $4 = "$(Version)"
# $5 = "$(Description)"

TAB=$'\t'
NL=$'\n'
PROFILES_DIR="/home/nyxchrono/.config/r2modmanPlus-local/LethalCompany/profiles"
PROFILE="Development"
URL="https://github.com/Nyxchrono/$2"

install $1 $PROFILES_DIR/$PROFILE/BepInEx/plugins/Testing/
install $1 ../Thunderstore/

# Update manifest.json in Thunderstore directory
echo "\
{$NL\
$TAB\"name\": \"$3\",$NL\
$TAB\"version_number\": \"$4\",$NL\
$TAB\"description\": \"$5\",$NL\
$TAB\"website_url\": \"$URL\",$NL\
$TAB\"dependencies\": [ \"BepInEx-BepInExPack-5.4.2100\" ]$NL\
}" > ../Thunderstore/manifest.json

exit 0
