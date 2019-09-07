@echo off
cd "%~dp0"

:: This script makes a standard Spotlight API call and saves the result to SpotlightArchive
:: It also randomly defines a new Spotlight image as lockscreen from SpotlightArchive
:: This allows to gradually download images without hammering the Spotlight API

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

mkdir SpotlightArchive > nul 2>&1
SpotlightDownloader download --maxres --metadata --outdir SpotlightArchive
SpotlightDownloader lockscreen --from-dir SpotlightArchive