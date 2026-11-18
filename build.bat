@echo off
chcp 65001 > nul
title Сборка Dynamic Island для Windows 10
echo ========================================================
echo   Сборка Dynamic Island для Windows 10 (Оптимизированный)
echo ========================================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [.NET SDK не найден в системе]
    echo.
    echo Пытаемся автоматически установить .NET 8 через winget...
    where winget >nul 2>nul
    if %errorlevel% equ 0 (
        winget install --id Microsoft.DotNet.SDK.8 -e --accept-source-agreements --accept-package-agreements
        echo.
        echo Если установка завершилась успешно, перезапустите этот файл (build.bat).
        pause
        exit /b 0
    ) else (
        echo [Внимание] Скачайте и установите бесплатный .NET 8 SDK вручную:
        echo https://dotnet.microsoft.com/download/dotnet/8.0
        echo После установки снова запустите этот файл.
        pause
        exit /b 1
    )
)

echo Компиляция и создание автономного .EXE файла...
echo Это займет около 15-30 секунд...
echo.

dotnet publish DynamicIsland\DynamicIsland.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Publish

if %errorlevel% equ 0 (
    echo.
    echo ========================================================
    echo   [УСПЕХ] Готово! Исполняемый файл собран:
    echo   %~dp0Publish\DynamicIsland.exe
    echo ========================================================
    echo.
    set /p runNow="Запустить Dynamic Island прямо сейчас? (Y/N): "
    if /i "%runNow%"=="Y" (
        start "" "%~dp0Publish\DynamicIsland.exe"
    )
) else (
    echo.
    echo [ОШИБКА] Сборка завершилась с ошибкой. Проверьте вывод выше.
    pause
)
