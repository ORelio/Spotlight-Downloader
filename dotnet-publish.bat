@echo off
cd %~dp0
echo Running dotnet publish
echo.
dotnet publish
echo.
echo If the above command worked, exe should be in:
echo SpotlightDownloader\bin\Release\net9.0-windowsXXXXXXX\win-x64\publish
pause
