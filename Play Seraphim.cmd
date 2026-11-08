@echo off
if not exist "%~dp0Builds\Windows\Seraphim.exe" (
    echo Build Seraphim first by double-clicking Build Seraphim.cmd.
    pause
    exit /b 1
)
start "" "%~dp0Builds\Windows\Seraphim.exe"
