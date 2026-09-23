# Build Elite.sln: NuGet restore (packages.config) + MSBuild located via vswhere.
# Usage (from any shell, including Git Bash):
#   powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Release
# Prerequisites (see docs/L0-baseline-build.md): VS Build Tools 2022, .NET Framework 4.8 Developer Pack, C:\nuget\nuget.exe
# Note: keep this file ASCII-only (Windows PowerShell 5.1 misreads UTF-8 without BOM).

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Set-Location -LiteralPath $PSScriptRoot

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    Write-Error "vswhere.exe not found: install Visual Studio Build Tools 2022 (see docs/L0-baseline-build.md)"
    exit 1
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    Write-Error "MSBuild.exe not found via vswhere"
    exit 1
}

$nuget = 'C:\nuget\nuget.exe'
if (-not (Test-Path -LiteralPath $nuget)) {
    Write-Error "nuget.exe not found in C:\nuget (see docs/L0-baseline-build.md)"
    exit 1
}

Write-Host "== NuGet restore"
& $nuget restore Elite.sln -NonInteractive
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== MSBuild ($Configuration) using $msbuild"
& $msbuild Elite.sln "/p:Configuration=$Configuration" /m /nologo /v:minimal
exit $LASTEXITCODE
