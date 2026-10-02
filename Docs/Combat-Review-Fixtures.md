# Level 50 combat review fixtures (prepared, not run)

This is an opt-in Unity Editor manual-input review harness. It does not produce a pass verdict or simulate gameplay when Unity is unavailable. The current cloud attempt could not download the official editor because its proxy returned HTTP 403. No gameplay log, screenshot or video accompanies this preparation.

## Fixed configurations

`CombatReviewConfigurations.Create()` provides `vanguard`, `arcanist-shatter`, `arcanist-burn`, `ranger`, and `summoner-bonded`. Every configuration uses level 50, seed 61453, all ten skills at rank 1, zero mastery, default hotbar, starter equipment, no equipment mechanisms, and the Bonded summon route. These are controlled introductory ability baselines, not optimized level-50 builds or a damage ranking. Skill investment, equipment and difficulty are deliberately explicit. `profile.json` captures the exact generated equipment and all profile fields; GUIDs may differ between fresh characters even with the same random seed. The seed controls Unity randomness, not the separate loot RNG; starter item stats are fixed by production CreateProfile. Later loot rolls are not promised deterministic.

Scenarios preserve the actual wilderness ground and collision rules, with nearest-walkable spawn placement recorded in the log:

- `stationary`: level-50 Goblin, AI disabled; real damage/status/reward logic remains active. Finite normal health, no hidden immunity or heal loop.
- `moving`: level-50 Goblin with normal movement, attacks and damage.
- `formation`: level-50 Guardian and Goblin in front, level-50 Wisp behind; all use normal AI. This is a controlled enemy formation, not a new production level.

Unrelated wilderness enemies are removed and the ambient respawn timer is deferred beyond the session. No run rewards or real saves are used. Actual terrain can adjust requested spawn positions; compare the recorded final coordinates before comparing sessions. Killing the last target ends the useful test; restart for a fresh target rather than comparing invulnerable-target throughput.

## Run in a licensed Unity 6000.6.3f1 Editor

Save your currently edited scene. Use `Emberfall > Review > Start level 50 fixture` for the default Vanguard/stationary case, or launch a fresh editor with explicit parameters:

```sh
Unity -projectPath /absolute/path/to/emberfall_win \
  -executeMethod Emberfall.Editor.CombatReviewFixture.Run \
  -reviewBuild arcanist-shatter -reviewScenario formation \
  -logFile /absolute/path/to/editor-review.log
```

Do not pass `-quit` or `-nographics`. Select each of the 15 build/scenario combinations in separate sessions. The harness creates `Tests/TestResults/CombatReview-<unique id>/IsolatedSave` before loading Main; it checks the resulting save file path, restores the prior override on leaving Play Mode, and never opts into a normal player save. It refuses an active play session or unsaved current scene. Stop Play Mode to finish, or let the 180-second / 100,000-event cap stop it.

Use real input. For each build/scenario: hold basic attack for ten seconds; repeat while moving sideways; cast the class combo; start then cancel a charged skill; exhaust energy and attempt another cast; let a moving enemy hit/kill the hero; for Summoner, focus a distant enemy and observe the pet approach. Record exact actions, input device, commit (`git rev-parse HEAD`), quality setting, resolution and capture start/end frame in an external review note. The harness logs actual starting resolution and quality. It does not fix OS DPI, emulate touch, drive inputs, record video or assert visual quality. Capture actual Game-view/player video separately if the engine runs.

## Evidence semantics and integration

`configuration.json`, `profile.json`, and streaming `events.jsonl` are written only by the real editor session. Rows include Unity frame, scaled game time, actor/target instance IDs, amount, skill and detail. No synthetic damage, hit or miss data is shipped.

`CombatReviewEvents` is an opt-in event bus: no listener means no history; subscriber exceptions cannot affect combat. Production Player/Projectile/Charge hooks are owned by the combat-timing slice and must be integrated alongside this fixture. Those callbacks provide actual attack/skill attempts, damage/hit/miss or projectile-end reasons, charge cancellation, no-energy refusal, incoming damage and death. Interpret each event by its hook: an epoch cancellation is not a miss, a projectile touch is not automatically accepted damage, and sampled target health loss must not be counted again as hook damage. This fixture alone does not guarantee hook coverage. Review raw emitted event kinds and hooks before summarizing totals.

The separate `observed_enemy_hp_delta` is a 100ms aggregate health observation, not per-hit attribution; a dead/removed enemy may leave the registry before this sample. `petchase` records observed movement with a non-null target and target distance, not an inferred attack or path success. Pet target IDs make switches visible. Optional `HoldState` and `HoldIdleWaitSeconds` properties, when present and a hold mode is entered during the isolated session, are sampled as state/cumulative wait; the three initial fixtures do not automatically enter that mode. Paused frames carry no invented combat events. The logger has bounded time/event output and flushes every sample.

Prepared tests check bus payload delivery/isolation/unsubscribe, independent configurations and save/logging source guards. These are not Unity compilation, Play Mode execution, animation synchronization, real hit-rate, performance or device acceptance evidence. No validation runner was changed.

## Summarize recorded events

```sh
python3 Tools/summarize-combat-review.py Tests/TestResults/CombatReview-<id>/events.jsonl
```

The summary counts raw kinds and adds only `damage` callbacks for damage totals (not projectilehit or sampled HP a second time). `derived_projectile_no_enemy_hit` counts `projectileend` with `expired`/`terrain` and `hits=0`; retired/disposed projectiles are excluded. This is the transparent ranged-miss proxy, may include projectiles that struck props, and is not whole-attack/melee/skill miss rate. `projectilehit` details retain projectile IDs. `enemydeath` and player `death` are separate. Unobserved event kinds are listed as missing evidence, not zero failures. No synthetic parser test output is a gameplay report.

`basicmiss` / `spellmiss` 来自已释放近战在该次直接伤害检查中没有造成敌人生命损失，或已尝试射击但枪口受阻；不统计可破坏物，不代表所有持续领域/后续命中都落空。汇总保留事件类别，不将其与投射物到期代理合成统一命中率。

## Unity 6 identity compatibility correction

Recorder v2 emits `actorId` and `targetId` as invariant decimal **strings**, including `"0"` for absent/destroyed objects. `CombatReviewObjectId` uses `EntityId.ToULong(GetEntityId())` on Unity 6.4+ and signed `GetInstanceID()` only in the older conditional branch. No integer cast, hash, registry, or object retention is involved; every raw bit is preserved. IDs identify live objects within one recording session, not persistent save identities or cross-run comparable values. Unity's raw bit layout can change between engine versions, so consumers treat these strings as opaque keys. Projectile detail IDs use the same format.

The summary accepts legacy integer IDs and new strings without float conversion; fixture health/pet dictionaries use string keys. Tests include adjacent values at ulong max, signed legacy values, repeated reads, deliberately colliding object hashes and null/destroyed objects. API-double tests do not establish engine compatibility: actual Unity 6000.6.3 reference compilation must be reported separately. The previous frozen c022a463/11d02c8 candidates failed that check because `GetInstanceID` and EntityId-to-int conversion are error-obsolete; legacy 2021 compilation was insufficient.

API references: https://docs.unity3d.com/6000.6/Documentation/ScriptReference/EntityId.ToULong.html and https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.GetEntityId.html .
