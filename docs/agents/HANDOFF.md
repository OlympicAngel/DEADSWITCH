# Agent handoff log

Append-only. Newest entry on top. Every agent session (Claude or Codex) adds an entry before ending.

## Template
```
### YYYY-MM-DD - <agent> - <branch>
- Done:
- Half-done / known issues:
- Next:
- Decisions made (link ADR/spec):
```

---

### 2026-10-03 - Claude - initial scaffold
- Done: repo structure, agent instructions, ADR-0001..0006, roadmap, SPEC-001, sim core skeleton (Pcg32, GameState, Simulation, StateHasher, EventLog), tests, CLI harness, CI, PowerShell tooling.
- Half-done / known issues: **C# was not compiled** when generated (no .NET in the authoring sandbox). First action: `pwsh tools/check.ps1` and fix anything it reports. PCG32 vector and the economy numbers were verified separately in Python.
- Corrected: Tier 1 energy generation +6 -> +8 (net was -1/min). Logged in doc 10, section 11.
- Next: M0 in `docs/roadmap/ROADMAP.md` (paper prototype), then write the advisor's first 50 lines.
