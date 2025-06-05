@echo off
cd "%~dp0"

:: This script restores the default lock screen image.
:: SpotlightDownloader performs a backup before overwriting the file,
:: so we just need to ask it to restore the backup.

SpotlightDownloader lockscreen --restore