@echo off
cd "%~dp0"

:: This script makes a standard Spotlight API call and saves the result to SpotlightArchive
:: It also randomly defines a new Spotlight image as lockscreen from SpotlightArchive
:: This allows to gradually download images without hammering the Spotlight API
:: The script will skip downloading new images on metered connections

if not exist "%SYSTEMROOT%\System32\gpedit.msc" (
    echo Cannot set All Users lockscreen on Windows Home edition.
    pause > nul
    exit
)

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

mkdir SpotlightArchive > nul 2>&1
powershell -ExecutionPolicy Bypass -File check-metered.ps1 && ^
SpotlightDownloader download --metadata --outdir SpotlightArchive
SpotlightDownloader lockscreen --from-dir SpotlightArchive --embed-meta --outname lockscreen --allusers