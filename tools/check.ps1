<#
.SYNOPSIS  Full quality gate: restore, format check, build (warnings as errors), test.
#>
$ErrorActionPreference = "Stop"
Set-Location (Resolve-Path (Join-Path $PSScriptRoot ".."))

$dotnetRoot = $env:DOTNET_ROOT
$dotnetExe = if ($dotnetRoot) { Join-Path $dotnetRoot "dotnet.exe" } else { $null }
if (-not $dotnetExe -or -not (Test-Path $dotnetExe)) {
    $dotnetCommand = Get-Command dotnet -ErrorAction Stop
    $dotnetExe = $dotnetCommand.Source
}

function Invoke-Step {
    param([string]$Label, [scriptblock]$Cmd)
    Write-Host ("==> " + $Label) -ForegroundColor Cyan
    & $Cmd
    if ($LASTEXITCODE -ne 0) { throw ("Step failed: " + $Label) }
}

Invoke-Step "unity meta" { python tools/gen_meta.py --check }
Invoke-Step "uss lint" { python tools/uss_lint.py }
Invoke-Step "restore" { & $dotnetExe restore DEADSWITCH.sln }
Invoke-Step "format (verify)" { & $dotnetExe format DEADSWITCH.sln --verify-no-changes --severity warn --no-restore }
Invoke-Step "build" { & $dotnetExe build DEADSWITCH.sln -c Release --no-restore -warnaserror }
Invoke-Step "unity compile check" { & $dotnetExe build tools/UnityCompileCheck -c Release -warnaserror }
Invoke-Step "test" { & $dotnetExe test DEADSWITCH.sln -c Release --no-build }
Write-Host "All checks passed." -ForegroundColor Green
