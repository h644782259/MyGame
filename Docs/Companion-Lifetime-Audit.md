# Companion lifetime and collection audit

This source audit found two concrete issues. Every hero's teleport inserted a
static companion bond, but old owner keys were removed only by later summoner
combat calls. Repeated non-summoner save loads could therefore retain destroyed
controllers and their bond state. Separately, each active summoner frame built a
filtered companion list and array during capacity upkeep; after the starter retry
timer expired, starter checks also created a capturing predicate each frame.

The player now explicitly retires companion state in `OnDestroy`. Retirement uses
managed-reference null checks so Unity's destroyed-object equality cannot skip the
dictionary removal, and dismisses all that owner's companions without accessing
the destroyed owner's transform or combat epoch. Non-summoner transfers do not
create bonds. Runtime subsystem registration clears companion static registries.
Defensive stale-owner pruning reuses a cleared scratch list.

Capacity upkeep now walks the existing list from oldest to newest. Dismissal
removes the current entry immediately, so the cursor examines the entry shifted
into that position before advancing. This retains the same oldest bonded spirit,
starter wolf, eviction policy, health, lifetime conversion and command behavior.
Starter presence uses the same owner/alive/starter predicates in a direct loop.
Explicit snapshots remain available to infrequent command and test callers.

This removes those specific temporary collections and capturing predicates. It
does not claim zero allocation for a complete gameplay frame. This audit also
identified allocation in shared equipment queries (`ProgressionService.FindItem`
and enum validation); those service fixes are validated separately. No frame-time
or memory-usage measurements were performed.

## Validation

- `CompanionLifetimeSourceTests.py` checks owner retirement wiring, destroyed-owner
  handling, non-summoner isolation, registry reset, collection-free capacity and
  starter loops, and dispatch of the isolated engine fixture
- Existing pure companion and combat-pacing rules verify unchanged contract,
  capacity, health, protection and cadence semantics
- `SummonerValidation` contains prepared engine checks for repeated non-summoner
  teleports, real destroyed-owner cleanup with companion updates disabled,
  idempotent retirement and oldest-first route switching with adjacent removals
- Runtime and Editor assemblies can be compiled against Unity 6000.6.3f1 APIs
  separately. The prepared assertions still require an authorized Unity runtime
  environment; they are not evidence of executed PlayMode or device tests

## Other inspected resource paths

No additional concrete native resource leak was found in large-boss anchors and
beam ownership, fixed filled-effect mesh/material caches and reused property
blocks, equipment/fashion mesh replacement, world/loot/debris destruction, enemy
auras/dissolve/telegraphs, or projectile lists and per-projectile hit sets. World
transitions deactivate and destroy the previous hierarchy. Owner/epoch validation
retires stale effects, and projectile destruction releases both owned materials
and its static hostile-list entry. Equipment appearance palettes are bounded by
clamped tier/rarity/upgrade variants and destroyed with the model. These statements
describe inspected ownership paths, not measured native-memory or render behavior.

## Shared equipment read path

`ProgressionService.HasMechanic` now checks the closed mechanic catalog without boxing the enum for reflection. `FindItem` walks the current inventory directly instead of allocating a capturing `List.Find` predicate. It deliberately does not cache item references: profile replacement, removal and direct equipment changes must be visible immediately. Slot/class gates and invalid-mechanic rejection are unchanged.

`EquipmentLookupTests` executes95 identity/eligibility assertions with isolated fake storage. After warmup,16,384 pairs of managed mechanic/equipment reads allocate0bytes according to `.NET GC.GetAllocatedBytesForCurrentThread`. This narrow managed probe does not measure Unity's update loop, native allocations, device memory, frame rate or garbage-collection pauses. Other infrequent contract/spawn/query APIs still allocate snapshots as documented.

## Live build follow-on

Permanent Spirit rank, paused route/capacity changes, absolute-HP preservation and
active command scaling now reconcile at the successful build-change boundary.
See [live builds and world-loot ownership](Build-And-Loot-Transitions.md) for the
exact semantics, executable regressions and prepared-only Unity checks.
