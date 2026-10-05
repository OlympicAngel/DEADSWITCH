# SPEC-046: Interface redesign (information architecture, legibility, decision screens)

- Status: In progress (F-106)
- Pillar: AI relationship (the interface is the AI's voice), Base & economy, Defense & offline, Offense & diplomacy (every screen)
- Touches: HUD, every screen under `UI/Screens`, base quick actions, map, guide/objectives, comms, settings (text scale)
- Source rules: doc 10 (major UI redesign, mobile legibility, HUD essentials: next timers, energy balance, corruption, highest heat; colorblind shape+color; scalable text), doc 11 (Screens, Type), quality bar (mobile legibility, 44 px targets), ADVISOR_VOICE

## Goal
A first-time player always knows three things without navigating: what state the Hub is in, what needs them now, and where to tap to act. Every screen has one job and one primary action; detail is one tap away, never stacked on top. Text reads at arm's length on a phone.

## Audit (Editor, 720x1280, 2026-10-06)
1. **Chrome eats the screen.** Resource pods + status row + objective card + comms strip + tab bar take ~40% of the height on every screen. Screen content gets the middle 50%.
2. **Same message twice.** The objective card and the comms strip often say the same thing (raid, defense) at once; CORE then repeats the comms log a third time.
3. **Overlays cover decisions.** The objective card sits over screen content (MAP: the order buttons are cut off under it).
4. **Panels over a live 3D scene.** Screens are translucent over the base; text contrast drops and the scene's lights read as noise (OPS, WORKFORCE).
5. **Three navigation layers per screen.** Bottom tab, screen header (title + subtitle), then an inner tab row; WORKFORCE adds a close button and is reached only from the Command menu.
6. **Type far too small.** The UI scales from a 1080 px reference (3 px per dp). `--fs-nano` 19 px = 6.3 dp, `micro` 7.3 dp, `xs` 8.7 dp, `sm` 10 dp. 140+ uses of nano/micro (HUD pod sub-lines, tab labels, chips like AI ESTIMATE, map pin values).
7. **Map labels collide** (names and `~N` values overlap at the default zoom; `~N` is unexplained). Owned by F-107 (map interaction); this spec covers only its type and popover kit.
8. **CORE's first page is decoration.** A large orb and three tiles; the real decisions (modules, actions) are behind inner tabs.

## Reference patterns (adapted, not copied)
- **Clash of Clans / Boom Beach:** the world is the home screen; tapping a building opens a small context bar of 2-4 verbs at the building; full-screen panels only for big decisions. Resources top, never more than one line.
- **Frostpunk (mobile) / This War of Mine:** one persistent pressure readout (temperature/hope; here: next threat, energy balance, corruption); alerts appear as a short queue of actionable cards at the screen edge that each open the relevant place.
- **Rise of Kingdoms / State of Survival:** a single "what next" goal chip that jumps to the action; badge counts on tabs instead of banners.
- **XCOM 2 / Into the Breach:** decision screens are opaque, full-screen and state the consequence next to the button (cost, time, risk), one primary button.
- **Fallout Shelter:** status as glanceable pictograms + numbers; text only on demand.

Principles extracted: (a) the world is the hub, panels are short-lived; (b) one persistent status line, one goal chip, one alert queue; (c) decisions on opaque sheets with consequence beside the action; (d) navigation depth ≤ 2 (tab, then one sheet); (e) badges, not banners; (f) legibility floor before density.

## Rules
1. **Type floor.** No essential text below 11 dp (33 px at the 1080 reference); body 13-14 dp; numbers that drive decisions ≥ 15 dp. Implemented in `Tokens.uss` (ramp at text scale 100/115/130), not per-screen overrides. `nano` and `micro` merge into one 11 dp step.
2. **Touch targets** ≥ 44 dp (132 px) high for every tappable row, tab and chip.
3. **Persistent chrome ≤ 22%** of a 9:16 screen height on BASE and ≤ 18% on other screens: one status row (resource pods with value + rate only, threat timer, corruption), and the tab bar. Sub-lines (FULL 4H, WORKERS 3/3) move into the pod's tap sheet.
4. **One voice at a time.** Objective and comms merge into a single bottom "next" strip: the current objective or the newest unread AI line, never both. Tapping it opens the relevant place. Older lines live in CORE.
5. **No overlay over a decision screen.** The next strip collapses to a chip while a screen other than BASE is open; it never covers screen content.
6. **Opaque decision surfaces.** Screens and sheets use an opaque surface token; the 3D scene may show only behind BASE and the MAP render.
7. **Navigation depth ≤ 2.** Bottom tabs (BASE, MAP, CORE, DEFENSE) + Command menu; inside a screen at most one tab row; no separate subtitle line under the title (title only, short).
8. **Consequence beside the action.** Every primary button shows its cost/time/risk on or directly above it; unavailable actions say why.
9. **CORE opens on decisions.** First page: what the AI wants from you now (pending module, override charges, actions), then status. The orb shrinks to the header.
10. **Keep identity.** Military-terminal look, established names and colors, shape + color for state, reduced motion and text scale honored.

## Non-goals
Map pan/zoom and pin popovers (F-107). Facility visuals and economy (F-111). New mechanics or balance changes.

## Steps
1. Type and target floor in tokens (rule 1-2); fix overflow on every screen at 100% and 130%.
2. HUD v4: one status row, pod sheets take the sub-lines, merged next strip (rules 3-5).
3. Opaque surfaces and single-level screen headers across screens (rules 6-7).
4. CORE decision-first page (rule 9); consequence lines on primary actions (rule 8).
5. Per-screen pass: OPS/DEFENSE, WORKFORCE, STORY, SEASON, SETTINGS, GUIDE, LEGACY, DISPATCH, REPORT, BATTLE.
6. First-time task check at 720x1280 and 1080x2340: build a battery bank, set a defense before a raid, answer a module request, send an op from the map. Each reachable in ≤ 2 taps from BASE with no overlapping text.

## Acceptance criteria
- [ ] No text style below 33 px at the 1080 reference (grep `--fs-*` and literal `font-size` in USS).
- [ ] Chrome height measured in the Editor at 720x1280 meets rule 3.
- [ ] Objective and comms never shown at once; nothing overlaps screen content.
- [ ] All screens captured at 100% and 130% text with no clipping.
- [ ] The four first-time tasks pass the step 6 check.

## Tests
None in the sim (presentation only). `tools/uss_lint.py` gains a floor check for font sizes below the token floor.

## Open questions
- Owner: is merging objective and comms into one strip acceptable (rule 4), or must the AI line always be visible?
