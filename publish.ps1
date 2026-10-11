# Збирає сервер разом із вебклієнтом у теку publish/:
# REST API роздає WebAssembly-клієнт зі свого wwwroot, тож усе працює з однієї адреси.
# З параметром -Runtime (наприклад, win-x64) сервер збирається самодостатнім:
# на цільовій машині не потрібно встановлювати .NET.
param([string]$Runtime)

$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'publish'
$web = Join-Path $PSScriptRoot 'artifacts/web'

# Очищаємо вміст, а не саму теку: вона може бути відкрита в терміналі чи провіднику.
if (Test-Path $out) { Get-ChildItem $out -Force | Remove-Item -Recurse -Force }
$target = if ($Runtime) { @('-r', $Runtime, '--self-contained', 'true') } else { @() }
dotnet publish "$PSScriptRoot/src/MyDbShumeiko.Api" -c Release -o $out @target
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet publish "$PSScriptRoot/src/MyDbShumeiko.Web" -c Release -o $web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Copy-Item (Join-Path $web 'wwwroot') (Join-Path $out 'wwwroot') -Recurse

$run = if ($Runtime) { "$out\MyDbShumeiko.Api.exe" } else { "dotnet $out/MyDbShumeiko.Api.dll" }
Write-Host "Готово. Запуск: $run --urls http://localhost:8080"
