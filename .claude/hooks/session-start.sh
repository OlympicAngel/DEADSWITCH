#!/usr/bin/env bash
# SessionStart hook: make the .NET gate runnable in Claude Code cloud sessions.
# Local machines manage their own toolchain (tools/setup-env.ps1), so this only acts in remote sessions.
set -euo pipefail
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then exit 0; fi
if command -v dotnet >/dev/null 2>&1; then exit 0; fi
if command -v apt-get >/dev/null 2>&1; then
  (apt-get install -y dotnet-sdk-8.0 >/dev/null 2>&1 || (apt-get update >/dev/null 2>&1 && apt-get install -y dotnet-sdk-8.0 >/dev/null 2>&1)) \
    && echo "Installed dotnet $(dotnet --version)" || echo "WARN: could not install dotnet-sdk-8.0; run tools/check.sh after installing it."
fi
