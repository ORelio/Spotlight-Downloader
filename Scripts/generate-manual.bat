@echo off
cd "%~dp0"

:: This script saves usage info as a text file for convenience

SpotlightDownloader > Manual.txt 2>&1
start Notepad Manual.txt