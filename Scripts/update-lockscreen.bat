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

mkdir SpotlightCache > nul 2>&1
SpotlightDownloader download --amount 10 --cache-size 10 --metadata --outdir SpotlightCache
SpotlightDownloader lockscreen --from-dir SpotlightCache