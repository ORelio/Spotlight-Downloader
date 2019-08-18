@echo off
cd "%~dp0"

:: This script maintains a cache of 10 Spotlight pictures
:: and randomly defines a new Spotlight image as lockscreen
:: It will not archives pictures so old ones are deleted

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

:: The Spotlight directory will hold a cache of 10 images
:: for performing lockscreen updates without Internet access

mkdir SpotlightCache > nul 2>&1

:: Try retrieving a new set of images using a temporary directory
:: In case of success, old images are replaced with new ones

mkdir SpotlightCache2 > nul 2>&1
SpotlightDownloader download --amount 10 --metadata --outdir SpotlightCache2
if "%errorlevel%" == "0" (
    del /Q SpotlightCache\*
    move SpotlightCache2\* SpotlightCache\
)
rmdir SpotlightCache2

:: Regardless of whether the cache was updated, we pick a new pic
:: That way, the lockscreen gets updated even without Internet access

SpotlightDownloader lockscreen --from-dir SpotlightCache