# TASK: F-107 Interactive sector map

- Status: Done 2026-10-06
- Branch: feat/f107-map
- Spec: `docs/specs/SPEC-033-sector-map.md` (rule 4)

## Steps
- [x] 1. Camera view over the map: focus + zoom (`SectorScene.View`, `MapView.SetView`), tunables in `BaseLook.json` `map`
- [x] 2. Full-screen plot; pager becomes a bottom sheet, folded to its tab row by default
- [x] 3. Drag pan, pinch and wheel zoom; drag slop keeps pin taps; off-view pins hide
- [x] 4. Pin tap opens the SITE sheet and pans a covered site into view (`panSlack` lets edge sites clear the sheet)
- [x] 5. Editor check at 380x800: opening view, pan, sheet open/fold, edge site reveal, zoom limits (zoomMax 0.62 keeps the ground clear of haze and tags apart)

Previous: F-106 on `feat/f106-ui-redesign`, F-110 on `feat/f110-fog` (both awaiting merge).
