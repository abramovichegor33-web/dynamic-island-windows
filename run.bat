@echo off
chcp 65001 > nul
echo Запуск Dynamic Island...
dotnet run --project DynamicIsland\DynamicIsland.csproj -c Release
pause
