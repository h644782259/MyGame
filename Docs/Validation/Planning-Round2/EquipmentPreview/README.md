# G06 equipment appearance comparison

Parent reward commit: `37d5407cf6ee78cfd9079ec59cd33fed97c998af`; source hashes in `source-manifest.json` identify this separate implementation batch.

## Actual entry and behavior

Inventory → 外观比较, on desktop and mobile, keeps one `CollectionModelPreview` host/render surface. Current/candidate switching preserves the fixed camera and other equipment/fashion. Candidate items still use the detached inherited-slot-upgrade preview. Actor geometry may be rebuilt when appearance changes; there are never simultaneous current/candidate mannequins.

- 展示观看: fixed near-front orthographic camera, size2.1 corrected for portrait aspect.
- 战斗观看: fixed perspective48° FOV, camera offset(0,17.1,-15.2) from target height0.7. These reproduce the default live camera's pitch48.36646°, distance19×1.2041595, and48° FOV (`GameSession.cs`). It intentionally does not mimic zoom/orbit changes chosen later by the player. At a matching aspect its default vertical occupancy corresponds to gameplay; no bounds fit enlarges small candidates. Both views retain a20° mannequin facing for comparison.
- Only the compared equipment subtree is tinted gold during rendering. Explicit weapon/armor/shoulder/head/relic groups exclude body/skin/wings/other slots. MaterialPropertyBlocks are restored in `finally`, including render errors; shared materials are not mutated. Fashion continues to cover hidden equipment; the UI explains when a compared weapon can therefore remain invisible.
- Short Move(1.4s) and Basic Attack(0.95s) use the isolated presentation clock and `SamplePreview`; they do not call live action dispatch, controllers, damage, projectiles, summons, energy or save APIs. Movement is a bounded two-step pose with0.45-unit maximum mannequin displacement, returning to idle. Imported idle is temporarily hidden for this pose-only movement so the procedural step remains visible; the guard is `isolatedPreview && actionSkill == -3`, leaving combat unchanged. Attack uses the existing class preview pose and ranger draw/release/reload.
- Six controls, render viewport, and explanation stay inside mobile body scrolling; short landscape cases568/800/1136×320 at scales0.75/1/1.5 are covered. No native screen or pixel claim follows from these managed layout assertions.

## Validation

All commands take `/workspace/shared/emberfall-tools/dotnet/dotnet` as their optional first argument:

- `Tests/EquipmentAppearanceProductionTests.py`: actual UI partial, current/candidate inherited item composition, single lazy host, two viewing modes, slot highlight routing, Move/Attack routing, preserved fashion/override copy, clipped mobile rectangles; raw-item and insufficient-height negative controls.
- `Tests/CollectionPreviewPresentationProductionTests.py`:1342 production host lifecycle assertions plus14981 existing surface/motion checks. New assertions inspect actual host camera params, candidate/move fixed framing, only-selected-subtree tint, exact property restoration and render-failure cleanup. Seven compiled negative controls include wrong combat FOV, missing tint and restored old equipment-idle freeze. Raw failure output is retained in `host.log`.
- `Tests/CollectionPoseIsolationProductionTests.py`:185 actual pose sampling assertions across four classes; local/world-clock independence, bounded move transform, no action identity dispatch, return to idle, and actual ranger arrow lifecycle. Existing clock/contact negative controls pass.
- `Tests/HeroPoseCommitTests.py`:54 live pose/commit/recovery checks and existing negative controls.
- `Tests/BlenderPilotAdapterProductionTests.py`:13774 layered sampling assertions,55 loader/readiness checks,116 adapter/ownership assertions and27 total compiled negative controls (including imported-idle fallback for short preview Move).
- `Tests/CollectionPreviewSourceTests.py` and `Tests/CollectionStageRecoveryNegativeTests.py`: existing isolation/resource contracts and compiled failed-stage cleanup control remain green.
- `Docs/Validation/Planning-sweep/compile-platforms.py`: all runtime source compiled against pinned Unity2021.3.33 references for explicit Windows/iOS/Android defines; all passed. `platforms/report.json` records input hashes and sourceUnchanged=true. This is source/API compilation, not an engine/player build.

One initial pose fixture lacked Unity's `Vector3.forward` member; corrected the explicit test double, retained `initial-fixture-forward-failure.log`. Production API compilation found no corresponding issue.

No Unity editor/player/GPU/device execution was available. No screenshot or MP4 is offered as engine evidence; actual tint appearance, small combat-scale readability, layout/font rendering and visual comparison require Unity/device acceptance. No new textures, meshes, materials on disk, or authored assets were added. Existing test registrations cover this batch; no cloud registry addition is required.
