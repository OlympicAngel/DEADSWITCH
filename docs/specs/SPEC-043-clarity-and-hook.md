# SPEC-043: Clarity and hook (plain language, no monetization, new opening, engagement)

- Status: Done (code) 2026-10-05 (F-104); Editor check pending
- Owner direction (2026-10-05): "rework the naming, each thing should be clear, simple language (what is crew / garrison / ironman?); no leftover or unconnected features, no placeholders; drop the pay guard and any ads; rework the startup story (story, theme, animation, effects); think of anything missing from a top-notch addictive game."

## 1. One name per concept (player-facing text only; code identifiers stay)
Canon glossary words stay (Core, Hub, Outpost, Corruption, Heat, Override, Signature, Purge, Relocation) and get a Field Guide entry.

| Was | Now | Meaning in one line |
|---|---|---|
| Garrison, on the wall, defenders | **DEFENDERS** | People posted on the wall to fight. |
| Crew (on facilities), "on crew duty" | **WORKERS** | People running your buildings. Unstaffed buildings run on the AI at lower output. |
| Population | **PEOPLE** | Everyone living in the Hub. |
| AI-RUN 3 | **3 RUN BY AI** | Buildings without workers; the AI runs them. |
| OVR | **OVERRIDE** | Emergency charges that force the AI's hand. |
| Posture: HOLD / TURTLE / GO DARK / EVACUATE | **NORMAL / FORTIFY / HIDE / EVACUATE** | How the Hub meets the next attack. |
| Autonomy: MANUAL / ROUTINES / AUTOPILOT | **YOU DECIDE / AI ASSISTS / AI DECIDES** | How much the AI does without asking. |
| SET & GO | **USE AI PLAN** | Apply the AI's recommended defense. |
| Ironman | **HARDCORE** | One life: if the core falls, the run ends. |
| LP | **LEGACY POINTS** | Score carried from one Hub to the next. |
| Player op "RAID" | **STRIKE** | Your team attacks a site (enemy attacks stay RAID / SIEGE / PURGE / VIRUS). |
| PACT / PLANT / FRAME | **CEASEFIRE / SEND SPY / FRAME A RIVAL** | Diplomacy and espionage actions. |
| Climax "PURGE CORE" | **RESET THE CORE** | Wipe the AI's hidden project (not the enemy Purge). |
| EST 34 // DEF 12 | **~34 ATTACKERS // DEFENSE 12** | The AI's estimate versus your strength. |
| E / C / F after numbers | **ENERGY / COMPUTE / FUEL** where there is room | No unexplained letters. |

A **Field Guide** (Command menu) explains every term in one or two plain sentences, grouped by Resources, People, Defense, The AI, The World and Progress. Long-press hints cover the jargon on DEFENSE, CORE and the MAP.

## 2. No monetization (for now)
Everything is free. No store, no ads, no unlock gate, no season pass. The reward track stays: free cosmetics earned by play. The sim keeps dormant ad-convenience commands only so old saves replay.

## 3. No leftovers
Dead code removed (LockedScreen, ReportRender, unused helpers). Hard-coded names routed through `Names`. Sample text in layouts never reaches the player before the first refresh.

## 4. The opening
A short, cinematic boot: black, a heartbeat of static, the core's first lines, a scan of the ruined world, then the drone descends onto the Hub and the HUD assembles piece by piece. Skippable at every step, off-tone flashes avoided, reduced motion shows it as still cards.

## 5. The hook (fair, earned; engagement playbook)
- **Next milestone tracker:** a slim card shows the next tier's requirements with progress (the same gates as MODULES), so the next goal is always visible.
- **Celebration moments:** tier reached, module restored, attack repelled, chapter closed, building maxed: a short full-width banner with an icon, a line from the AI and what it unlocked. Never blocks input, never sells anything.
- No timers that punish absence, no streak loss, no fake urgency.

## Done
- s1: names applied across UXML, screens, advisor lines and hints; `Presentation/Glossary` + FIELD GUIDE screen (`UI/Screens/FieldGuideScreen`, Command menu); long-press hints on DEFENSE, CORE and LEGACY jargon; raw enum text replaced (report outcomes, module states).
- s2: store, ads, unlock gate and season pass removed; `Runtime/Cosmetics` (free reward track, interface colours in SETTINGS).
- s3: dead code removed; the warlord named once in `Names.Warlord`; SETTINGS START OVER (two taps, replays the opening); override pips follow the real cap; version in About.
- s4: six-beat prologue (`Host/Narrative/OpeningGuide` `Prologue.Scenes`), `UI/Hud/PrologueWorld` (cities light, burn and go dark), mood colours, tap to advance, TAKE CONTROL, orbit shot and drone descent (`OpeningFlow`). Chapter close is not repeated as a banner: the STORY screen already marks it.
- s5: `UI/Hud/GoalCard` (next tier's gates on BASE), `UI/Hud/Celebrations` (tier, memory, repelled attack, fully upgraded, mastery).
- Screenshots: `docs/media/li5-*.png`. Fixtures: `opening-war`, `opening-handler`, `milestone`.
