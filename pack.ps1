# Package the Release build as the installable file dist\com.mhwlng.elite.streamDeckPlugin (docs/L9-finalisation.md).
# Usage (from any shell, including Git Bash), after build.ps1 -Configuration Release:
#   powershell -NoProfile -ExecutionPolicy Bypass -File pack.ps1
# Uses Elgato's DistributionTool.exe when it is found (tools\ or PATH; it also validates the plugin). Otherwise zips
# the plugin folder: a .streamDeckPlugin file is a zip whose root is the com.mhwlng.elite.sdPlugin folder.
# Debug symbols (*.pdb) and logs (*.log) are left out.
# Installing this file by double-click only works when the plugin is not installed yet: to update the installed
# plugin, use the robocopy procedure of CLAUDE.md ("Deployer").
# Note: keep this file ASCII-only (Windows PowerShell 5.1 misreads UTF-8 without BOM).

param(
    [string]$Source = 'Elite\bin\Release\com.mhwlng.elite.sdPlugin'
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$pluginFolder = 'com.mhwlng.elite.sdPlugin'
$packageName = 'com.mhwlng.elite.streamDeckPlugin'

$manifestPath = Join-Path $Source 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    Write-Error "$manifestPath not found: run build.ps1 -Configuration Release first"
    exit 1
}
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
if ((Split-Path -Leaf $sourcePath) -ne $pluginFolder) {
    Write-Error "the plugin folder must be named $pluginFolder (the plugin identifier comes from it)"
    exit 1
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
Write-Host ("== Packing {0} {1} ({2} actions)" -f $manifest.Name, $manifest.Version, $manifest.Actions.Count)

$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$package = Join-Path $dist $packageName
if (Test-Path -LiteralPath $package) {
    Remove-Item -LiteralPath $package -Force
}

$tool = $null
$local = Join-Path (Join-Path $PSScriptRoot 'tools') 'DistributionTool.exe'
if (Test-Path -LiteralPath $local) {
    $tool = $local
} else {
    $command = Get-Command 'DistributionTool.exe' -ErrorAction SilentlyContinue
    if ($command) { $tool = $command.Source }
}

# copy without debug symbols and logs, in a temporary folder that keeps the plugin folder name
$stage = Join-Path ([System.IO.Path]::GetTempPath()) ('zv-pack-' + [guid]::NewGuid().ToString('N'))
$stagePlugin = Join-Path $stage $pluginFolder
try {
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item -LiteralPath $sourcePath -Destination $stagePlugin -Recurse
    Get-ChildItem -LiteralPath $stagePlugin -Recurse -File |
        Where-Object { $_.Extension -in '.pdb', '.log' } |
        Remove-Item -Force

    if ($tool) {
        Write-Host "== DistributionTool: $tool"
        & $tool -b -i $stagePlugin -o $dist
        if ($LASTEXITCODE -ne 0) {
            Write-Host "== PACK FAILED (DistributionTool exit code $LASTEXITCODE)"
            exit $LASTEXITCODE
        }
    } else {
        Write-Host "== Zip (DistributionTool.exe not found in tools\ nor in the PATH)"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::CreateFromDirectory($stagePlugin, $package, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    }
} finally {
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
}

if (-not (Test-Path -LiteralPath $package)) {
    Write-Host "== PACK FAILED ($package was not created)"
    exit 1
}
$size = (Get-Item -LiteralPath $package).Length
Write-Host ("== PACK OK: dist\{0} ({1:N0} KB)" -f $packageName, ($size / 1KB))
exit 0
