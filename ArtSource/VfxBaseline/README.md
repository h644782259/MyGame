# Rank-1 VFX comparison baseline

Pinned Windows source: `03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e`. This is a source-geometry/timing export, not a Unity recording. No runtime files are changed.

```
python ArtSource/VfxBaseline/export_baseline.py REPOSITORY OUTPUT DOTNET
```

The script reads every production source using `git show PIN:path`, compiles the actual `FilledSkillVfx.Crescent` / `Animate`, mesh recipes, `CombatFx.Ring`, `FadingCombatEffect.Setup` / `Update` with managed engine doubles, and emits `baseline-vfx-03422ab.json` plus source hashes. PlayerController cast dispatch is inspected and explicitly mapped, not executed as a complete gameplay simulation.

Schema: `fps=24`, `duration=9`, 216 frames. Cast times .75 / 2.75 / 5 / 7; slots 0 / 0 / 1 / 1, all rank 1. Coordinates are Unity world +Y up / +Z forward. Each frame contains world-space mesh vertices referencing shared triangles/UV, material `color`, `opacity`, `progress`, `style`; rings contain 64 world-space centerline points, width and color. Empty frames are intentionally empty. Cast handedness follows four PlayAction calls: -1,+1,-1,+1. Cooldown gaps are edited; effect ages run at 1×. This is not a continuous legal cooldown replay.

Actual baseline:

- Whirlwind: radius 3.4 ring, .45 s, requested width .2 clamped to .12, desktop alpha .66. No filled crescent is spawned by this slot. `Melee` adds no automatic slash.
- Groundshock: one WeaponSlash radius 4.8, color (1,.85,.4), .34 s: TWO closed crescent meshes plus FOUR crystal shards. A separate ring is centered forward 2.5, radius 2.1, .4 s, width .16. Ring class color is (1,.65,.26). No rank-2+ follow-up field is included.
- Desktop full-effects settings, open arena, no enemies. No fabricated target hit effects or damage claims.

Limits that must accompany the video:

- Real baseline also has an animated sword root-tip ribbon. It is intentionally **not exported** because this fixture does not reproduce animated weapon sockets. Do not imply it is absent from the game.
- FilledSpell fragment shader uses UV grain, normal-dependent shading, multiplicative alpha (`color.a * opacity * grain/dissolve`) and alpha clipping below .025. The uniforms/UV are exported; an ordinary Blender transparent material is an approximation.
- Unity LineRenderer's camera-facing ribbon tessellation, transparent sorting, GPU shading and actual engine frame scheduling are not reproduced. Ring point geometry and sampled expansion/fade are actual production results.
- Managed quaternion/TRS reconstruction is not Unity execution. The hero body is only a shared reference, not the subject of this VFX comparison.

Validation checks the 216-frame sequence, finite geometry, triangle/vertex mappings, release frame (one primary crescent before delayed pieces), .125 s frame (six pieces), empty tail after .5 s, exact ring widths, and alternating cast sides. Baseline native renders must retain the closed volumes and shard surfaces rather than simplifying the old version to flat lines.

## Complete baseline with dynamic sword ribbon (preferred)

```
python ArtSource/VfxBaseline/export_with_ribbon.py REPOSITORY OUTPUT DOTNET
python ArtSource/VfxBaseline/validate_ribbon.py BASE_JSON OUTPUT/baseline-vfx-with-ribbon-03422ab.json
```

The complete exporter now includes the real sword trail, superseding the no-ribbon limitation above. It executes pinned `PlayAction`, `AnimateHero`, `Pose`, `WeaponRig` anchors and `WeaponSlashRibbon.Spawn/LateUpdate/Sample`. Original unit-scale spine/shoulder/elbow/wrist offsets were separately checked against the full baseline Hero constructor. Zero locomotion has no upper-body movement contribution; breath phase is fixed at 3.14 (matching the previous baseline hero fixture). The skill pose begins at .52 of .68 s; trail lives .22 s and is sampled sequentially at 24 Hz, retaining actual previous endpoints. This is an honest 24 Hz sampling fixture, not a claim of device refresh-rate equivalence.

Only frames 120–125 and 168–173 add a trail (12 frames). All previously exported filled surfaces/rings are byte-equivalent. Shared mesh `Weapon root-tip swept volume` has 8 vertices and 12 triangles, with actual .024 thickness. Material is the SAME FilledSpell shader: `_Opacity=1`, `_Progress=0`, `_Style=0`; no UV exists in production, so default (0,0) is exported. Fade changes material color alpha. Optional `sockets.root` and `sockets.tip` are world-space validation metadata.

Complete JSON SHA256: `1dd484a473987317fc6dbb6e687cd00af8a19e41d8e5c4a78a88a763b2de9f23`. Independent rebuild matches exactly. Validations check 12 changing socket/fade samples, prior endpoints matching successive swept mesh corners, .024 thickness, exact changed frames and preservation of every previous effect. Shader/LineRenderer/Unity-rendering limitations remain. A static body reference in a VFX-only comparison must not be described as a full animated gameplay recording.
