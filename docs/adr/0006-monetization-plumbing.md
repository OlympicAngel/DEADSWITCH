# ADR-0006: Monetization plumbing: free demo + premium unlock

- Status: Accepted
- Date: 2026-10-03

## Context
Doc 10 section 1.1: Tier 1 free with optional rewarded ads (convenience only); one-time premium unlock for Tier 2+, Ironman and future content; season pass is cosmetic/convenience only. Nothing sold or watched may grant safety, defense, timer skips or combat power.

## Decision
- Unity layer exposes an `IEntitlements` interface (`HasPremium`, `OwnsSeason(id)`). The sim never sees purchases.
- Ad rewards are granted through a whitelist of **convenience grants** (extra salvage roll, small idle-cap extension, cosmetic). Anything outside the whitelist is rejected in code.
- Ads are suppressed while an attack countdown, battle or crisis is active.
- Purchases are restored from the store; no custom server needed at v1.

## Consequences
- Good: design rules are enforced structurally, not by policy.
- Bad: whitelist must be reviewed whenever a new reward type is proposed.
