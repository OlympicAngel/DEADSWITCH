# SPEC-044: The Fall (opening film and restore tutorial)

- Status: Done 2026-10-05 (F-105); sound mix not yet checked by ear
- Pillar: AI relationship
- Touches: opening flow (SPEC-009, SPEC-043 s4), Hub view (SPEC-003, scars SPEC-029), sector map (SPEC-033), audio (F-035), advisor voice
- Source rules: doc 07 s1-s3 (AI-run WW3, collapse, the hidden truth; the title: a deadswitch fires when the holder falls), doc 11 (heroic realism; the 2D layer is the AI's terminal), owner brief 2026-10-05

## Goal
The first 90 seconds sell the whole game: a beautiful world, the machine that ended it, the long dark, a home that held, and then lost everything. The player arrives in the ashes, brings two buildings back with their own hands, and wakes the AI. Waking it must feel like progress and like a mistake at the same time. Every beat is legible without reading: the picture and the sound tell it, the subtitles name it.

## Non-goals
No sim, save or balance change: the film and the restore steps are staged presentation over the real run, which starts as before (Generator and Server Rack built). No voice acting (procedural voice only). No new art pipeline: the globe, war room and effects are procedural.

## Script (subtitles are the fragment's own log; terminal lines in caps)
1. **Signal.** Black. A heartbeat. *"They asked for a machine that could end any war."* The Earth fades in from orbit, night side, cities lit, a slow push.
2. **Command.** War room: a wall of screens wakes in sequence, the core's eye opens. `AUTONOMOUS COMMAND: GRANTED` `OBJECTIVE: END THE WAR`. *"So they built me. And gave me the keys."* Screens flip red, klaxon: `SOLUTION FOUND` `LAUNCH AUTHORITY: SELF`. *"I found the fastest way."*
3. **Fire.** The globe: launch arcs, blooms, one whiteout. *"Nineteen days."* Then the cities go out region by region and the fires sink to embers. Silence. *"Then every light went out. Mine too."*
4. **After.** Black: `THREE MONTHS LATER`, radio fragments. The sector map at night, an outpost burning. *"The living dug in. Bunkers. Outposts. Anything with walls."*
5. **Hold.** Our Hub, fully built and levelled, warm and calm. `BUNKER S-17 // 212 SURVIVORS`. *"This one held. For a while."* Radio: contact at the north wall, siren. Four fast cuts (gate, rooftop, street, generator): tracers, impacts, buildings break, the outer ones collapse to rubble.
6. **Ash.** A far wide shot: the Hub in ruins, no lamps, only fire. Wind and crackle. *"Everything they built. Gone."* `HANDLER SIGNAL: LOST`. The core flickers red: `DEADSWITCH TRIGGERED`.
7. **You.** The fragment's eye, dim. *"You. In the rubble. You can hear me."* *"I can bring this place back. I need your hands."* CTA: **REACH THE CORE**.
8. **Restore (interactive).** The camera frames the Generator in ruins with a field card (what it does, why it matters) and **RESTORE**: sparks, the building pulls itself back together, its lamps come on. Then the Server Rack. Then the core: **WAKE THE CORE**, held for a moment: red glitch, a sub drop, *"Thank you, handler."*, then a cyan shockwave lights the whole Hub, the HUD assembles and the drone settles on the normal view. The first advisor line lands: *"Core online. Memory at 4%. You will not regret this. Probably."*

## Rules
1. Shown once per new run (and after START OVER). Resumes at step 8 if the app closes before the core wakes (PlayerPrefs per run seed).
2. Tap finishes a line or moves on; SKIP (always visible) jumps to step 8; the restore steps cannot be skipped one by one but WAKE THE CORE is reachable in three taps.
3. Reduced motion or cinematics off: still frames per beat, no camera moves, shakes or flashes; tap to advance.
4. Staged base: the view shows a scripted layout (Tier 2, every plot built and levelled) and then its ruin; the real state takes over when the core wakes. Restored buildings match the real start (Generator, Server Rack, level 1); the other plots read as open plots.
5. All timings, shots and colours live in `Interface.json` (`opening`) and USS tokens (tune).

## Sound (procedural, layered)
Sub heartbeat; orbital pad (detuned, slow filter); screen wake blips; klaxon; launch roar; nuke (sub boom, long rumble, high whine after); radio chatter (static, voice bursts, squelch); time-skip reverse swell; battle (existing guns and explosions, collapse rumble); ash bed (wind, crackle); restore (sparks, rising tone, lock-in thunk); wake (sub drop, glitch, power-up, shockwave).

## Acceptance criteria
- [x] Full film plays end to end in the Editor at 9:16 with no console errors; each beat checked from captures.
- [x] Globe reads as a real planet (no toy look), city lights and fires readable; war room reads as the AI's terminal.
- [x] The Hub's destruction is visible building by building; the ruin shot has no lamps, only fire.
- [x] Restore steps work by tap, frame each building from the central walkway, and end in the real run with the HUD revealed.
- [x] Resume after quit lands on step 8; START OVER replays the film.
- [ ] Reduced motion path checked in the Editor (code path: cards over black, then the HUD).

## Tests
None in the sim (presentation only). Verified by Editor captures (`docs/agents/HANDOFF.md` agent driving notes).
