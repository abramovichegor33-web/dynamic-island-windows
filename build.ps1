$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $PSScriptRoot

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "   Dynamic Island Windows 10 - Сборка программы" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Поиск dotnet
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)?.Source
if (-not $dotnet) {
    $stdPath = "C:\Program Files\dotnet\dotnet.exe"
    if (Test-Path $stdPath) {
        $dotnet = $stdPath
    } else {
        $x86Path = "C:\Program Files (x86)\dotnet\dotnet.exe"
        if (Test-Path $x86Path) {
            $dotnet = $x86Path
        }
    }
}

if (-not $dotnet) {
    Write-Host "[ОШИБКА] .NET SDK не найден на компьютере!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Пожалуйста, установите официальный .NET 8 по ссылке:" -ForegroundColor Yellow
    Write-Host "https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Если вы уже установили его - перезагрузите компьютер или Проводник." -ForegroundColor Gray
    Write-Host ""
    Read-Host "Нажмите Enter для выхода..."
    exit 1
}

Write-Host "[1/2] Найден .NET: $dotnet" -ForegroundColor Green
Write-Host "[2/2] Идет сборка автономного файла DynamicIsland.exe..." -ForegroundColor Yellow
Write-Host "      (это займет около 15-20 секунд, подождите)..." -ForegroundColor Gray
Write-Host ""

$projPath = Join-Path $PSScriptRoot "DynamicIsland\DynamicIsland.csproj"
$outPath = Join-Path $PSScriptRoot "Publish"

& $dotnet publish $projPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $outPath

$exePath = Join-Path $outPath "DynamicIsland.exe"
if (Test-Path $exePath) {
    Write-Host ""
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host "   [УСПЕХ!] Файл DynamicIsland.exe успешно создан!" -ForegroundColor Green
    Write-Host "   Путь: $exePath" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Запускаем Dynamic Island прямо сейчас..." -ForegroundColor Cyan
    Start-Process $exePath
    Start-Sleep -Seconds 2
    exit 0
} else {
    Write-Host ""
    Write-Host "[ОШИБКА] Сборка не удалась. Пожалуйста, посмотрите вывод выше." -ForegroundColor Red
    Write-Host ""
    Read-Host "Нажмите Enter для выхода..."
    exit 1
}
