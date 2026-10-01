# Deterministic combo and resource budget

Generated from the production rule snapshot listed below. The CSV contains 48 scenarios: four classes, levels 20/50/100, two valid recipes per class, and 5-second or 10-second windows. Tests passed 1,602 checks. No Unity player, rendering, physics or measured time-to-kill experiment was run.

## What the numbers mean

`A` is one unit of the player’s current noncritical attack. These are modeled event ledgers for the explicitly listed recipes under ideal hit assumptions, not the full game’s DPS or a claim that a boss dies in a particular time. Level 50 and 100 have identical coefficients where their learned ranks are identical; equipment and level-dependent attack still change actual damage. Gear/stat scenarios are deliberately omitted rather than replacing the real stat service with a guessed attack number.

- One stationary, always-visible, indefinitely surviving target; no incoming attacks, movement, interruption, armor or other target mitigation.
- Every scheduled area impact lands. Fan arrows, nova shards and ultimate radial arrows use an ideal full-overlap target; the real per-target projectile diminishing/cap helper is applied to fan/shards. This is an overlap upper bound, not a promise that every arrow hits a normal-sized creature.
- Projectile flight is treated as negligible. No travel/muzzle/terrain misses are simulated. Ground-effect startup, duration, inclusive ticks, finishers and skill charge time are retained.
- Fixed 0.01-second clock. A priority input opportunity occurs every 0.20 seconds when not charging; this is a declared input recipe, not an invented production global cooldown. Instant skills can coexist with a basic attack. Charge release suppresses basics and new skill input in that step.
- Input and hits at the exact 5/10-second endpoint are included. Damage still scheduled after the endpoint does not enter the damage total. A charge merely begun by the deadline is shown as pending and costs no energy yet.
- All skills begin ready with 100 energy. Real SkillRuntime controls costs, cooldowns, passive regeneration and its 100-energy cap. Only confirmed basic hits restore the production 8 energy; actual restoration after cap clipping is separately counted.
- Legal ranks are derived from production level requirements, prerequisites and level-minus-one point budget. Every available first rank is learned first, then recipe priority receives legal upgrades, then remaining skills. No mastery points, selected cores, run blessings, gear mechanisms, critical hits, fashion proc effects, perfect dodges, incoming-damage passives or healing actions are assumed.

## Recipes and intrinsic effects

| Class | Starter/command priority | Advanced priority | Included intrinsic mechanics |
|---|---|---|---|
| Vanguard | 0 → 1 → 2 | 9 → 2 → 0 → 1 | Main strikes, rank echoes/finishers, blade-field ticks, ultimate tail |
| Arcanist | 0 → 1 → 2 | 9 → 2 → 0 → 1 | Balanced specialization; nova frost, every-third-basic frost, once-per-cast meteor shatter; meteor/field/ultimate tails |
| Ranger | 2 → 1 → 0 | 9 → 2 → 1 → 0 | Basics stack/refresh poison; fan waits for three stacks and consumes them once per cast; poison ticks, capped fan/explosions, rain and rank3 radial volley |
| Summoner | 2 → 4 → 1 → 0 | 9 → 7 → 2 → 4 → 1 → 0 | Bonded contracts, actual form attack/command cadence, thorn poison, finite mark ticks and finisher |

Priority means “first learned, ready and affordable action”, not a forced sequence ignoring cooldowns or energy. Level20 skips locked ultimate9. The CSV includes each committed skill and timestamp; the advanced Summoner often cannot afford the mark until later in the window. Skills outside these recipe lists are not evaluated. In particular, moving dash/fault geometry, shields, healing and the Ranger vulnerability-volley recipe are excluded rather than treated as stationary free damage.

Arcanist uses production OpeningFrostCounter, CombatProcCooldown, RecentCastGate and PlayerUpgradeRules.ShatterMultiplier(Balanced). The target accepts the nova frost window used by the recipe; actual control immunity/target-specific freeze resistance is excluded. Ranger/Summoner poison uses the production ScheduledTickWindow and StatusTickRates, caps three stacks, retains the strongest dose, refreshes without resetting tick cadence, and clears on detonation/expiry. Damage from reactions and poison is listed separately.

## Summoner lifecycle assumptions

A single foundation wolf is already present at time0, at the actual learned contract rank, and ready to attack. There is no pre-summoned spirit or treant. The bonded route creates at most one permanent wolf, one permanent spirit, and one separately capped timed treant. Contracts command the existing living partner rather than manufacturing another copy. Wolves use the production command strike and 0.6-second recovery; spirit/treant readiness uses the production command-ready bound. Command damage multipliers and duration come directly from CompanionRules. No perfect-dodge empowered command is granted.

Companions are already in attack range whenever commanded and remain able to attack the target; movement/acquisition delay and companion death are excluded. Permanent partners really are permanent; the treant uses its production finite lifetime. An additional 35-second test verifies the treant expires and stops attacking while the foundation partner remains. No pack reinforcements, TwinSummonResonance gear multiplier or gear-gated cooperation proc is included.

