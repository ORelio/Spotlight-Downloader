@echo off

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

schtasks /delete /tn SpotlightLockscreen /f
cmd /c restore-lockscreen.bat
del lockscreen.jpg >nul 2>&1
del lockscreen.png >nul 2>&1
del lockscreen.bmp >nul 2>&1
rmdir SpotlightCache /S /Q
