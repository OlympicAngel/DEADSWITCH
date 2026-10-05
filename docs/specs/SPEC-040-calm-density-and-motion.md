# SPEC-040: Calm density and motion (living interface II)

- Status: In progress (F-101)
- Owner direction (2026-10-05, after SPEC-039): "more animation, the UI still feels dense, break it down even more; free hand."

## Player value
A screen shows one topic at a time; detail is one deliberate tap away. Motion tells the player what just appeared, what changed and where they went, so the interface feels alive without being busy.

## Rules
1. **One topic per view.** Dense screens split into pages with a tab strip (`ds-pager`); secondary cards collapse to their header (`ds-card--fold`) and remember their state.
2. **HUD rests quiet.** The top-right stack becomes a status rail of compact pills (threat, build, dispatch, report, override); tapping a pill opens its card, one at a time, and it closes on its own. An imminent threat opens its card by itself. The AI comms panel collapses to a slim bar after a line has been read and opens for a new line.
3. **Motion explains:** screens slide in the direction of travel; cards rise in a short stagger; meters sweep; taps ripple; the active tab's indicator slides; primary actions shimmer now and then; changed values punch. Every animation has a readable static end state, never delays input, and reduced motion removes it.
4. **The base moves:** new construction rises out of the ground with dust; a finished build punches; hits flash.
5. Presentation only. Tunables in `Resources/UI/Interface.json` or the motion constants named in code.

## Ideas (status)
61. Staggered card entrance on every screen and page. *Done*
62. Directional screen slides between command-bar layers. *Done*
63. Press ripples on buttons, tiles, chips and tabs. *Done*
64. Meter sweep on show. *Done*
65. Primary-action shimmer. *Done*
66. Springy bottom sheets. *Done*
67. Sliding tab indicator and tab icon punch. *Planned*
68. Status rail with expandable pills. *Planned*
69. Comms panel auto-compact. *Planned*
70. Page tabs (`ds-pager`) for OPS, CORE, LEGACY, MAP. *Planned*
71. Foldable cards with remembered state. *Planned*
72. Value punch when a pod rises; flash red when it drops sharply. *Planned*
73. Construction rises from the ground with dust; build-complete punch. *Planned*
74. Hit flash on facilities damaged in a raid. *Planned*
