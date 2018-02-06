@echo off
cd "%~dp0"

:: This script downloads as much images as possible from the Spotlight API,
:: with maximum res and metadata. Please note that there is no actual way
:: of listing *all* images so SpotlightDownloader will make many API calls
:: to discover and download new images, and stop when no new images are
:: discovered. It may miss a few images but you should get most of them.

mkdir SpotlightArchive > nul 2>&1
SpotlightDownloader download --many --maxres --metadata --outdir SpotlightArchive