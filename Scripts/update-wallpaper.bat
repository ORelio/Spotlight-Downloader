@echo off
cd "%~dp0"

:: This script maintains a cache of 6-7 Spotlight pictures
:: and randomly defines a new Spotlight image as wallpaper

:: The Spotlight directory will hold a cache of 6-7 images
:: for performing wallpaper updates without Internet access

mkdir SpotlightCache > nul 2>&1

:: Try retrieving a new set of images using a temporary directory
:: In case of success, old images are replaced with new ones

mkdir SpotlightCache2 > nul 2>&1
SpotlightDownloader download --metadata --outdir SpotlightCache2
if "%errorlevel%" == "0" (
    del /Q SpotlightCache\*
    move SpotlightCache2\* SpotlightCache\
)
rmdir SpotlightCache2

:: Regardless of whether the cache was updated, we pick a new pic
:: That way, the wallpaper gets updated even without Internet access

SpotlightDownloader wallpaper --from-dir SpotlightCache