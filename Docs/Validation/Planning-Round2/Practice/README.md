# Camp practice — package 5

Baseline: Windows main `5d85e47b9fab2489d5b06963a0b896ec19112740`.

Three selectable actual combat arrangements in the camp world: stationary Guardian, a continuously moving Guardian, and Guardian with a rear Wisp supplying 30% mitigation inside six metres and line of sight. Practice targets have 1,000,000 HP and ordinary armor/status behavior; they do not attack. Their HP is disclosed in the UI. No simulated damage score is presented.

The existing singleton GameSession temporarily owns a deep, memory-only ProgressionService and a new PlayerController. Original player/enemies/dynamic scene roots are suspended and restored by reference; the original world/navigation remains. Active charge blocks entry. Existing ground-loot collection, kill rewards, tutorial evidence and lifecycle save paths are gated during practice. Practice persistence writes are memory-only; load/create/delete are blocked. Existing original HP/energy/cooldown fields are not reset. Temporary actors/effects are retired before ownership restoration; a finally block restores ownership after teardown failures. The original camp UI panel and draft remain attached after practice.

10 or 60 seconds of advancing game time are recorded (pause/background time does not count). Damage is actual EnemyController health loss including companions/DOT/reactions. Energy is actual successful consumption and capped restoration from SkillRuntime. Successful cast IDs map to effective hit casts once per cast; missed casts and repeated ticks do not increase that hit-cast count. Mechanism counts use the existing actual RecordCombatAction calls. A/B retains complete immutable serialized profile snapshots and scene/seed/duration. Different scenarios, duration or incomplete records explicitly cannot be compared directly. Changing configurations is labeled A/B rather than silently described as identical. No automatic rotation or synthetic score is used.

Refresh restarts only the temporary trial and marks the previous partial record invalid; leaving, death and setup exceptions restore original ownership. Save failures on later explicit draft apply retain the draft and original profile. No ZIP/Library operation, new Android repository or main merge was performed.

## Evidence

- `service-tests.log`: 22 assertions against actual service/record/SkillRuntime, covering unchanged disk, deep profile isolation, three-point draft transfer, two practice snapshots, failed-save apply, apply once, cancel, clipped energy and cast-hit deduplication. Four compiled mutations fail their named behavioral assertions.
- `session-tests.log`: 29 assertions running the actual complete GameSession.Practice partial and the extracted actual PlayerController.Initialize method with Unity API boundary doubles. Three scenarios, duplicate start, restart, normal/early finish, exact original object/vitals restoration, RNG restoration and injected model-creation failure rollback; two compiled mutation controls.
- `draft-regression.log`: existing 289 desktop/mobile draft assertions and 136 build preset assertions plus four pre-existing compiled controls.
- `wiring-audit.log`: explicitly source-only audit of actual damage, cast/energy, reward/loot/save/tutorial and UI gates.
- `windows-api.log`, `ios-api.log`, `android-api.log`: pinned Unity 2021.3.33 API reference compile, zero warnings/errors. Initial iOS/Android launcher failures from an unset writable DOTNET_CLI_HOME are retained separately; reruns use the existing scratch CLI home.

These are managed tests/API compilation, **not Unity execution or device acceptance**. Still required in Unity/Win/iOS: actual touch/keyboard play and visible targeting, on-disable effects/cloth ownership, companion/DOT timing under scene activation, collision/occlusion, background resume, engine destruction ordering, abnormal callbacks during teardown, and same-condition 10/60-second capture. There is no fabricated gameplay recording or claimed FPS/device result.
