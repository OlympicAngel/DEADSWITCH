<#
.SYNOPSIS
  Point toolchains and caches at D: so nothing heavy lands on C:.
.DESCRIPTION
  Creates the folder layout under -DevRoot, sets user-level environment variables, and prints the
  install commands to run. Run once, then restart your terminal.
  Honest limit: Unity Hub and the Unity licensing/log folders still write small amounts to
  %APPDATA% and %LOCALAPPDATA% on C:. Editors, the .NET SDK, NuGet and the UPM cache can all be moved.
#>
[CmdletBinding()]
param([string]$DevRoot = "D:\dev")

$ErrorActionPreference = "Stop"
if ($DevRoot -match '^[cC]:') { throw "DevRoot must not be on C: (got $DevRoot)" }

$dirs = @("dotnet", "dotnet-cli", "nuget\packages", "unity\editors", "unity\hub", "unity\upm-cache", "git", "tmp")
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path (Join-Path $DevRoot $d) | Out-Null
}

$vars = @{
    "DOTNET_ROOT"                 = (Join-Path $DevRoot "dotnet")
    "DOTNET_CLI_HOME"             = (Join-Path $DevRoot "dotnet-cli")
    "NUGET_PACKAGES"              = (Join-Path $DevRoot "nuget\packages")
    "UPM_CACHE_PATH"              = (Join-Path $DevRoot "unity\upm-cache")
    "DOTNET_CLI_TELEMETRY_OPTOUT" = "1"
    "DOTNET_NOLOGO"               = "1"
    "TEMP_DEV"                    = (Join-Path $DevRoot "tmp")
    "TEMP"                        = (Join-Path $DevRoot "tmp")
    "TMP"                         = (Join-Path $DevRoot "tmp")
}
foreach ($k in $vars.Keys) {
    [Environment]::SetEnvironmentVariable($k, $vars[$k], "User")
    Set-Item -Path "Env:$k" -Value $vars[$k]
    Write-Host ("set {0} = {1}" -f $k, $vars[$k])
}

$dotnetDir = Join-Path $DevRoot "dotnet"
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($null -eq $userPath) { $userPath = "" }
if ($userPath -notlike "*$dotnetDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$dotnetDir;$userPath", "User")
    Write-Host "added $dotnetDir to user PATH"
}

Write-Host ""
Write-Host "Next steps (run once):"
Write-Host "  1. .NET 8 SDK on D:"
Write-Host "     Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $DevRoot\tmp\dotnet-install.ps1"
Write-Host "     & $DevRoot\tmp\dotnet-install.ps1 -Channel 8.0 -InstallDir $dotnetDir"
Write-Host "  2. Git for Windows: run the installer and choose $DevRoot\git as the install folder. Then: git lfs install"
Write-Host "  3. PowerShell 7 (pwsh): optional but the scripts work in Windows PowerShell 5.1 too."
Write-Host "  4. Unity Hub: after installing, Settings > Installs > Install location = $DevRoot\unity\editors"
Write-Host "     Install Unity 6 LTS (6000.x) with Android and iOS build support modules."
Write-Host "  5. Restart the terminal, then run: powershell -ExecutionPolicy Bypass -File tools\check.ps1"
Write-Host "     Direct .NET commands should use: & `"$dotnetDir\dotnet.exe`" <arguments>"
