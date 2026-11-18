@echo off
chcp 65001 > nul
cd /d "%~dp0"
title Сборка Dynamic Island для Windows 10

echo ========================================================
echo   Сборка Dynamic Island для Windows 10
echo ========================================================
echo.

:: 1. Проверяем наличие dotnet в PATH или стандартной папке Program Files
set "DOTNET_CMD=dotnet"

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles%\dotnet\dotnet.exe"
        set "PATH=%ProgramFiles%\dotnet;%PATH%"
    ) else if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles(x86)%\dotnet\dotnet.exe"
        set "PATH=%ProgramFiles(x86)%\dotnet;%PATH%"
    ) else (
        echo [ОШИБКА: .NET SDK не найден]
        echo.
        echo Пожалуйста, установите официальный .NET 8 SDK от Microsoft:
        echo https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe
        echo.
        echo Если вы уже установили его прямо сейчас - перезагрузите компьютер
        echo или перезапустите Проводник, чтобы Windows обновила пути.
        echo.
        pause
        exit /b 1
    )
)

echo Обнаружен .NET: %DOTNET_CMD%
echo Идет сборка программы в один файл DynamicIsland.exe...
echo Пожалуйста, подождите 15-20 секунд...
echo.

"%DOTNET_CMD%" publish "%~dp0DynamicIsland\DynamicIsland.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0Publish"

if exist "%~dp0Publish\DynamicIsland.exe" (
    echo.
    echo ========================================================
    echo   [УСПЕХ!] Файл успешно создан:
    echo   %~dp0Publish\DynamicIsland.exe
    echo ========================================================
    echo.
    echo Запуск Dynamic Island...
    start "" "%~dp0Publish\DynamicIsland.exe"
    timeout /t 3 >nul
    exit /b 0
) else (
    echo.
    echo [ОШИБКА] Не удалось создать DynamicIsland.exe.
    echo Код ошибки: %errorlevel%
    echo.
    pause
    exit /b 1
)
