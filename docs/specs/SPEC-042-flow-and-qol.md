# SPEC-042: Flow and QOL (information architecture pass)

- Status: Done (code) 2026-10-05 (F-103); Editor check pending
- Owner direction (2026-10-05): "the UI is still off, break it down even more, think of QOL; is this UI good, intuitive, cramped, dense, related to the rest of the page, should it move somewhere else? This is a big one."

## Audit (what a new player hits)
Each finding names the problem, why it hurts, and the decision.

### Navigation and placement
1. **Hidden destinations.** WORKFORCE is only reachable through a shortcut inside the PEOPLE resource sheet; SETTINGS, STORY, LEGACY, SEASON and FULL GAME hide inside CORE. Nobody looks for settings or their people inside the AI terminal. **Move:** a Command menu (drawer from a menu button at the left of the context strip) lists every secondary destination with an icon, one line of meaning, and a badge.
2. **Two tab levels in CORE** (STATUS/MODULES, then PRESENCE/ACTIONS/PROFILE/RECORDS) feel like a maze. **Flatten** to one row: PRESENCE, MODULES, ACTIONS, PROFILE. RECORDS and SETTINGS leave CORE for the Command menu.
3. **"OPS" says nothing to a new player.** The tab defends the Hub. **Rename the label** to DEFENSE (route id stays `ops`; doc 08 calls it Operations, the label is player-facing).
4. **No back gesture.** Android back / Escape does nothing. **Add:** back closes the top layer (hint card, sheet, menu, secondary screen) and returns to BASE from a top-level tab.

### Threat flow
5. **Five taps to answer a raid** (pill, card, DEFEND, DEFEND page, SET & GO). The AI already computes a plan. **Shortcut:** the raid card shows "AI PLAN: TURTLE // 5 DEFENDERS" with one APPLY button beside OPEN DEFENSE; applying shows a toast and the pill reads PLAN SET.
6. **The raid card hides what is already decided.** Show the current posture and garrison on the card so the player knows whether to act at all.

### Building
7. **The empty-plot sheet is a 12-row list** of near-identical rows; a player scrolls to compare. **Rebuild** as a categorized grid (POWER, COMPUTE, PEOPLE, DEFENSE, SUPPORT tabs) of build cards: icon, name, the one number that matters, cost, and either the build time or "IN 2H" when it cannot be afforded yet. The recommended facility (from a resource shortcut) opens its category and is marked.
8. **Unaffordable actions are dead ends** (a shake). **Say when:** the upgrade tile and build cards show the wait until affordable at the current rate ("IN 2H 10M"), or "NEED STORAGE" when the cost exceeds capacity.
9. **The build pill only knows the soonest job.** **Queue sheet:** tapping the pill lists every job with its progress and time, each row flies to its plot; an idle queue offers the best next build.

### Feedback and orientation
10. **Every screen's first glance.** Each top-level screen opens to the page that matters now (DEFENSE: THREAT only when there is one, else DEFEND; CORE: ACTIONS when a crisis is live). Already partly in; keep it consistent.
11. **Badges mean "act here".** The menu button carries the sum of its children's badges (dispatch waiting, report unread, perks affordable, workforce restless).

## Rules
- One tab level per screen. Secondary destinations live in the Command menu, not inside other screens.
- Every disabled action says why and when.
- Every threat can be answered from where it is announced.
- Back always goes somewhere predictable.
- Presentation only; commands are the existing sim commands.

## Done
- Findings 1-4: Command menu (`UI/Hud/CommandMenu`) with badges, CORE in one tab row, DEFENSE label, back stack (`UI/Back`, Android back / Escape via NavigationCancelEvent); detail screens close back to where they were opened (`ScreenRouter.Return`).
- Findings 5-6: the raid card shows the current setup and the AI plan with one-tap APPLY.
- Finding 7: build picker grid by category (`SlotSheet.Empty`, fixture `build-picker.uxml`).
- Finding 8: `Presentation/Afford` ("IN 2H 10M", "NEED STORAGE") on build cards and the upgrade tile.
- Finding 9: build queue sheet (`UI/Hud/QueueSheet`) from the job pill.
- Finding 10: DEFENSE opens on DEFEND when there is nothing to answer; WORKFORCE splits PEOPLE and HARD CHOICES.
- Screenshots: `docs/media/li4-*.png`.
