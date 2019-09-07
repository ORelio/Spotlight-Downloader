@echo off
cd "%~dp0"

:: This script maintains a cache of 10 Spotlight pictures
:: and randomly defines a new Spotlight image as wallpaper
:: It will not archives pictures so old ones are deleted

mkdir SpotlightCache > nul 2>&1
SpotlightDownloader download --amount 10 --cache-size 10 --metadata --outdir SpotlightCache
SpotlightDownloader wallpaper --from-dir SpotlightCache --embed-meta --outname wallpaper