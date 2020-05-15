@echo off

ver | findstr 10.0 >nul
if "%errorlevel%" == "0" (
    explorer ms-settings:lockscreen
)