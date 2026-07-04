@echo off
setlocal

set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%PS%" (
    "%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
) else (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
)

echo.
echo Appuyez sur une touche pour fermer cette fenêtre.
pause >nul
