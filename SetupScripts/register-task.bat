@echo off

net session > nul 2>&1
if not "%errorlevel%" == "0" (
    echo Please run me as administrator^!
    pause > nul
    exit
)

:: https://stackoverflow.com/a/2997161
:: Get SID for current user for registering the task as this user
for /f "delims= " %%a in ('"wmic path win32_useraccount where name='%USERNAME%' get sid"') do (
   if not "%%a"=="SID" (          
      set USERID=%%a
      goto :loop_end
   )   
)
:loop_end

if exist "%SYSTEMROOT%\System32\gpedit.msc" (
    set LOCKSCREEN_SCRIPT=update-lockscreen-all-users.bat
) else (
    set LOCKSCREEN_SCRIPT=update-lockscreen.bat
)

type task-part1.xml > task.xml
echo       ^<UserId^>%USERID%^</UserId^> >> task.xml
type task-part2.xml >> task.xml
echo       ^<Arguments^>"%CD%\hide-console.vbs" "%CD%\%LOCKSCREEN_SCRIPT%"^</Arguments^> >> task.xml
type task-part3.xml >> task.xml

schtasks /delete /tn SpotlightLockscreen /f >nul 2>&1
schtasks /create /TN SpotlightLockscreen /xml task.xml
del task.xml

cmd /c %LOCKSCREEN_SCRIPT%
