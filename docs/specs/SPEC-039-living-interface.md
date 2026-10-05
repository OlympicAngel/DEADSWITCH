# SPEC-039: Living interface (UI, camera, motion, VFX rework)

- Status: In progress (F-100)
- Owner direction (2026-10-05): "most of the UI is cramped, plain text, no icons, no depth, no grouping, overwhelming"; resources must read like Travian / Clash of Clans; the camera should fly to and frame a building; attacks get a movie-like sequence; the AI interface becomes JARVIS-like; base and UI get a **restrained cyberpunk** accent (bold color, not too much).
- Canon change: doc 11 said "avoid generic neon cyberpunk". The owner now wants a slight cyberpunk accent on the **interface and AI technology only** (the world keeps heroic realism; bright color stays reserved for AI tech, alarms and interactables, which the master brief already allows). Recorded in the doc 10 corrections log and doc 11.

## Player value
Every screen answers in one glance: *where am I, what is wrong, what do I tap*. Resources show what is missing and what fixes it. The base reacts to touch (fly-in, highlight, quick actions), attacks feel like events, and the AI feels like a presence, not a log.

## Rules
1. **Hierarchy:** each screen has a header (icon + title + one-line status), grouped cards with icon headers, one primary action. Secondary detail collapses or moves behind a tap.
2. **Resources** show icon, value, capacity bar, net rate and a state (OK / LOW / SHORT / FULL). Tapping one opens a breakdown: sources, drains, time to full or empty, and "how to get more" shortcuts that fly the camera to the building that helps.
3. **Shortage is legible everywhere:** a cost the player cannot pay names the missing amount and the time until it is affordable.
4. **Camera focus:** selecting a facility flies the drone in (pan + zoom + slight orbit), dims the rest, pulses a highlight and opens quick actions beside it. Back / tap empty ground flies out.
5. **Attack sequence:** on contact a short cinematic (letterbox, title card, low-angle gate shot, push-in, impacts) plays once, skippable with a tap, off under reduced motion; it never delays input or decides anything.
6. **AI presence:** a holographic core orb (rings, waveform while speaking) carries the advisor; lines can carry actionable suggestion chips.
7. **Accent color:** a cyan "AI tech" accent and a hot magenta-pink highlight join the token set; used on AI surfaces, focus states and interactables only. Signal meanings (amber raid, red siege, magenta virus) never change.
8. All tunables in tokens (`Tokens.uss`) or `Resources/UI/Motion.json`-style config; reduced motion and effect intensity are honored everywhere.

## The 60 ideas
Status: **Planned** (this branch), **Planned** (planned), **Editor** (needs live Unity to tune).

### A. Visual language and tokens
1. Cyberpunk accent tokens: AI cyan, hot pink highlight, deeper glows; one place, themes keep working. *Done*
2. Depth tokens: three elevation levels (sunken, base, raised) with inner light edge and outer shadow strokes. *Done*
3. Glass panels: translucent "holo" panel variant with a top light edge for AI surfaces. *Done*
4. Section header component: icon + title + count/status chip + divider. *Done*
5. Card component with icon well, title, value and a footer action. *Done*
6. Shared icon set in one data file (`Resources/UI/Icons.json`) read by Unity and the preview; 40+ glyphs. *Done*
7. Progress bars with a glowing head and a time label (build, research, repair). *Done*
8. Stat delta pill (`+12/h` green, `-30/h` red) with arrow glyph. *Done*
9. Empty states: every list says what to do when it is empty. *Planned*
10. Number formatting with k/M and a fixed-width digit font everywhere. *Planned*

### B. Resources (Travian / Clash of Clans readability)
11. Resource pods: icon badge, value, capacity bar, rate pill. *Done*
12. Pod states: FULL (production wasted), LOW (< 20% or < 1 h of drain), SHORT (negative and empty soon); shape + color + word. *Done*
13. Time-to-full / time-to-empty under each pod. *Done*
14. Tap a pod: resource breakdown sheet (producers, drains, net). *Done*
15. "How to get more" shortcuts in the breakdown that select and fly to the building that helps (or the empty plot to build one). *Done*
16. Costs show the missing amount ("NEED 140 MORE") and when it will be affordable at the current rate. *Done*
17. Floating "+N" pickups over producing buildings when stock ticks up (throttled). *Done*
18. Storage fill rendered on the battery bank / fuel depot model (lit cells). *Editor*
19. Pod flashes and the rate pill turns red when a drain starts (blackout warning lead time). *Done*
20. Production summary card on BASE when the player returns ("while you were away: +2,340 energy"). *Done*

