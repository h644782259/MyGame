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
