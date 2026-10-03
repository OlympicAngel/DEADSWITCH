# Roadmap

Build in pillar order: **1 AI relationship, 2 Base & economy, 3 Defense & offline, 4 Offense & diplomacy.**
Each milestone has an exit criterion. Do not start the next milestone until it is met.

## M0 - Foundations (this scaffold)
- [x] Repo structure, agent instructions, ADRs, CI, tooling
- [x] Sim skeleton: Pcg32, GameState, tick loop, event log, hasher, tests, CLI
- [x] `pwsh tools/check.ps1` green on your machine (Linux `tools/check.sh` verified)
- [ ] `tools/setup-env.ps1` run; all toolchains on `D:`
- [ ] Repo folder renamed to remove the space (optional but recommended)
- **Exit:** clean clone builds and tests pass on a fresh machine.

## M1 - Paper prototype: the pressure loop (Phase 0)
Spec: `docs/specs/SPEC-001-pressure-loop.md`
- [x] Energy, compute, people, corruption, one raid type in the sim
- [x] Defense setup input (posture + garrison slots) affecting raid outcome
- [x] Corruption bands 0-30 / 31-60 / 61-85 / 86-100 with effects hooks
- [x] OVERRIDE charges + shared cooldown (no UI)
- [ ] CLI scenario runner: scripted player behavior over N days, prints a loss ledger
- [ ] Guard tests for intended feel (cadence, caps, no blackout, mercy window)
- **Exit:** 30 simulated days across 100 seeds: no soft-locks, never wiped in one hit, raids feel like doc 10 section 4.

## M2 - AI relationship core (Phase 1, pillar 1)
- [ ] Coldness / Boldness dials (hidden), delegation levels (Manual / Delegated / Offline autopilot)
- [ ] Advisor line system with lie rules (`docs/narrative/ADVISOR_VOICE.md`); the first lie
- [ ] Hidden project clock (Dormant / Active / Advanced / Imminent) + Audit tool
- [ ] Module trunk M1-M3 + one field (8 nodes)
- [ ] Terminal HUD in Unity (text-first, always-visible essentials)
- **Exit:** a tester catches the first lie via cross-checking and says "clever", not "bug".

## M3 - Offline pressure (Phase 2, pillar 3)
- [ ] Logout/return flow, offline catch-up (ADR-0004), clock-cheat handling
- [ ] All four signatures (raid, siege, virus, purge) + purge warning ladder
- [ ] Mercy window, vacation shield, tribute standing orders
- [ ] Battle report (4-6 panels + loss ledger + Verify)
- [ ] Local notifications from projected attacks
- **Exit:** close the app prepared and feel slightly nervous; return and read exactly what was lost and why.

## M4 - Vertical slice (Phase 3)
- [ ] Cinematic prologue, boot sequence, early protection, first hit
- [ ] Living base view (diorama), battle scars, corruption visuals
- [ ] Audio pass: ambient dread, glitchy AI voice, silence before purge
- **Exit:** 30-minute opening playable end to end with art and audio.

## M5 - v1 content (Phase 4)
- [ ] Tiers 1-2, four module fields (32 nodes), three factions (Scavengers, Military, Cultists teaser)
- [ ] Raids + cyber offense, per-faction heat, scouts / AI prediction / spies
- [ ] Free demo (Tier 1) + premium unlock (ADR-0006), colorblind-safe HUD, assists
- **Exit:** closed beta build on Android and iOS.

## M6+ - Expansion and online
Tiers 3-4, conquer-and-hold, Corporate Holdouts, Ironman, season pass, relocation cycle polish, then alliances / leaderboards / world events with server verification.

## Standing risks
- Scope: mitigated by pillar order and the "touches two others" rule.
- Offline fairness: mercy window, readable reports, offline attack cap.
- AI lying feels like a bug: lie rules + traces + Audit.
