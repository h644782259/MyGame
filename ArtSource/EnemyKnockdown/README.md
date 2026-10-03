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

Validation: the production pose/status tests execute `CombatModel.Animate`, `Recoil`, `ApplyRecoil`, the complete knockdown partial, and `EnemyStatusEffects.LateUpdate`. They do not execute the damage/controller update loop. The geometry suite constructs both actual production factories with real authored binary meshes, then checks 1,900 frame samples across four frame rates and four control durations. Lowest sampled vertex heights are Goblin .0194 m and Guardian .00284 m. This is managed TRS evidence; Unity import, rendering, skinning, collision, devices, and actual frame scheduling remain unverified.

Runtime budget: no added triangles, textures, meshes, materials, draw calls, or resources. The overlay retains three arrays for twelve joint/body transforms and their base/display rotations after first use, restores their base state, and applies eleven joint blends only while its weight is nonzero. The `.blend` and PNG are source/review artifacts outside `Assets`; they add no player package content. Existing mesh GUIDs and old non-articulated/boss fallback remain intact.

## Death-order correction and support evidence

`CombatModel.Animate` restores the procedural base before status `LateUpdate` composes knockdown. A kill between those callbacks used to capture that temporary upright base. The model now retains the last complete displayed root, joint, and recoil-body rotations. `BeginDeath` restores that display state before `EnemyDeathDissolve.Initialize` captures it. Zero-weight standing/airborne samples invalidate the cached pose, and repeated death initialization never rewrites the death-owned transform. No status timer is advanced by this handoff.

The death suite executes the actual enemy controller `Update` prefix through its stunned animation/return branch, actual `AnimateModel` and both `BeginDeath` methods, the actual dissolve capture prefix, and actual status `LateUpdate`. It covers kills before Update, between Update and LateUpdate, and after LateUpdate, for held/re-controlled/recoil/airborne/paused-airborne/fully recovered states on both rigs. Unstunned AI/navigation, lethal damage resolution and death particles remain explicit fixture boundaries.

`GuardianDown-Low.png` and `GuardianDown-Side.png` use one identical exported production down pose, at the actual y=0 reference floor, without per-camera geometry adjustments. Their editable scene is `GuardianDown-Support.blend`; reproduce with `render_support_review.py` using the geometry JSON above. Independent support checks measure both boots (.02218/.00284 m), grounded body shoulder armor (.01310 m), and the hammer (.29975 m). The chest/back mesh itself remains .19723 m above the floor; the stylized pose rests on thick shoulder armor and boot ends. These are mesh/contact-height observations, not a rigid-body stability or Unity physics claim.
