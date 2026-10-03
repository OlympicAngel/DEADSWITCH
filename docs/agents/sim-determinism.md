# Playbook: Simulation determinism

The sim must produce **bit-identical** results for the same seed + inputs on every device. This powers offline catch-up, replayable battle reports, and later server verification (see ADR-0002, ADR-0003).

## Allowed
- `int`, `long`, `uint`, `ulong`, `bool`, enums, readonly structs.
- Fixed-point as scaled integers (e.g. thousandths). Name the scale in the identifier (`RateMilli`).
- `Pcg32` for all randomness. Exactly one draw per decision point, always, even when the result is discarded, so caps and branches never shift the stream.
- Integer division with explicit rounding (document floor vs ceil).

## Forbidden in `Deadswitch.Sim`
| Thing | Why | Instead |
|-------|-----|---------|
| `float`, `double`, `decimal`, `Math.*` on floats | Platform and compiler differences | scaled integers |
| `System.Random`, `Guid.NewGuid()` | Not seeded / not stable | `Pcg32` |
| `DateTime.*`, `Stopwatch`, `Environment.TickCount` | Wall-clock | `State.Tick` |
| `Dictionary`/`HashSet` iteration that affects state | Order not guaranteed | `List` / sorted keys |
| `UnityEngine.*`, file/network I/O, `static` mutable state | Not engine-agnostic / hidden state | pass in via arguments |
| LINQ `OrderBy` without a total order key | Unstable ties | add a unique tiebreaker (id) |
| Threads / async in the tick | Non-deterministic order | single-threaded tick |

## Checklist for any sim change
- [ ] New state field added to `GameState` **and** `StateHasher`.
- [ ] New randomness uses `state.Rng` and always draws the same number of times per tick.
- [ ] Test: same seed gives same hash; different seed diverges.
- [ ] Test: chunked run equals single run.
- [ ] Test: any cap or limit rule (loot cap, offline attack cap, resource caps).
- [ ] Offline attack cap and mercy window rules from doc 10 section 4 still hold.

## Offline catch-up
Long absences are stepped tick by tick up to a cap, then **coarse-grained** (hourly) beyond it. The coarse path is a separate function with its own determinism tests and must never grant free progress (clock-cheat rule, ADR-0004).
