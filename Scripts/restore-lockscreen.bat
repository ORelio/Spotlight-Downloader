@echo off
cd "%~dp0"

:: This script restores the default lock screen image.
:: SpotlightDownloader performs a backup before overwriting the file,
:: so we just need to ask it to restore the backup.

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

SpotlightDownloader lockscreen --restore