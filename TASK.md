# TASK: F-099 polish and balance pass (last)

- Status: In progress (owner steps). Every design feature buildable in a cloud session is in (F-001..F-063, 2026-10-05 gap audits clean).
- Branch: claude/confident-heisenberg-m3gwju

## Steps
- [x] Balance pass with the runner (doc 10 corrections log, SPEC-014 findings)
- [x] Missing doc 10 rules: ambushes, highest heat on the HUD
- [x] Corruption visuals on the base; crewed glitches; dilemmas wait for the handler
- [x] Play in the Unity Editor (UI layout, lighting, damage FX, report stills fixed; open items in docs/agents/HANDOFF.md)
- [x] Night report stills readable (BaseLook.reportNightBoost)
- [ ] Editor pass for F-055..F-063 (HANDOFF checklist: sector map render + MapRender URP path, pins, overlay, gestures, new facilities, OPS forces line, premium ad grants, mobile notifications package resolve)
- [ ] Android phone run (owner)
- [ ] Fix whatever the phone run finds; open Editor items: puddle reflections, ALLY/SABOTAGE/RECALL, reactor, themes

## Needs the owner (accounts, services or native tooling)
Code is in; each service compiles only once its package is installed (package-gated asmdefs).
- Cloud backup (doc 10 s2): install `com.unity.services.cloudsave` and link the Unity Cloud project (`Cloud/Services`).
- Store and ads (ADR-0006): install `com.unity.purchasing` and `com.unity.ads`, fill `Resources/Store/StoreIds.json` (product ids, ad game ids).
- Widget (doc 08 s5): copy `unity/NativeWidgets~` into place per its README (Android androidlib; iOS widget extension + App Group).
- Editor resolve and device tests for all of the above.

## Blocked / questions
- See "Needs the owner".
