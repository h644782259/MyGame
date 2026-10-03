# Finite Blender upgrade and six planning changes — final review

The approved finite art roster is implemented and connected to actual factories and skill events. The final combined branch also includes the separately authorized six planning changes. Draft PRs remain open for parent review; no merge is performed by this task.

## Exact validation source

- Windows: `bc6a572a86fe4376f6f346b0fe52f20d9678aec3`.
- iOS: `30f748b1bf6da7def9d91118aa756cd225c03e79`.
- Parent-reviewed main incorporated: Win `4214b885b5346b4e4987e9d50a3abf27f329db5c`, iOS `c1a49acd52cac28d1408a6d9bb25af971f7e3343`.

Final full result: PENDING. The pure-art 227/227 report predates the last persistent-protection correction and is retained as historical evidence. The earlier combined candidate was interrupted after review exposed the mobile counter admission defect; its logs remain under `Docs/Validation/Combined-Superseded-Candidate`. Neither substitutes for this exact final tree. A second candidate was interrupted when review found missing death-persistence fixture dependencies; its partial run is retained under `Combined-Envelope-Candidate-Interrupted`, with exact before/after failure and repair evidence in `VanguardDeathFixture`. The subsequent complete preliminary run passed 231/233: two skill/status fixture compile dependencies failed, and a broader input audit caught the defense test overwriting historical JSON. `Combined-Fixture-Preliminary` retains the complete failure report and before/after evidence; fixture repairs preserve production behavior and isolate default test exports.

## Scope and concrete changes

| Batch | Actual connection and evidence |
|---|---|
| Earlier accepted skills / E01–E06 | Recognized skill previews connected first; complete procedural action rig with authored additive channels, real status retirement, skill identities, captured death pose, tactical attachments and generation-safe arrow leases. Original incomplete whole-body FBX pilot stays disabled. |
| F1 heroes / companions | New rigid robe, hood, feathers, antlers, spirit and treant pieces on existing joints; existing faces, cloth and qualified body parts retained. `ArtSource/ActorSilhouettes/` and `IntegratedActorArt/`. |
| F2 enemies / boss | Goblin/Guardian pieces and boss anchor plinth/crystal/claw; qualified Slime/Wisp/astrolabe body retained. Actual control/death poses and anchor ownership preserved. `ArtSource/EnemySilhouettes/`. |
| F3 weapons / fashion | Original authored sword split into four real attachments, bow/staff small modules; T4 qualified silhouettes and grip/string/ribbon anchors retained. `ArtSource/WeaponModules/`. |
| F4 fixed scenery | 27 modules at real factories for six groups and three NPC stations; 116 enabled constructions, every missing/corrupt resource route, real navigation/occlusion registration. `ArtSource/FixedScenery/EnabledIntegration/`. |
| F5 projectiles | Ordinary arrow/caster/contract/hostile identity hooks, meteor and bounded trap decoration. Runtime flight, collision and actual timing retained. `ArtSource/BlenderProjectiles/`. |
| F6 skill closure | Forty slots classified in `SkillCoverage.md`; real guard/passive/state ownership. Final equipped-body check corrected persistent Y scale minimum to 2.6 and steady birth motion, XZ scale minimum unchanged at 2.6 (scale parameters, not meters). 59 poses × 28 actual trigger/time samples, eight negative controls. `ArtSource/FinalBodyEnvelope/`. |

Dynamic hazard boundaries, damage/target geometry, navigation, procedural layouts, beams/ribbons and live clipping remain runtime systems. The art work does not change gameplay ranges, damage or schedules. Explicit balance changes below are independent planning requirements, not silent art adjustments.

| Planning PR | Result |
|---|---|
| 33 | Companion pack targeting priority. |
| 34 | Harder star sweep with the requested 14m / 18-degree geometry. |
| 36 | Retain death gold/loot and same-seed retry; preserve pending world pickups before saving, including partial-save/retry cases without duplicate rewards. |
| 43 | Restricted challenge-dungeon healing over five seconds at 60/70/80%; ordinary healing/companions unchanged, target/cost and lifecycle checks retained. |
| 44 | Camp draft changes remain local until one atomic apply/save, with stale-profile and failed-save handling. Existing 0.5 HP stays 0.5; dead actors stay dead and no cooldown/energy refill. |
| 45 | Returning Blade B perfect-dodge counter, shared mobile admission/execution landing prediction and desktop A/B switch. Existing A remains separately selectable. |

## Resource and platform closure

`ResourceBudget.json` verifies all 124 resource payload hashes at this freeze. Relative to original main: 112 new resources / 1,094,536 source bytes, 111 meshes / 11,866 unique source triangles, zero new texture or material asset files, zero modified/deleted pre-existing resource payloads. These are source/library figures, not packaged size, frame cost or memory. Per-instance/material/low-tier budgets remain in `Cumulative-Budget.md` and batch manifests.

All original GUIDs are preserved (Windows 306; iOS 309), with 142 new paths including two planning sources. All 273 runtime C# files and 813 common runtime input paths match except the preserved original Fonts.meta platform difference; four additional iOS-only font files also remain at their original baseline. At the complete Assets scope, Resources.meta also retains its original platform-specific blob. Full Assets/Tests/Tools/ArtSource hashes and platform-only input checks are in the final validation manifest; platform-specific fonts/export files remain unchanged.

Blender sources, reproducible builders, old-asset fallbacks, same-camera before/after stills and raw initial failures/negative controls remain in the repository. Images are production-geometry Blender reconstructions, not Unity captures. The final protection sheet writes actual MPB tint alpha × opacity to BSDF Alpha; its diagnostic initial opaque image is retained and labeled. The observation angle is not the game's camera.

## Android and remaining acceptance

`Docs/Validation/Final-Android-Handoff` records the exact offline source package, GUID audit, per-file manifests, generator and checksums. Full authoring/source archive: 166,612,907 bytes; runtime-only delta: 474,114 bytes; Windows-baseline binary patch: 219,660,079 bytes. These archives live outside Git and can be recreated from the frozen refs. They are not an Android checkout, APK or proof of patch applicability. Android remote access remained unavailable; no login, permission changes or new repository were attempted.

The current environment provides Blender 4.3.2 and .NET 8.0.425. Managed fixtures and pinned Unity 2021.3.33 API compilation are not Unity6 engine acceptance. Unity import/shaders, actual physics callbacks, gameplay-camera readability, Windows/iOS devices, frame time, memory and final package size remain unverified because Editor acquisition is blocked. Distant observatory marks, similar NPC bodies and Treant crown readability remain camera-review points rather than reasons to expand this finite asset list.

Review order: art PR37 → 38 → 39 → 40 → 41 → 42 → 46, plus planning PR33/34/36/43/44/45, then combined PR47. Each exists in both Windows and iOS repositories. Parent owns merge order and retargeting; keep platform settings intact. `Final-PR-Heads.json` records reviewed source heads; the final evidence-only PR47 head is reported in the PR handoff to avoid a self-referencing commit hash.
