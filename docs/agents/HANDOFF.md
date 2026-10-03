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

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-009)
- Done: `Hud.uxml`/`Hud.uss` (status strip: hub + clock, OVERRIDE pips, energy panel with meter + sparkline, compute, people/crew, core corruption gauge + band, next-timer chip, threat chip, advisor line, raid banner with DEFEND; command bar with procedural icons), `HudController` (live binding, animated counters, real-time countdowns), `AdvisorTicker` (typewriter + glitch), `ScreenRouter` (eased cross-fade), `LockedScreen` (Map, offline until M1), `Fmt` display helpers, `Icons` (procedural, mirrored in the preview). Previews: `docs/media/ui-hud.png`.
- Half-done / known issues: Base/Core/Ops screens register in F-010/F-011/F-012; until then those tabs do nothing.
- Next: F-010 3D base diorama.
- Decisions made (link ADR/spec): status strip kept under ~25% of screen height so the 3D base dominates.

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-008)
- Done: design tokens (`Resources/UI/Tokens.uss`), component kit (`Components.uss`: corner-bracket panels, buttons, segmented meters, chips with shape pips, banner, tab bar, bottom sheet, key/value rows), fonts (Chakra Petch, IBM Plex Mono via `resource()`), `UiRoot` (runtime UIDocument + PanelSettings 1080x1920 portrait, layers, safe area), `Kit` behaviors, `Mesh2D` (feathered lines/arcs), `Sparkline`, `ArcGauge`, `Motion`/`Ease`/`AnimatedNumber`, `GlitchText`, `CrtOverlay` (scanlines, vignette, roll bar, corruption slices). `tools/uipreview` renders UXML/USS to PNG (`docs/media/ui-kit.png`).
- Half-done / known issues: not yet seen in the Unity Editor; USS features used (text-shadow, rotate, scale, translate transitions, var()) need Unity 6. Git LFS pushes are blocked from cloud sessions: fonts/screenshots stored as plain binaries.
- Next: F-009 terminal HUD + command bar.
- Decisions made (link ADR/spec): fonts Chakra Petch (display/labels) + IBM Plex Mono (values/body), OFL.

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-007)
- Done: `tools/UnityCompileCheck` (Unity reference assemblies, in gates + CI), `Deadswitch.Game` / `.Editor` asmdefs, `.toml` ScriptedImporter, code-only `Bootstrap`, `GameHost` (balance load, save/load, offline catch-up via `Deadswitch.Host.Timing.OfflineClock`, real-time ticking, autosave, presence on pause/resume, command path + event dispatch), `GameSettings`. `[host]` section in the balance file.
- Half-done / known issues: **not yet run in the Unity Editor** (no Editor in the cloud session). First thing on the owner's machine: open `unity/`, press Play in SampleScene, check the Console for `[DEADSWITCH]` messages. Editor scripts are not compile-checked.
- Next: F-008 visual system.
- Decisions made (link ADR/spec): real time, 1 tick = 1 real minute (dev time scale in debug builds).

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-006)
- Done: pressure loop per SPEC-001: `FixedMath` (Q16 log2/exp2/pow, power^0.7), corruption in milli-units with bands and automation load, OVERRIDE charges/regen/cooldown + Emergency Lockdown, Turret facility, garrison, postures (Turtle/Dark/Evacuate), presence command + offline penalty, raid lifecycle (warning with AI estimate -> resolve vs defense, breach-scaled loot + casualties), loss ledger events, mercy window. CLI `run --garrison/--posture/--away`. Event kind 1 renamed RaidStarted -> RaidWarning (pre-release, no saves shipped).
- Half-done / known issues: AI estimate error exists but nothing displays it yet (F-011). Raid cadence open question unchanged.
- Next: F-007 Unity foundation.
- Decisions made (link ADR/spec): SPEC-001 rewritten with all numbers in the balance file.

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-005)
- Done: economy core per SPEC-002: Hub slots, 4 facility kinds with per-level tables in the balance file, construction queue (build/upgrade/cancel/demolish), power priority with shedding + restart hysteresis, blackout when the AI core is unpowered, crew with unmanned (AI-run) output and automation load, Life Support pop cap, Battery energy cap, per-hour rates delivered exactly per tick, CLI economy readout. Merged main (art direction now stylized 3D, ADR-0007); my ADRs renumbered to 0008 (balance) and 0009 (save). Owner rule adopted: minimal testing.
- Half-done / known issues: none in sim. Backlog reshaped for the 3D base diorama (F-010).
- Next: F-006 pressure loop.
- Decisions made (link ADR/spec): SPEC-002; duplicate-build surcharge as the diminishing-returns rule (tune).

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-004)
- Done: `IStateVisitor` + `GameState.Visit` (fields declared once; hasher rebuilt on it), binary save format v1 (`SaveGame`, checksummed, state-hash verified, version gated), new `src/Deadswitch.Host` package (UPM `com.deadswitch.host`, in Unity manifest and sln) with crash-safe `SaveFileStore` (.tmp/.bak rotation, fallback load), CLI `run --save/--load`. 71 tests green, including every-byte-flip and every-truncation detection.
- Half-done / known issues: no save migrations yet (v1 only). Unity has not been opened with the new host package; its `.meta` files were generated by `tools/gen_meta.py`.
- Next: F-005 economy core (SPEC-002).
- Decisions made (link ADR/spec): ADR-0009.

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-003)
- Done: `Simulation.Step` split into `Systems/` over `SimContext` (hashes verified identical before adding state); command pipeline (`Command`, `CommandResult`/`RejectReason`, `CommandProcessor`, `CommandLog`), first command `SetDelegation`; typed `SimEvent` (Seq + 4 payloads, `EventLog.SchemaVersion`); `Replay`. 50 tests green.
- Half-done / known issues: `GameState` is still hashed by hand in `StateHasher`; F-004 replaces that with a single state visitor shared by hasher and save serializer.
- Next: F-004 save/snapshot format (ADR-0009).
- Decisions made (link ADR/spec): ADR-0003 amendment (commands apply between ticks; rejected commands leave no trace).

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-002)
- Done: sectioned `SimConfig` (`src/Deadswitch.Sim/Config/`), strict balance file reader/writer with line-numbered issues, config hash, `ConfigEntries` diff, shipped `src/Deadswitch.Sim/Resources/DeadswitchBalance.toml`, CLI `run/config dump/check/diff`, feel guards on defaults + shipped file, ADR-0008 (renumbered from 0007), `tools/gen_meta.py` (+ gate check). 37 tests green.
- Half-done / known issues: Bool/IntList readers have no shipped keys yet (add tests with the first one). Unity does not load the file yet (F-008: ScriptedImporter for `.toml` + Resources load).
- Next: F-003 commands and typed event log.
- Decisions made (link ADR/spec): ADR-0008 (balance; was 0007). Save format is ADR-0009.

