# SPEC-020: Short live battles

- Status: Done (numbers are placeholders, tune at F-099)
- Pillar: Defense & offline
- Touches: raids and threats (resolution), OVERRIDE, corruption, OPS, 3D base, audio
- Source rules: doc 04 s7 (combat resolution layers), doc 01 v1 scope, doc 10 assists (slow motion)

## Goal
Important fights are not just a report: with the handler present, the attack reaches the wall and the handler
commands it for a minute, spending a few abilities, then sees it resolve.

## Rules (all numbers in `[battle]`, (tune))
1. An incoming siege, purge strike or Warlord wave is commanded live by default; a raid when the handler taps TAKE COMMAND (`TakeCommand(on)`, before contact).
2. At contact with the handler present and in command, resolution waits `battle_minutes` (BattleStarted). Away = auto-resolve as before, and leaving mid-fight resolves it at once.
3. Abilities, each once per fight (`UseBattleAbility`): FOCUS FIRE (defense +`focus_defense_pct`%, `focus_compute` compute), MORTAR BARRAGE (strength -`barrage_strength_pct`%, `barrage_energy` energy), MANUAL TAKEOVER (OVERRIDE charge: defense +`takeover_defense_pct`%), MACHINE SEIZURE (OVERRIDE charge: strength -`seize_strength_pct`%, +`seize_corruption` corruption). Strength cuts cap at 90%.
4. The modifiers apply at resolution; everything else (losses, scars, report) is unchanged. No RNG draws. Save layout v13, event schema v13.
5. Presentation: the fight plays at the gate the attackers really used (raiders advancing, tracers, muzzle flashes, shells, seized machines going dark); slow motion is a visual-only assist.

## Tests
`ThreatTests.Siege_BreaksABuilding_AndStartsMercy` (a siege with the handler present is commanded live before it resolves).
