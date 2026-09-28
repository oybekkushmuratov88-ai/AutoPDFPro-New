@echo off
rem AutoPDF Pro - uninstall (add -Purge to also delete settings and logs)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0uninstall.ps1" %*
pause