---

### 2026-10-03 - Claude - claude/magical-ritchie-bx4xbb (F-001)
- Done: work loop for "continue" (`docs/agents/continue.md`, `TASK.md`, `docs/roadmap/BACKLOG.md`), production quality bar (`docs/agents/quality-bar.md`), skill pointers for Claude and Codex, `tools/check.sh`, cloud SessionStart hook that installs the .NET SDK, CI format step now blocking. Gate green on Linux.
- Half-done / known issues: none.
- Next: F-002 balance config (top Ready item in BACKLOG).
- Decisions made (link ADR/spec): owner direction recorded in quality-bar.md: no throwaway prototypes, all tunables in config, heavy investment in visuals.

---

### 2026-10-03 - Claude - art direction
- Done: switched art direction from 2D gritty painted realism to stylized 3D realism (semi-cartoonish PBR forms) seen through a subtle drone-feed camera; battle reports become graphic-novel panels of rendered stills. Added ADR-0007; updated docs 07, 08, 09 (superseded note), 10 (battle report wording + corrections log), 11, ADR-0001 context note, and `unity/README.md`.
- Half-done / known issues: no 3D assets or URP post-processing set up yet; camera-effect intensity needs device testing against the effect-intensity and reduced-motion settings.
- Next: concept-art passes for the five key screens in the new style; profile a URP base diorama on a low-end Android device early.
- Decisions made (link ADR/spec): [ADR-0007](../adr/0007-art-direction-stylized-3d.md).

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
