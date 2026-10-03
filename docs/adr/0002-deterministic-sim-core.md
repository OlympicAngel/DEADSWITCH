# ADR-0002: Engine-agnostic deterministic simulation core

- Status: Accepted
- Date: 2026-10-03

## Context
Attacks resolve while the player is offline and must be fair, readable and later verifiable by a server. Battle reports and replays need reproducible results.

## Decision
All game rules live in `src/Deadswitch.Sim` (netstandard2.1, no dependencies, no UnityEngine). Integer/fixed-point math only, seeded `Pcg32`, 1 tick = 1 game minute, single-threaded. Unity consumes the library as a local UPM package (`package.json` + `asmdef` in the same folder). Rules in `docs/agents/sim-determinism.md`.

## Consequences
- Good: fast headless tests and balancing via `Deadswitch.Cli`; identical results on every device; server can re-run the sim later; engine swap is possible.
- Note: build output is redirected to `/artifacts` (`Directory.Build.props`) so Unity never imports stray `bin/obj` DLLs from the package folder. Requires .NET SDK 8+.
- Bad: no floats means fixed-point discipline; designers cannot tweak rules by editing Unity scenes; every state field must be hashed.

## Alternatives considered
- Rules inside Unity MonoBehaviours: faster to start, impossible to verify or test headlessly.
- Floating point with tolerance: breaks bit-exact replay across CPUs.
