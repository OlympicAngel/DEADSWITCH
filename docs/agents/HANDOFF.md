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

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-001)
- Done: work loop for "continue" (`docs/agents/continue.md`, `TASK.md`, `docs/roadmap/BACKLOG.md`), production quality bar (`docs/agents/quality-bar.md`), skill pointers for Claude and Codex, `tools/check.sh`, cloud SessionStart hook that installs the .NET SDK, CI format step now blocking. Gate green on Linux.
- Half-done / known issues: none.
- Next: F-002 balance config (top Ready item in BACKLOG).
- Decisions made (link ADR/spec): owner direction recorded in quality-bar.md: no throwaway prototypes, all tunables in config, heavy investment in visuals.

---

### 2026-10-03 - Copilot - repository setup
- Done: resumed Editor validation using the existing Unity Hub session (the user confirmed Hub is signed in); Unity 6000.3.25f1 opened `unity/`, resolved the existing Unity Personal entitlement, resolved URP dependencies and the local `com.deadswitch.sim` package, and produced `Assembly-CSharp` and editor assemblies without compiler errors. Unity import/cache artifacts and the editor log are on D:; C: remained at about 2.07 GB free. Updated the Unity README to clarify that Hub sign-in and Unity CLI auth are separate.
- Half-done / known issues: Unity CLI itself reports no CLI sign-in, but this did not block opening the Editor through the Hub session or resolving the existing entitlement. No reason to request another sign-in. This is an empty project scaffold; application gameplay/UI implementation remains future work. iOS archive/signing requires macOS/Xcode.
- Next: continue feature development in `unity/Assets` and `src/Deadswitch.Sim`; keep generated Unity `Library/` and caches out of version control. `powershell.exe -NoProfile -ExecutionPolicy Bypass -File D:\coding\DEADSWITCH\tools\check.ps1` is the validated .NET quality gate.
- Decisions made (link ADR/spec): used the catalog's blank URP starter because Unity 6.3 did not expose a dedicated 2D URP template; URP remains compatible with the planned 2D workflow.

---

### 2026-10-03 - Copilot - repository setup
- Done: completed Unity 6.3 LTS 6000.3.25f1 installation under `D:\dev\unity\editors` with Android (SDK/NDK/OpenJDK) and iOS Build Support; generated the URP blank project in `unity/`, recorded the exact editor version, registered the repo project with Unity Hub, and linked `src/Deadswitch.Sim` as a local UPM package. Updated `unity/README.md` with the actual template/platform setup and workstation license note.
- Half-done / known issues: Unity CLI is not signed in, so the Editor has not been opened to resolve packages or verify compilation. C: has about 2.1 GB free; Hub profile/licensing metadata remains there and the Hub download cache is redirected to D:.
- Next: sign in and activate a Unity Personal license in Hub, open `unity/`, confirm the local package resolves, and run the Unity compile check. iOS archive/signing requires macOS/Xcode.
- Decisions made (link ADR/spec): used the catalog's blank URP starter because Unity 6.3 did not expose a dedicated 2D URP template; URP remains compatible with the planned 2D workflow.

---

### 2026-10-03 - Copilot - repository setup
- Done: adapted the useful design/engagement guidance from `D:\coding\DEADSWITCH AI` into canonical playbooks and synchronized Codex/Claude skill pointers; added provisional visual/motion guidance without changing doc 10 canon. Configured user development caches and temp folders on D:, installed .NET SDK 8.0.425, Unity Hub 3.22.2, and Unity CLI under D:, set the Editor install path to `D:\dev\unity\editors`, redirected the Hub download cache physically to D:, and verified Git LFS was already configured.
- Half-done / known issues: Unity Editor installation was paused after the user reported only about 2 GB free on C:. No Editor is installed yet and the Unity project has not been generated. Hub profile/licensing metadata remains on C: as approved; no Unity CLI executable or download payload remains there.
- Next: investigate the reported C: space drop before resuming Editor installation; then install Unity 6 LTS with the user-approved Android/iOS module terms, create the 2D URP project, and wire the local simulation package.
- Decisions made (link ADR/spec): no canon or balance changes; prototype palette values remain provisional.

---

### 2026-10-03 - Claude - initial scaffold
- Done: repo structure, agent instructions, ADR-0001..0006, roadmap, SPEC-001, sim core skeleton (Pcg32, GameState, Simulation, StateHasher, EventLog), tests, CLI harness, CI, PowerShell tooling.
- Half-done / known issues: **C# was not compiled** when generated (no .NET in the authoring sandbox). First action: `pwsh tools/check.ps1` and fix anything it reports. PCG32 vector and the economy numbers were verified separately in Python.
- Corrected: Tier 1 energy generation +6 -> +8 (net was -1/min). Logged in doc 10, section 11.
- Next: M0 in `docs/roadmap/ROADMAP.md` (paper prototype), then write the advisor's first 50 lines.
