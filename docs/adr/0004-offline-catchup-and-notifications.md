# ADR-0004: Offline catch-up, clock-cheat handling, local notifications

- Status: Accepted
- Date: 2026-10-03

## Context
Offline-first: attacks and timers continue while the app is closed. No server exists yet to push notifications or to be the trusted clock.

## Decision
1. **Catch-up:** on launch, step the sim from the last tick to now. Step every tick up to a cap, then coarse-grain hourly beyond it. Coarse path never grants more than the tick path would.
2. **Clock-cheat:** track monotonic uptime and the last trusted network time. If the device clock jumps beyond tolerance, raise a **time desync** event (shown in-story as an AI glitch), cap offline gains, and never grant free progress.
3. **Notifications:** at logout, run the deterministic sim forward on a copy of the state to project likely attacks and schedule **local notifications** in the AI's voice. Server push comes with the online phase.
4. **Fairness:** offline attack cap (3 per 24h Tier 1, 4 Tier 2), mercy window, vacation shield (doc 10 section 4 and 2).

## Consequences
- Good: works with no backend; notifications double as the AI's forecast (which may be wrong when corrupted).
- Bad: projections go stale if the player acts later; local notification limits per OS; a determined cheater can still block network time (acceptable for single-player, revisit for leaderboards).

## Alternatives considered
- Wait for a server: delays the core loop.
