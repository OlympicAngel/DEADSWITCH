<#
.SYNOPSIS
  Move whatever is currently in the working folder into _legacy/ (nothing is deleted) and lay this scaffold down.
.DESCRIPTION
  Safe by default: runs as a DRY RUN and only prints the plan. Add -Apply to execute.
  Steps on -Apply:
    1. If the folder is a git repo: tag 'pre-restructure' and switch to branch 'chore/restructure'.
    2. Write _legacy/INVENTORY.txt (every file path and size, minus build/cache folders).
    3. Move every top-level item except .git into _legacy/ (same-drive moves are instant).
    4. Copy this scaffold into the folder (robocopy), excluding .git and _legacy.
  You then merge anything worth keeping from _legacy/ by hand (or ask an agent to, using INVENTORY.txt).
.EXAMPLE
  pwsh tools\Restructure-Repo.ps1 -RepoPath "D:\coding\DEADSWITCH AI"            # dry run
  pwsh tools\Restructure-Repo.ps1 -RepoPath "D:\coding\DEADSWITCH AI" -Apply     # do it
#>
[CmdletBinding()]
param(
    [string]$RepoPath = "D:\coding\DEADSWITCH AI",
    [string]$ScaffoldPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$Apply
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $RepoPath)) { throw "Repo folder not found: $RepoPath" }
$RepoPath = (Resolve-Path -LiteralPath $RepoPath).Path
if ($RepoPath -eq $ScaffoldPath) { throw "RepoPath and ScaffoldPath are the same folder. Extract the scaffold elsewhere first." }
if ($RepoPath -match '^[cC]:') { throw "Refusing to operate on C:" }

$legacy = Join-Path $RepoPath "_legacy"
$items = Get-ChildItem -LiteralPath $RepoPath -Force | Where-Object { $_.Name -ne ".git" -and $_.Name -ne "_legacy" }
$isGit = Test-Path -LiteralPath (Join-Path $RepoPath ".git")

Write-Host "Repo:      $RepoPath"
Write-Host "Scaffold:  $ScaffoldPath"
Write-Host "Git repo:  $isGit"
if ($RepoPath -match '\s') { Write-Warning "The path contains a space. Consider renaming the folder to 'deadswitch' (ADR-0005)." }
Write-Host ""
Write-Host "Will move $($items.Count) top-level item(s) into _legacy\ :"
$items | ForEach-Object { Write-Host ("  - " + $_.Name) }
Write-Host "Then copy the scaffold in."

if (-not $Apply) {
    Write-Host ""
    Write-Host "DRY RUN. Nothing changed. Re-run with -Apply to execute." -ForegroundColor Yellow
    return
}

if ($isGit) {
    Push-Location $RepoPath
    try {
        git tag -f pre-restructure | Out-Null
        $branches = git branch --list "chore/restructure"
        if ([string]::IsNullOrWhiteSpace($branches)) { git switch -c chore/restructure | Out-Null } else { git switch chore/restructure | Out-Null }
    }
    finally { Pop-Location }
}

New-Item -ItemType Directory -Force -Path $legacy | Out-Null

$skip = '\\(\.git|Library|Temp|node_modules|bin|obj|_legacy)(\\|$)'
$inv = Get-ChildItem -LiteralPath $RepoPath -Recurse -Force -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch $skip } |
    ForEach-Object { "{0,12}  {1}" -f $_.Length, $_.FullName.Substring($RepoPath.Length + 1) }
$inv | Set-Content -LiteralPath (Join-Path $legacy "INVENTORY.txt") -Encoding UTF8
Write-Host "Wrote _legacy\INVENTORY.txt ($(@($inv).Count) files)"

foreach ($i in $items) {
    Move-Item -LiteralPath $i.FullName -Destination $legacy -Force
}

& robocopy "$ScaffoldPath" "$RepoPath" /E /XD ".git" "_legacy" "bin" "obj" /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Next: pwsh tools\setup-env.ps1 ; pwsh tools\check.ps1 ; review _legacy\ and port anything worth keeping ; git add -A"
