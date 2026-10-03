# Vanguard authored skill surfaces — batch 1

Original Emberfall geometry, derived from `ArtSource/BlenderPilot/render_vfx_comparison.py`'s user-approved optimized proposal. No external assets, textures, purchases or generated gameplay rules. Editable source: `Vanguard-Skills.blend`; generator: `build.py`.

`BlenderSkillVfx.TryPlay` is integrated at the existing Vanguard slot 0/1 release. Melee damage, range, cone, status durations, rank followups and growth values are unchanged. Whirlwind uses three closed-volume amber blades and two separately authored narrow hot cores; ground shock keeps the existing immediate contact ring and weapon ribbon, then adds a short crest/fault/chip wake. This is a bounded adaptation of the proposal, not a pixel-identical port of the Blender compositor. A 1.15-second tail is visual only and implies no later impact. The effect remains at the cast origin.

Only missing/invalid assets, unsupported shader, or an uncertified full rotating footprint use the original presentation. Near cover we deliberately keep the existing path rather than projecting an authored effect through a wall. Budget rejection suppresses the new decoration without resubmitting a lower-priority fallback. Owner death, epoch change, session replacement/end retire it; blocked input and zero scaled delta pause it.

Runtime geometry is little-endian `.bytes`, decoded by the shared strict `AuthoredActorMeshes.Decode` loader. Blender Z-up is explicitly converted to Unity Y-up `(x,z,-y)` for vertices and normals; no FBX/OBJ import transform is required. OBJ exports under ArtSource are editable interchange only. Runtime GUIDs are deterministic and remain stable across rebuilds.

Budget: four buffers, 11,624 bytes total; no textures; one shared existing FilledSpell shader/material. Up to 12 desktop, 8 mobile, 5 reduced renderers per cast. Whirlwind maximum 1,022 source triangles; ground shock maximum 68. Global CombatVisualLease limits remain authoritative. Build logs and `budget.json` record exact source triangles/materials/bytes/hashes. Runtime import metadata and engine packaging overhead are not measured.

Rebuild from repository root:

```sh
blender --background --python ArtSource/BlenderVfx/build.py
python3 ArtSource/BlenderVfx/validate.py
blender --background --python ArtSource/BlenderVfx/preview.py
```

`Authored-Skills-Preview.png` was visually checked: separated curved volumes and narrow bright cores, small ground crest/fault silhouettes. This is a Blender still of authored geometry at approximately 0.18 seconds. It is NOT Unity rendering, shader parity evidence, device performance validation, or an approved gameplay capture. `build.log`, `preview.log`, `validation.log` are raw outputs. Unity import/actual shader transparency, device performance and gameplay visual acceptance remain unverified. User requested new-asset checks only; no new before/after video was produced.

Excluded from offline replacement: danger/target boundaries, dynamic cover clipping, actual weapon endpoint ribbons, live beam endpoints, runtime collision/damage and area scheduling. Further reusable base meshes can be upgraded separately while retaining those systems.

Production-component tests: `DOTNET=/path/to/dotnet python3 Tests/BlenderSkillVfxProductionTests.py` executes the actual decoder, VFX class and shared lease using managed engine substitutes and the four checked-in buffers. 584,457 assertions and four compiled negative controls passed (age-zero initialization, epoch cleanup, footprint containment, zero-normal rejection). The original log is `managed-production.log`. This does not execute Unity or the caller damage pipeline.
