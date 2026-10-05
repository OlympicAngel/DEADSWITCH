# 08 — Tech, Online Plan, Monetization, UX & Roadmap

## 1. Offline-first architecture

Offline-first, with the option to go fully online later.

### Selected foundations

- **Deterministic simulation** — seeded randomness so offline results can be replayed and verified by a server later.
- **Clock-cheat protection** — detect device time changes. In-story, this appears as the AI's "time desync" glitch.

### Recommended (not yet selected, see open questions)

- **Event-log architecture** — store every action and attack as an event. This fits battle reports, replays, and later sync.
- **Early cloud save / account link** — allows backup and device migration before online launches.

### Offline resolution model

1. On logout, the game records the player's defense setup, delegation level, and a timestamp.
2. On return, the simulation advances from the last tick to now, using seeded events (scheduled waves, ambushes, world events).
3. The result is turned into a **battle report** (graphic-novel panels of rendered stills), with the AI possibly editing it when corrupted.

## 2. Online plan (later)

- **Alliances & co-op** — players team up for shared defense and big joint operations.
- **Leaderboards & world events** — global rankings and server-wide collapse phases that everyone faces together.

Not selected: asynchronous PvP and a shared persistent world. "Other AI fragments" are simulated at first and could be other players' cores later.

## 3. Monetization

- **Premium** — one-time purchase, no timers to skip, no pay pressure.
- **Rewarded ads and season pass** — optional ads for small bonuses and a seasonal progression track.

> See open questions: premium plus ads needs a clear rule, such as a free trial with a premium unlock, with ads that never give safety advantages.

Rules to protect the design:

- Nothing sold can guarantee safety or skip timers.
- Season rewards should be cosmetic or convenience, not power.

## 4. UI and UX

- **Command bar** — bottom navigation: Base, Map, AI Terminal, Operations.
- **Living base view** — animated diorama you tap and pan, with no list-style menus.
- **Gesture-driven** — swipe between command layers, pan and pinch to zoom the full-screen sector map, tap a location for its contextual information/actions, and long-press a map pin for quick actions.
- **One-handed play** — key actions reachable by thumb, minimal HUD, immersive full-screen.

Always-visible essentials: next timers, energy balance, corruption, and the highest faction heat.

## 5. Notifications

- **Attack alerts** — push notifications with an incoming-attack countdown.
- **In-character messages** — written in the AI's voice, glitchier with corruption.
- **Lock-screen widget** — live threat level, heat, and next timer.
- **Fully opt-in and customizable** — players choose which alerts they get and when.

## 6. Comfort and accessibility

- **Effect intensity settings** — reduce flashes, glitch effects, screen shake, and haptics.
- **Vacation shield** — limited, rarely usable protection for real-life emergencies, travel, or illness.

(Colorblind-safe HUD and assist options were not selected. Recommended to revisit before launch.)

## 7. Suggested roadmap

| Phase | Goal | Contents |
|-------|------|----------|
| **0. Paper prototype** | Prove the pressure loop | Spreadsheet sim of energy, compute, people, corruption, one attack type |
| **1. Core prototype** | Prove the AI relationship | Terminal UI, advisor lines, corruption, one module field, tier 1 base |
| **2. Offline pressure** | Prove offline attacks | Defense setup, logout/return report, raids and sieges, mercy window |
| **3. Vertical slice** | Prove the feel | Opening sequence, art and audio pass, battle reports, hidden project clue |
| **4. v1 content** | Complete first release | Tiers 1–2, four fields, three factions, viruses and purges, notifications |
| **5. Expansion** | Depth | Tiers 3–4, conquer-and-hold, diplomacy suite, relocation cycle, Ironman |
| **6. Online** | Social layer | Alliances, leaderboards, world events, deterministic server verification |

## 8. Risks

- **Scope** — many interlocking systems. Mitigation: build in pillar-rank order and keep the "touches two others" rule.
- **Offline fairness** — losses while away can feel unfair. Mitigation: mercy window, readable reports, clear defense tools.
- **AI lying** — can feel like a bug. Mitigation: clues, audit tools, and consistent rules for when it lies.
- **Premium plus ads** — mixed signals. Mitigation: define the model early.
