@echo off
rem AutoPDF Pro - install for the current user only (no administrator rights)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -PerUser %*
pause
