# Blender art upgrade: bounded inventory and acceptance

Start state verified against remote main on 2026-10-03: Windows `25096ba7dbf6e9d7ba9463ec29104c2c462da426`; iOS `1cd7ce36c77064757899788e23520fb796266888`. Both clean. Android provided baseline `8654c803eb29b87dea7d69f09678ec21e1955f6d` cannot be independently verified: remote Git and connected GitHub API return repository not found. No repository creation, login or credentials change is authorized or attempted.

No AGENTS.md, .instructions or .agents/skills present in these checkouts; workspace .agents/.codex empty. Blender 4.3.2, FFmpeg and Python are available. Historical Unity/.NET paths are absent in this fresh environment. .NET 8.0.425 reinstalled from the official dotnet installer to workspace tools for source checks. Unity execution/device capture remains unverified.

## Existing assets and live call sites

| Asset family | Existing implementation | Upgrade boundary |
|---|---|---|
| Four hero classes | PlayerController -> CombatModel.Hero; same factory in collection preview | Rigid visual meshes below existing animated joints; keep full action/gear contracts |
| Sword, bow, staff, armor and fashion | CombatModel, WeaponRig, Costumes, CostumeLayers | Preserve anchor positions, tier/color/structure identity and attachment hierarchy |
| Wolf, Spirit, Treant | SummonedCompanion -> CombatModel.Companion | Preserve gait, jaw/tail motion and summon lifecycle |
| Slime, Goblin, Wisp, Guardian | EnemyController -> CombatModel.Enemy | Preserve squash/float/rig animation and hit footprint |
| Guardian boss, expedition astrolabe | CombatModel and LargeBossRig | Preserve exposed core/shutdown and beam emitter movement |
| Tent, campfire, crate | Existing BlenderPilot FBX gated by default-off hero switch | Reuse existing authored assets through independent scenery fallback |
| Pot, rubble, trees | DestructiblePropFactory; WorldBuilder.Tree/BuildBranchTree | Replace intact decoration only; preserve navigation, destruction and occlusion |
| Skills | FilledSkillVfx, CombatFx, PlayerController skill dispatch | First integrate approved whirlwind/ground-shock direction at existing release call sites |

Existing FBX inventory: Vanguard body 2328 triangles + embedded sword 392; standalone StarcoreSword 392 (not called separately); WayfarerTent 1588; SupplyCrate 1228; StarEmberCampfire 842. One Standard material, two 1024-square textures. Hero pilot default-off and supports only restricted starter gear and five clips; this work does not broaden that incomplete animation gate.

The existing VFX Before/After MP4s are 1280x720, 24fps, nine seconds. Inspected actual decoded frames: After has closed amber rotating blades/bright cores and ground crest/chips. Before is production-code reconstruction, After is Blender proposal, neither Unity capture. The latest user request removes baseline recreation and per-batch comparison videos; inspect new output and retain compact evidence instead.

## Finite delivery batches

1. Skill VFX: authored offline shapes and live whirlwind/ground-shock visual integration; keep damage, timing, ranges, rank rules, primary feedback, reduced effects and lifecycle controls.
2. Actors/equipment: selected characteristic rigid meshes for the four classes, their weapon families, three companions, four enemy families and boss bodies using existing animation nodes. No new animation system.
3. Scenery: reuse three pilot props, author pottery/rubble/open canopy assets at existing call sites. No changes to random room layouts, collision, navigation, water or danger geometry.

No new assets are required for runtime-only target outlines, threat clipping, camera-facing labels, dynamic water, hit detection, projectile paths, or topology dictated by room seeds. Existing detailed weapon fashion and procedural architecture stay when replacement would discard their meaningful identity. Exact delivered coverage and deferred items are recorded with each batch.

Every batch preserves editable source, reproducible generator, stable .meta GUIDs, budgets/provenance, procedural fallback and inspection evidence. Windows/iOS share changed source and resources only, preserving platform configuration. Android-compatible source is retained as a portable patch/archive if repository access remains blocked.

Draft PRs are technical review checkpoints for the parent; do not merge directly. Managed/source/API checks are not Unity asset import, shader rendering, Play Mode, device performance or platform builds. New assets require that distinct acceptance before claiming a validated release.
