@echo off
cd "%~dp0"

:: This script restores the default lock screen image from Windows 10.
:: SpotlightDownloader performs a backup before overwriting the file,
:: so we just need to define the backup file as lockscreen image.

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

SpotlightDownloader lockscreen --from-file "%systemroot%\Web\Screen\img200.jpg"