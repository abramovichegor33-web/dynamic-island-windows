@echo off
cd /d "%~dp0"
if exist "%~dp0Publish\DynamicIsland.exe" (
    start "" "%~dp0Publish\DynamicIsland.exe"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
)