### C. Base interaction and camera
21. Fly-to focus: pan + zoom-in + small orbit to frame the selected building. *Done*
22. Focus dims the rest of the base (labels fade, selection pulse ring). *Done*
23. Quick-action ring next to the building: UPGRADE, INFO, POWER, REPAIR, with costs. *Done*
24. Compact info card instead of the full sheet; "details" expands to the full sheet. *Done*
25. Swipe left/right in focus to hop to the next building. *Done*
26. Double-tap ground to zoom in/out between two presets. *Done*
27. Camera inertia: flick pans glide and settle; rubber-band at bounds. *Done*
28. Idle drift: after 20 s untouched, the drone slowly orbits (reduced motion: off). *Done*
29. Build-complete moment: camera nudges to the building, scan sweep, "ONLINE" stamp. *Done*
30. Upgrade construction: scaffold flicker and sparks while building. *Editor*
31. Holographic plot markers on empty plots ("+ BUILD" ring that breathes). *Done*
32. Data links: thin animated cyan lines from powered facilities to the core. *Done*
33. Ambient drones circling the compound, more at higher tiers. *Done*
34. Neon accent strips on AI-tech facilities (server rack, cooling, memory chamber). *Editor*

### D. Attack cinematic and battle feel
35. Contact sequence: letterbox bars, title card (signature shape + name + gate), 3 shots. *Done*
36. Shot 1: low-angle at the gate looking out at the attackers. *Done*
37. Shot 2: side dolly along the attackers' line. *Done*
38. Shot 3: high pull-back to the drone view, HUD returns. *Done*
39. Camera shake on shell impacts (scaled by effect intensity; off with reduced motion). *Done*
40. Impact flashes and debris bursts on shells. *Done*
41. Hit-stop: a 60 ms time dip on big impacts. *Planned*
42. Battle HUD: attacker/defender strength bars that move with the fight. *Planned*
43. Siren light sweep (red rotating light) on the bunker during attacks. *Done*
44. Aftermath shot: smoke drift, damaged buildings framed, "DEFENSE HELD" / "BREACH" stamp. *Done*
45. Raid warning: red edge vignette pulse on the HUD frame, countdown in the frame. *Done*

### E. The AI (JARVIS-like)
46. Core orb: concentric rotating rings, tick marks and a pulsing heart; color follows the corruption band. *Done*
47. Voice waveform under the orb while a line is being typed. *Done*
48. Advisor comms panel: orb avatar + typed line + mood tag + timestamp. *Done*
49. Suggestion chips on advisor lines ("BUILD SOLAR", "OPEN OPS") that act in one tap. *Done*
50. Scan-sweep when the AI "thinks" (before a recommendation). *Done*
51. Corruption shows as orb ring stutter and color drift, never on the buttons. *Done*
52. Core screen: orb as the focal hero, diagnostics in grouped holo cards around it. *Done*
53. Boot / wake line when the app opens: the orb assembles. *Planned*

### F. Screens and QOL
54. OPS regrouped: Threat card, Posture grid, Forces, Autonomy, Away settings, each a titled card with icons; scrolls. *Done*
55. Command bar: active tab lifted with glow, badges with counts. *Done*
56. Toasts: short stacked confirmations with icons (built, upgraded, refused + reason). *Done*
57. Contextual header line under each screen title ("2 alerts // 1 job running"). *Done*
58. Badges on BASE for idle builders and full storage. *Done*
59. Long-press any value to see its explanation (tooltip card). *Planned*
60. Settings: "camera cinematics" and "focus fly-in" toggles beside reduced motion. *Done*

## Screenshots
Headless previews: `docs/media/li-*.png` (HUD, resource sheet, focus, cinematic, OPS, CORE, WORKFORCE, DISPATCH).

## Out of scope
Sim rules and balance (presentation only, no `GameState` change). Audio beyond existing cues.
