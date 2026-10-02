# Regression coverage and measured allocation maintenance

This round preserves gameplay behavior, state, seed selection, rewards and saves. It starts from the free-order expedition seals candidate and addresses coverage drift plus measured managed allocations. Runtime changes are limited to reuse within a single HUD draw call and invariant navigation offsets; no cross-frame snapshot cache or pathfinding pool is introduced.

## Existing contract drift

Five unregistered source scripts failed at the earlier clean c5a734e baseline. Read-only inspection found obsolete source shapes rather than confirmed lost production behavior. The original contracts must remain represented when their entry points are repaired:

| Existing script | Drift | Maintained behavior coverage |
|---|---|---|
| CombatOpportunitySourceTests | A target-refusal gate interrupts the old exact string | Actual draw/readiness and refusal priority, with the retained source contracts invoked by the already registered opportunity production check |
| CombatReviewSourceTests | Companion callback gained a block and telemetry | Actual friendly projectile contact branch: collision, target eligibility, LOS, impact order, companion callback and pierce continuation, plus retained source contracts |
| EnvironmentPresentationSourceTests | Tree recipe and colors moved to authored scenery | Material categories and actual application; explicit guards remain for world branches not executed by the fixture |
| TownBuildingGroupSourceTests | Roof helper replaced one direct Primitive call | Actual building parent/group relationships instead of a fixed source call count |
| HubSettlementSourceTests | Helper and group registration replaced inline flags | Actual town construction, upper-part registration, fade grouping and cleanup rather than an inline flag count |

The maintained first-room choice test uses real RunChoices/Rooms, pause, confirmation and transition logic. It adds coverage missing from the simpler room fixture's ChoiceDouble. The separately validated Redrock replay script joins the aggregate entry point; it repeats some positive geometry assertions but uniquely exercises actual room-zero spawn isolation and compiled wall/route-bit regressions. Registration counts are checks, not counts of unique assertions.

## Allocation measurement scope

Measurements use warmed .NET 8 production algorithms and recorded/no-allocation scene or drawing boundaries. They establish managed allocation changes under specified inputs; they do not establish Unity frame time, GC pauses, native allocation, GPU cost or device performance. Before/after comparisons must preserve selected targets, returned directions and visible UI values/rectangles. Absolute .NET byte counts are not portable Unity budgets.

The HUD baseline observed redundant snapshot construction within a single draw event. Reuse is local to that event; later events query current state again. The navigation baseline observed two invariant eight-integer offset arrays allocated per A* search. Reusing these private read-only arrays retains neighbor ordering. The large search buffers and heap remain, so the navigation improvement is deliberately modest.

The HUD probe warms 3,000 calls then records five samples of 10,000 calls. All five samples were equal for each case in this environment:

| Managed bytes per draw | Before | After |
|---|---:|---:|
| Expedition mobile | 1,560 | 784 |
| Expedition desktop | 1,656 | 1,272 |
| Chapter mobile | 792 | 392 |
| Chapter desktop | 1,280 | 880 |

The actual constructor remains 200 bytes in the probe. Rendering boundaries and host getters are explicit managed doubles; the values isolate snapshot/draw work, not the entire live HUD. `HudAllocationProbe.py` is a measurement tool, while the registered actual Draw regression enforces per-event query counts and next-event state freshness.

For navigation, the controlled 240-query-tick blocked cases saved 4,480 bytes for one wolf and 18,480 bytes for four wolves, about 0.257%; open cases stayed at zero. Target indexes and direction float-bit traces matched the compiled legacy variant exactly in all four cases. The maintained allocation check enforces the bounded improvement and unchanged outputs. See `Tests/CompanionPathAllocationNotes.md` for inputs and remaining allocations.

Exact source identities and validation results are recorded in the accompanying review evidence. No Unity Editor, real font rendering, touch/controller hardware, device gameplay or platform package build is claimed.
