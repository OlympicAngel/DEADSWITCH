# DEADSWITCH

> You control the last fragment of the war AI that ended the world. It keeps you alive. It might be lying to you.

Mobile, offline-first (online-ready), post-apocalyptic strategy. Hard, rough, infinitely scaling. Unity front end over a deterministic C# simulation core.

## Quick start (Windows)
```powershell
powershell -ExecutionPolicy Bypass -File tools\setup-env.ps1  # toolchains, caches, and temp files on D:
powershell -ExecutionPolicy Bypass -File tools\check.ps1      # restore, format, build (warnings as errors), test
& "$env:DOTNET_ROOT\dotnet.exe" run --project src\Deadswitch.Cli -- 42 24  # headless sim: seed 42, 24 hours
```

The check script prefers the SDK in `DOTNET_ROOT`. For direct .NET CLI commands, use that SDK explicitly so a machine-wide .NET installation earlier on `PATH` does not select a different SDK.
Unity: see [`unity/README.md`](unity/README.md).

## Where things are
| | |
|--|--|
| Game design | [`docs/design/`](docs/design/00_index.md) - start with `10_resolved_decisions.md` |
| Architecture decisions | [`docs/adr/`](docs/adr/README.md) |
| What to build next | [`docs/roadmap/ROADMAP.md`](docs/roadmap/ROADMAP.md) |
| Active feature specs | [`docs/specs/`](docs/specs/) |
| AI advisor writing | [`docs/narrative/ADVISOR_VOICE.md`](docs/narrative/ADVISOR_VOICE.md) |
| Agent instructions | [`AGENTS.md`](AGENTS.md) (Codex and humans), [`CLAUDE.md`](CLAUDE.md) (Claude Code) |
| Session handoff log | [`docs/agents/HANDOFF.md`](docs/agents/HANDOFF.md) |

## Design pillars (ranked)
1. AI relationship  2. Base & economy  3. Defense & offline attacks  4. Offense & diplomacy

## Status
M0 foundations. See the roadmap.
