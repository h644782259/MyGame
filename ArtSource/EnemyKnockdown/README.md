# Controlled knockdown pose review

The Goblin and ordinary Guardian retain their production articulated rigs and meshes. `CombatModel.Knockdown.cs` adds a continuous visual envelope driven by the **already granted** knockdown timer: fall, grounded hold, supported recovery. It changes no stun/control duration, collision/navigation transform, damage, stats, or attack cancellation. Ordinary hit recoil still overlays the current attack without pretending to interrupt it.

A small humanoid falls over .13 seconds and begins recovery in the last .26 seconds; the heavy Guardian uses .18/.34 seconds. Short controls blend only as far as their remaining duration permits. Reapplying control reverses recovery from the current pose. Airborne releases the grounded pose and retains the status's authoritative height. Death keeps ownership of the final displayed pose. Pause resamples the retained pose without advancing or accumulating offsets. Unsupported enemies retain the previous status presentation.

These variable-duration transitions are runtime pose composition, not a fixed Blender animation substituted for gameplay timing. `KnockdownPoseReview.blend` and `.png` are **Blender 4.3.2 previews of actual production factory vertices transformed in a managed test harness**, not Unity screenshots or imported animation clips. The ten editable mesh collections show upright/fall/down/rise/standing for each enemy. No external art or paid assets are used.

Reproduce from the repository root:

```sh
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownProductionTests.py
DOTNET=/path/to/dotnet python3 Tests/EnemyKnockdownGeometryTests.py --output /tmp/enemy-knockdown-geometry.json
blender -b --factory-startup --threads 2 --python ArtSource/EnemyKnockdown/render_pose_review.py -- /tmp/enemy-knockdown-geometry.json /tmp/enemy-knockdown-review.png
```

Validation: the production pose/status tests execute `CombatModel.Animate`, `Recoil`, `ApplyRecoil`, the complete knockdown partial, and `EnemyStatusEffects.LateUpdate`. They do not execute the damage/controller update loop. The geometry suite constructs both actual production factories with real authored binary meshes, then checks 1,900 frame samples across four frame rates and four control durations. Lowest sampled vertex heights are Goblin .0194 m and Guardian .0131 m. This is managed TRS evidence; Unity import, rendering, skinning, collision, devices, and actual frame scheduling remain unverified.

Runtime budget: no added triangles, textures, meshes, materials, draw calls, or resources. The overlay retains two arrays of eleven joints/rotations after first use, restores their base state, and applies eleven joint blends only while its weight is nonzero. The `.blend` and PNG are source/review artifacts outside `Assets`; they add no player package content. Existing mesh GUIDs and old non-articulated/boss fallback remain intact.
