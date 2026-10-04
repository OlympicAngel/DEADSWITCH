## What and why

## Spec / ADR
Link: 

## Checklist
- [ ] `pwsh tools/check.ps1` passes
- [ ] Tests added or updated (determinism, caps, chunking where relevant)
- [ ] New `GameState` fields are in `GameState.Visit` (hash + save); save version bumped if layout changed
- [ ] Docs updated (spec, doc 10 corrections log if numbers changed)
- [ ] `docs/agents/HANDOFF.md` current-state snapshot still true (edited in place, if needed)
- [ ] No sold or watched reward grants safety, defense, timer skips or combat power
