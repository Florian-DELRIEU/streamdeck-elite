# Rewrites Elite.CatalogGen\observed-keys.txt from the real journal files: journal keys the game writes but
# EliteJournalReader does not know yet (names and types only, never values). Review the diff before committing,
# then run build.ps1 to regenerate Elite\PropertyInspector\catalog.js.
# Usage (after build.ps1):
#   powershell -NoProfile -ExecutionPolicy Bypass -File update-observed-keys.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File update-observed-keys.ps1 -JournalDir "D:\Other\Journals"
# Note: keep this file ASCII-only (Windows PowerShell 5.1 misreads UTF-8 without BOM).

param(
    [string]$JournalDir = (Join-Path $env:USERPROFILE 'Saved Games\Frontier Developments\Elite Dangerous')
)

Set-Location -LiteralPath $PSScriptRoot

$generator = 'Elite.CatalogGen\bin\Debug\Elite.CatalogGen.exe'
if (-not (Test-Path -LiteralPath $generator)) {
    Write-Error "$generator not found: run build.ps1 first (Debug)"
    exit 1
}

& $generator scan $JournalDir $PSScriptRoot
exit $LASTEXITCODE
