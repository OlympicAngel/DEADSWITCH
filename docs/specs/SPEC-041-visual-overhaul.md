# SPEC-041: Visual overhaul III (critical remake)

- Status: In progress (F-102)
- Owner direction (2026-10-05): "continue with overall visual improvement, don't be afraid of completely remaking stuff, be critical."

## Critique that drove it (composed HUD over the 3D base)
1. Too much floats over the world: feed block, frame brackets, raid box, job chip and override row took the best part of the view.
2. Type too small and too uniform: everything uppercase, wide tracking, 16-20 px; rates unreadable on a phone; titles, labels and values barely differ.
3. Border soup: outlines inside outlines read busy and flat at once.
4. Buttons muddy: brown fills with amber outlines; no obvious primary.
5. No separation between UI and world: flat fills, hard edges, no light.
6. A heavy bottom: comms panel plus a boxy tab bar took ~18% of the screen.

## Rules
1. **The world is the hero.** HUD status hugs the top edge over a scrim; actions live in one floating dock; nothing else floats except the status rail.
2. **Type ramp v2:** floor `--fs-nano` 19 px, body one step up; tracking tokens (`--ls-tight/label/wide`) replace 4-6 px letter-spacing; descriptions use the mixed-case ChakraPetch Regular face; values in mono at hero sizes.
3. **Depth by light, not lines:** surfaces get a top sheen (UI/Sheen), cards lose their outlines (a 1 px light top edge only), tone shows by a 6 px left accent bar.
4. **Buttons v2:** tonal surface by default; one solid cyan primary per view; warn = amber accent bar; ghost for tertiary only.
5. Everything stays token-driven and mirrored in tools/uipreview.
