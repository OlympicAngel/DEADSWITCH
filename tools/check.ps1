<#
.SYNOPSIS  Full quality gate: restore, format check, build (warnings as errors), test.
#>
$ErrorActionPreference = "Stop"
Set-Location (Resolve-Path (Join-Path $PSScriptRoot ".."))

function Invoke-Step {
    param([string]$Label, [scriptblock]$Cmd)
    Write-Host ("==> " + $Label) -ForegroundColor Cyan
    & $Cmd
    if ($LASTEXITCODE -ne 0) { throw ("Step failed: " + $Label) }
}

Invoke-Step "restore" { dotnet restore DEADSWITCH.sln }
Invoke-Step "format (verify)" { dotnet format DEADSWITCH.sln --verify-no-changes --severity warn --no-restore }
Invoke-Step "build" { dotnet build DEADSWITCH.sln -c Release --no-restore -warnaserror }
Invoke-Step "test" { dotnet test DEADSWITCH.sln -c Release --no-build }
Write-Host "All checks passed." -ForegroundColor Green
