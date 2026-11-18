@echo off
chcp 65001 > nul
echo ========================================================
echo   Сборка Dynamic Island для Windows 10 (Оптимизированный)
echo ========================================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не найден!
    echo Установите .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Компиляция и сборка автономного .EXE файла...
dotnet publish DynamicIsland\DynamicIsland.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Publish

if %errorlevel% equ 0 (
    echo.
    echo ========================================================
    echo   [УСПЕХ] Готово! Исполняемый файл находится в:
    echo   Publish\DynamicIsland.exe
    echo ========================================================
) else (
    echo.
    echo [ОШИБКА] Сборка завершилась с ошибкой.
)

pause
