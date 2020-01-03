@echo off
cd "%~dp0"

:: This script makes a standard Spotlight API call and saves the result to SpotlightArchive
:: It also randomly defines a new Spotlight image as wallpaper from SpotlightArchive
:: This allows to gradually download images without hammering the Spotlight API
:: The script will skip downloading new images on metered connections

mkdir SpotlightArchive > nul 2>&1
powershell -ExecutionPolicy Bypass -File check-metered.ps1 && ^
SpotlightDownloader download --maxres --metadata --outdir SpotlightArchive
SpotlightDownloader wallpaper --from-dir SpotlightArchive --embed-meta --outname wallpaper