## Level100 comparison from this snapshot

| Class / recipe | Window | Committed skills | Basic hits | Spent / remaining energy | Skill + reaction + poison A | Pet A | Total A |
|---|---:|---:|---:|---:|---:|---:|---:|
| Vanguard / cleave_field | 5s | 4 | 11 | 78 / 98.40 | 21.760 | 0.000 | 32.760 |
| Vanguard / cleave_field | 10s | 6 | 22 | 110 / 100.00 | 32.240 | 0.000 | 54.240 |
| Vanguard / ultimate_field | 5s | 4 | 9 | 130 / 58.40 | 26.288 | 0.000 | 35.288 |
| Vanguard / ultimate_field | 10s | 5 | 20 | 142 / 100.00 | 32.858 | 0.000 | 52.858 |
| Arcanist / frost_meteor | 5s | 3 | 10 | 84 / 100.00 | 19.488 | 0.000 | 30.988 |
| Arcanist / frost_meteor | 10s | 4 | 19 | 102 / 100.00 | 26.720 | 0.000 | 48.570 |
| Arcanist / ultimate_field | 5s | 4 | 8 | 152 / 27.60 | 24.354 | 0.000 | 33.554 |
| Arcanist / ultimate_field | 10s | 5 | 17 | 170 / 82.52 | 37.002 | 0.000 | 56.552 |
| Ranger / poison_fan | 5s | 3 | 15 | 68 / 100.00 | 17.728 | 0.000 | 29.428 |
| Ranger / poison_fan | 10s | 4 | 30 | 82 / 100.00 | 30.336 | 0.000 | 53.736 |
| Ranger / rain_poison | 5s | 4 | 13 | 132 / 89.00 | 30.166 | 0.000 | 40.306 |
| Ranger / rain_poison | 10s | 5 | 28 | 146 / 100.00 | 43.474 | 0.000 | 65.314 |
| Summoner / bonded_commands | 5s | 4 | 10 | 102 / 98.00 | 7.104 | 17.441 | 36.045 |
| Summoner / bonded_commands | 10s | 5 | 20 | 118 / 100.00 | 13.440 | 28.289 | 64.729 |
| Summoner / treant_mark | 5s | 5 | 8 | 172 / 7.60 | 2.880 | 21.695 | 33.775 |
| Summoner / treant_mark | 10s | 7 | 18 | 228 / 51.60 | 17.792 | 40.137 | 78.629 |

The rows compare these declared recipes only. They are not a class-balance ranking: Vanguard shield retaliation and alternative skill packages are omitted, while pets are allowed to stay alive and in range. The 10-second Summoner treant/mark recipe spends228 energy and requires18 confirmed basics plus regeneration; its 40-energy mark commits only at6.92 seconds. Adding the entire mark budget to a 5-second burst would therefore be incorrect.

Vanguard’s ultimate-first recipe yields slightly more modeled 5-second output than the starter recipe (35.288A versus32.760A), but slightly less over10 seconds (52.858A versus54.240A) because charge/energy opportunity costs change the actual casts and basic hits. This does not evaluate the ultimate’s larger area or control value.

## Standalone spell analysis before and after the specific budget correction

Each row below is one rank3 paid cast with no basic attacks. Foundation-wolf damage is excluded from the listed spell/treant coefficient, so it cannot be mistaken for damage caused by the field. The full-lifetime endpoint includes startup/charge and poison that remains after the final field tick.

| Ability | Window from request | Cost | Before direct + poison A | Before total A | Corrected direct + poison A | Corrected total A |
|---|---:|---:|---:|---:|---:|---:|
| Thorn | 5s | 28 | 6.160 + 2.992 | 9.152 | 3.360 + 1.632 | 4.992 |
| Thorn | 10s | 28 | 7.920 + 6.688 | 14.608 | 4.320 + 3.648 | 7.968 |
| Thorn | Full10.85s | 28 | 7.920 + 7.216 | 15.136 | 4.320 + 3.936 | 8.256 |
| Gravity | Full2.60s / 5s / 10s | 40 | 9.440 + 0 | 9.440 | 7.040 + 0 | 7.040 |
| Treant only | 5s | 70 | Pet damage | 7.192 | Pet damage unchanged | 7.192 |
| Treant only | 10s | 70 | Pet damage | 14.152 | Pet damage unchanged | 14.152 |
| Treant only | Full17.10s | 70 | Pet damage | 21.112 | Pet damage unchanged | 21.112 |

Thorn’s production tick coefficient changed from0.55 to0.30, retaining the existing20% per-tick poison dose, poison stacking and duration. Gravity ticks changed from0.45 to0.30 and its finisher from3.2 to2.6. This addresses the specific low-cost field budget: the previous rank3 thorn produced15.136A after lingering poison, versus6.4/6.8/7.2A direct budgets for the other early fields. It is not a whole-class change derived from the ideal pet rows. No pet or treant coefficient changed.

