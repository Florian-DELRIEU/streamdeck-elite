# Run the unit tests (Elite.Tests) with the NUnit console runner restored by build.ps1.
# Usage (from any shell, including Git Bash), after build.ps1 (Debug):
#   powershell -NoProfile -ExecutionPolicy Bypass -File test.ps1
# Note: keep this file ASCII-only (Windows PowerShell 5.1 misreads UTF-8 without BOM).

Set-Location -LiteralPath $PSScriptRoot

$runner = Get-ChildItem -Path 'packages' -Directory -Filter 'NUnit.ConsoleRunner.*' |
    Sort-Object Name -Descending |
    ForEach-Object { Join-Path $_.FullName 'tools\nunit3-console.exe' } |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $runner) {
    Write-Error "nunit3-console.exe not found: run build.ps1 first (nuget restore)"
    exit 1
}

$dll = 'Elite.Tests\bin\Debug\Elite.Tests.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Error "$dll not found: run build.ps1 first (Debug)"
    exit 1
}

# --noresult is ignored by NUnit.ConsoleRunner 3.22.0: the XML report and the agent logs go to the
# work directory, next to the dll (bin/ is git-ignored) instead of the repository root
& $runner $dll '--work=Elite.Tests\bin\Debug' '--result=TestResult.xml'
$code = $LASTEXITCODE
if ($code -eq 0) { Write-Host "== TESTS OK" } else { Write-Host "== TESTS FAILED (exit code $code)" }
exit $code
