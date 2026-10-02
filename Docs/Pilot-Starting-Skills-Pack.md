# Starting skills and autonomous reinforcement wolves

Source pilot, based on Windows main `03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e`. No platform settings or save schema changes.

## Starting skill and shared point budget

All four classes start with their existing first active at rank one. Lifetime skill/mastery budget is `max(1, level - 1)`: one invested point at level one, no additional point at level two, one additional point at level three. Starting rank-two/rank-three gates remain level 10/20. Ordinary enemy XP, direct XP, mode rewards, dungeon completion and chapter completion use the same per-level budget delta; notification text reports that actual delta.

Normalization migrates only a level-one unlearned first skill. Existing level-two-or-higher legal allocations remain unchanged. Preset validation and rank/mastery refunds share the same lifetime budget. Repeated saves, loads, resets and receipt replay cannot mint additional points. A legacy level-two character with its one point unspent stays unspent.

## Reinforcement target policy

Only nonpermanent, nonstarter wolves in the Pack route use the new autonomous selection. Existing paid-command, recall, free focus and owner focus priorities still precede autonomy. Legal candidates belong to the current encounter, are within the existing 14-unit owner radius, have a navigable route at the wolf’s movement radius, and are already engaged when in the wilderness.

A clear direct segment is only a fast success; an occluded segment falls back to the real pathfinder. Per-companion reachability probes are capped at 32 entries and 250ms, invalidating on terrain revision or endpoint movement. A legal autonomous choice is held for one second; death, removal, range loss or loss of a ground path breaks the hold immediately. On selection, prefer the nearest legal enemy not currently occupied by another living allied wolf, including the permanent base wolf and its paid target. If every legal enemy is claimed, converge on the nearest legal candidate. Spirit, treant, permanent base wolf and Bonded-route behavior remain on their existing paths.

The recall button reads `出击` while the free recall directive is active. Free attack only clears that directive and visual targeting state. It does not cast a contract, damage, heal, refresh attack cooldowns, lifetime, command duration/multiplier or dodge-command opportunities. A temporary paid command still resolves before autonomous selection and returns to the latest team intent on expiry or target loss.

## Verification boundaries

`StartingSkillBudgetTests.py` compiles actual production progression services: 98 assertions plus three compiled negative controls for the old zero-point level-one budget, shifted 9/19 upgrade gates and missing migration.

`CompanionIntentProductionTests.py` extracts and compiles actual production command/selection methods: 1,053 assertions plus five compiled invalid-behavior controls, including nearest-only targeting, removed hold and cooldown-reset free attack. Unity object/physics/effect services are controlled shells; path legality is injected, so these are production-policy replays, not real navigation or gameplay footage.

Existing progression/reward/preset/save tests are updated where their fixtures assumed every new character had no learned skills. Explicit legacy fixtures remain unlearned at level two or above. Cached Unity 2021 API compilation checks source compatibility only. Unity 6000.6 and device acceptance, navigation under moving bodies, command button presentation and combat experience remain pending.

`PackDetourProductionTests.py` compiles the actual forest geometry, traversal implementation and pack admission/selection: 740 assertions and a compiled old-direct-segment negative control. It walks the actual Route/Move path around the central island into attack-contact range, checks immediate terrain-revision invalidation and bounded cache size. This is managed navigation replay, not a Unity gameplay test. `LevelUpPointFeedbackTests.py` exercises the actual level callback with 13 assertions and a compiled hardcoded +1 SP control, including sequential level-two-through-five notifications.
