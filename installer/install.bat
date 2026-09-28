@echo off
rem AutoPDF Pro - install for all users (asks for administrator rights)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" %*
pause