The separate ready rank3 foundation wolf contributes6.240A at5s and12.480A at10s in these no-basic standalone cases. It contributes13.520A through the thorn’s10.85s full window,4.160A through gravity’s2.60s full window, and21.840A through the treant’s17.10s window. The treant appears after1.10s charge, attacks for its actual16-second lifetime, receives one normal command buff, and expires; no spirit, pack, gear cooperation or dodge empowerment is present.

After regenerating all48 rows, non-Summoner rows and every cast/resource/basic-hit/pet-damage column were unchanged. Only the tuned Summoner spell and poison terms changed. The CSV contains corrected production values; the before-values above preserve the isolated diagnostic snapshot.

## Reproduction and checks

Run `ComboBudgetTests.Run(outputDirectory)` in the standalone test runner to validate and regenerate `Combo-Budget.csv`. The two new test sources compile with production GameTypes, CombatBalance, SkillDamageBudgets, SkillRuntime, ScheduledTickWindow, CombatDamage, ProjectileVolleyBudget, SummonerDamageRules, CompanionRules and PlayerUpgradeRules; `Tests/SkillRuntimeTests.cs` supplies only the Color stub. No ProgressionService, real save directory, filesystem character fixture or Unity object is required.

Tests compare repeated runs, verify legal ranks/prerequisites, reject locked skills, enforce one cost per committed charge, distinguish unfinished/cancelled charge and delayed impact, reconcile energy with actual SkillRuntime, reconcile delivered damage categories and full production skill budgets, retain projectile caps, include the intrinsic combo reactions, and bound pets/commands/lifetimes. The simulation records every cast and damage event for auditing; its reported coefficient is their sum rather than a hard-coded DPS formula.

`pending_skill_A` counts only already scheduled skill impacts beyond the window. It excludes hypothetical future input, future basic/pet attacks and unscheduled future status ticks. It must not be added to the window total.

## Production source fingerprint

Changing any coefficient or event schedule requires regenerating the CSV and this summary. Fingerprints capture the exact source inspected for this artifact, not a released build ID.

- `Assets/Scripts/Core/SkillDamageBudgets.cs`: `7612982bc3e1ee3539e7088822b13c07909ecefd6b9aeea6d90186c772a0c2d6`
- `Assets/Scripts/Core/SkillRuntime.cs`: `ae08b98e0d51fc4a45eb41341e10dd03124349076061a2a3fd52301823336afb`
- `Assets/Scripts/Core/GameTypes.cs`: `4c2d39db931e5132bd6a56190e8b7bc06a7e39e89ad8c4c121cf6207aee7e804`
- `Assets/Scripts/Combat/PlayerController.cs`: `b9dec630a4d64700147d1f77fb1575be0f3a4b26a0de7e0f5f11c76eb7a02581`
- `Assets/Scripts/Combat/PlayerUpgradeRules.cs`: `a96588f20e187a04d991f9fa778e0a7558127ba8a09423bb8c98e418d4210e54`
- `Assets/Scripts/Combat/AdvancedSkillSequence.cs`: `a96fd4e147736148b2c71ce09cef7001ee9a99bb2bda9c74d2c1c8d22acb7b6c`
- `Assets/Scripts/Combat/SummonerDamageRules.cs`: `175c6a26d84e8c89d697d131772bb2a1cfbb4347024eca154f6d058036d3d834`
- `Assets/Scripts/Combat/SummonerSpell.cs`: `95378b538d34b5be5380b87c3094927dffc1d22a839af7040f5e3335a721b0ce`
- `Assets/Scripts/Combat/CompanionRules.cs`: `396a6278308ec9e802203719671c8a586571fc1a011d4a138595e2452d098373`
- `Assets/Scripts/Combat/SummonedCompanion.cs`: `bffc12cbe7fcb95e6b0c5ed40c1335de9628eb35cd4f48295300d126f5a7da14`
- `Assets/Scripts/Combat/EnemyStatusEffects.cs`: `bd860b55f7a29b63b603d977cee4358964534f92346f22b2af610a9917b5f865`
- `Assets/Scripts/Core/ScheduledTickWindow.cs`: `7156afe825b8af1e5cd659044b3578460347da1605250568e5c5a4407a47617d`
- `Assets/Scripts/Combat/CombatEffects.cs`: `4c4f7bb92640fda445f415953452c31d499172094a6a290eae424d89297d0d8a`
- `Assets/Scripts/Combat/ProjectileVolleyBudget.cs`: `94fd1d71f0f630128656d342a396e301e11fc1e6def135a8dd1ebefb76813492`
- `Tests/ComboBudgetSimulation.cs`: `e52e63b35450e865f1c47508f914a514190ca16eacbf0f8c65ca2cbb065c3c79`
- `Tests/ComboBudgetTests.cs`: `a91367d060572c79b796ade5f103a81bc75c4e60033183dcb5d764759375a217`
