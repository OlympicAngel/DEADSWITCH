#!/usr/bin/env bash
# Full quality gate (Linux/macOS/CI): restore, format check, build (warnings as errors), test.
# Mirror of tools/check.ps1. Extra gates (Unity compile check, UI preview) are added here as they land.
set -euo pipefail
cd "$(dirname "$0")/.."

DOTNET="${DOTNET:-dotnet}"
if [ -n "${DOTNET_ROOT:-}" ] && [ -x "$DOTNET_ROOT/dotnet" ]; then DOTNET="$DOTNET_ROOT/dotnet"; fi

step() { printf '\033[36m==> %s\033[0m\n' "$1"; }

step "restore";         "$DOTNET" restore DEADSWITCH.sln
step "format (verify)"; "$DOTNET" format DEADSWITCH.sln --verify-no-changes --severity warn --no-restore
step "build";           "$DOTNET" build DEADSWITCH.sln -c Release --no-restore -warnaserror
step "test";            "$DOTNET" test DEADSWITCH.sln -c Release --no-build
printf '\033[32mAll checks passed.\033[0m\n'
