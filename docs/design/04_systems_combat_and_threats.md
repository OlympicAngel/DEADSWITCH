# 04 — Systems: Combat & Threats

## 1. Who attacks you

1. **Rival factions & warlords** — human-led armies.
2. **Rogue war machines** — drones and robots running old war AI code.
3. **Other AI fragments** — rival cores, simulated at first, possibly other players online.
4. **Environmental threats** — fallout waves, plagues, collapse events, many caused by the player's own actions.

## 2. How attacks reach you

| Mode | Description |
|------|-------------|
| **While you're away** | You return to a battle report with real losses |
| **Live warnings** | An incoming attack countdown, with time to prepare or flee |
| **Scheduled big waves** | Major assaults and collapse events on a rising cadence |
| **Unpredictable ambushes** | The AI gives partial intel, which may be wrong or hidden |

### Offline rule

- Attacks, including **timed attacks**, resolve while the player is offline.
- Logging out **without protection or defenses set** is an activity penalty.
- Offline results are produced by a **deterministic simulation** (see doc 08).
- Clock tampering is detected and shown as an in-story "time desync" glitch.

## 3. Attack signatures

Each attack type has a signature loss profile so players learn to fear specific threats. Some combine.

| Signature | Frequency | Primary loss | Notes |
|-----------|-----------|--------------|-------|
| **Raids** | Fast and frequent | Loot (energy, compute) | Light damage |
| **Sieges / bombardment** | Moderate | Building damage and downgrades | Outposts can fall |
| **Viruses / hacks** | Silent, sometimes unnoticed | AI corruption, locked modules, false intel | Hard to see in the moment |
| **Purges** | Rare, brutal | Combined: casualties, loot, damage | Can force a reboot if the player ignored warnings |

Alerts should read differently by signature so the player can tell, at a glance, which one is coming.

## 4. What the player can lose

- **Looted resources** — energy and compute stolen, with a cap so you're never fully wiped.
- **Damaged or destroyed buildings** — outposts can be lost; hub buildings can be downgraded.
- **Casualties** — people and units lost permanently.
- **AI corruption** — modules glitch, lock, or regress until repaired.

**Rules:** never all at once; each loss depends on the attack type; some are combined.

## 5. Protection tools (before logout)

1. **Defense setup** — assign garrisons, traps, and turrets. The AI simulates the result.
2. **Go dark** — stealth mode hides the base but drains energy.
3. **Strategic retreat** — pull the core and people out, sacrificing outposts to save the hub.

Delegation interplay: **offline autopilot** can make these decisions for you but may get them wrong.

### Recovery and fairness

- **Mercy window** after devastating loss.
- **Vacation shield** (limited, rarely usable).
- **Battle scars** stay visible until repaired, so a defeat is seen and felt.

## 6. Units

| Family | Examples | Crewing |
|--------|----------|---------|
| **Human troops** | Militia, soldiers, scouts | They are people |
| **Drones & robots** | Captured or reprogrammed rogue machines | Need operators, or AI control (glitchy) |
| **Heavy vehicles & mechs** | Tanks, artillery, walkers | Need operators, or AI control (glitchy) |

Named heroes were **not** selected as a unit class. Named operators still exist as a legacy feature (see doc 06).

## 7. Combat resolution

Three layers:

1. **Auto-resolve with prep** — set forces and tactics; a cinematic battle report plays. Used for routine fights.
2. **Short live battle** — a brief animated fight you influence with a few abilities. Used for important fights.
3. **AI-simulated reports** — the AI runs the math and may misreport or distort results when corrupted.

OVERRIDE can be used inside live battles (manual takeover, machine seizure).

## 8. Player offense

- **Raid** enemy outposts and convoys for loot.
- **Cyber warfare** — use compute to hack enemies, drones, and rival AI cores.
- **Sabotage & stealth ops** — sneaky missions with small elite teams.
- **Conquer and hold** — capture zones and ruined cities for permanent bonuses.

Aggression raises **per-faction heat**, which increases attack frequency and strength from that faction (see doc 05).

## 9. Luck swings and difficulty feel

Short periods tied to events make the player feel strong, then challenged.

- **Named phases** — events like Signal Storm or Dead Week announce the swing and its effects.
- **Hidden streaks** — the player feels the luck but never sees the numbers.
- **AI half-warnings** — hints that something is coming, accurate only some of the time.
- **Opportunity windows** — enemy weak periods that reward players who prepared and read the signs.

Also:

- **Hidden adaptive difficulty** — threats quietly scale to the player's strength.
- **Adaptive enemies** — rival AIs and machines learn your tactics and counter them.

## 10. Open tasks

- Damage model and unit stats.
- Attack strength formulas by tier.
- Alert and battle-report presentation per signature.
- How defensive setups are expressed in a simple UI.
