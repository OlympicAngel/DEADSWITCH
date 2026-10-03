# SPEC-010: Logout projection and local notifications

- Status: In progress (F-017)
- Pillar: Defense & offline, AI relationship
- Touches: offline catch-up (ADR-0004), raids (warnings while away, the AI's estimate), construction and research timers, settings (opt-in)
- Source rules: ADR-0004 decision 3, doc 11 "Notifications" surface priority, AGENTS design quality (no coercive notifications)

## Goal
When the handler leaves, the AI forecasts the next day and, only if the handler opted in, schedules a few local notifications in its own voice: raids it expects (with its estimate, which may be wrong), finished construction, restored modules, a power failure. Player value: no reason to keep checking in; come back when something actually needs a decision.

## Non-goals
Server push, rich media, notification actions, tribute reminders (F-018+).

## Rules
1. **Projection:** on logout the host copies the sim (save round-trip), marks the handler away, runs it `notify.project_hours` *(tune: 24)* forward and reads the copy's new events. The live sim is untouched.
2. **Alerts:** raid warnings (gate and estimate as the AI would report them), construction complete, module restored, blackout. At most `notify.max` *(tune: 6)*, earliest first, each category switchable.
3. **Opt-in:** off by default; the OPS screen has the toggle with one sentence on what it sends. Nothing is scheduled while off. All scheduled alerts are cancelled when the app returns.
4. **Tone:** short, factual, AI voice; no guilt, no countdown pressure, no "come back" lines.

## Acceptance criteria
- [ ] Projection is a pure function of the live sim (live state hash unchanged) and lists the expected raids
- [ ] Opt-in toggle; schedule on pause/quit, cancel on resume
- [ ] Mobile backend compiles only when com.unity.mobile.notifications is installed (no hard dependency)

## Tests
`ProjectionTests`: live sim unchanged; projected raid warnings match a copy run forward.
