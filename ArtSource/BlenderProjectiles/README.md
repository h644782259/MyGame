# F5 ordinary projectiles and trap preparation

Six original meshes made in Blender 4.3.2; no external assets, paid services or textures. `build.py` authors editable `Projectiles.blend` and exports Unity-axis EFM1 bytes through the existing strict `AuthoredActorMeshes.Decode` contract. GUIDs are deterministic and retained when rebuilding. `budget.json` contains exact bounds, triangle counts, byte counts and SHA256.

Actual integration is `CombatProjectile.Friendly/BasicShot/Hostile → Make → AuthoredProjectileMeshes.Load`. Only the existing visual child's mesh changes. The root, trail, source bridge, material ownership, collision radius, trajectory, target, volley, damage, event timing and lifetime are unchanged. An arrow uses its explicit flag; companionSource or Summoner identifies a contract bolt; other friendly non-arrows use a decorative caster shape; Hostile explicitly selects the enemy shape. Colors never choose geometry or imply a real damage element. Positive local Y is arrowhead-forward under the existing +90° X root rotation.

`CombatArea.Spawn` now loads MeteorRock with the existing WeatheredRock fallback. The old meteor was already weathered stone. The new single kernel has recessed channels and facets; it uses fewer triangles without a new flame renderer. Original 9→.5 quadratic descent, 120/70 rotation and disappearance at startup are intact.

The Ranger Neutral/statusSkill1/startup>0 preparation gets TrapCore. The rank2 follow-up does not create a second fake trap. This optional child has a Decoration lease using existing desktop32/mobile20/reduced12 caps. Higher priority contacts can evict it without touching the gameplay area. AnchoredImpactMesh clips the core against actual cover; a result exceeding128 triangles omits the optional core and preserves the original marker. The area removes it at its existing age>=delay boundary and owns destruction on room/death retirement. The component owns its clipped mesh/material; cache owns source meshes. No additional gameplay update loop.

| Mesh | Old triangles | New triangles | Runtime bytes | Renderer/material change |
|---|---:|---:|---:|---|
| Ordinary ArrowBody |416|48|5196|0; existing body+trail,2 renderers/2 materials|
| CasterBolt |384|24|2604|0; existing body+trail,2/2|
| ContractBolt |384|64|6924|0; existing body+trail,2/2|
| HostileBolt |384|24|2604|0; existing body+trail,2/2|
| MeteorRock |192|116|12540|0; existing single body,1/1|
| TrapCore |0 additional|68 source; <=128 clipped|7356|+1 renderer/+1 material during first preparation only; shared lease cap|

Total runtime mesh source37,224 bytes, zero textures. Platform build compression/actual package delta and GPU performance are unmeasured. Arrow bounds fit old capsule ±.5XZ/±1Y; bolts fit old sphere ±.5XYZ. Tests compare meteor vertices against exact original deformed Rock extrema. Low tier uses the same small primary meshes and original trail time .08 vs .16; no increased trail settings or renderer count for projectiles.

Individual missing/malformed resources use their old primitive/rock fallback; missing trap leaves original marker/charge feedback. Internal Enabled=false allows whole adapter fallback for diagnosis. Subsystem reset destroys cached meshes and resets failed loads; projectile retirement destroys only its original materials, not shared mesh.

## Reproduce

From repository root:

```
blender -b -t 2 --python ArtSource/BlenderProjectiles/build.py
python3 Tests/AuthoredProjectileProductionTests.py /path/to/dotnet
blender -b -t 2 --python ArtSource/BlenderProjectiles/review.py
```

`Factory-Review.png` renders `factory-samples.json` from actual production factory transformed vertices and actual material colors. Each original/new pair shares framing and magnification; projectile columns are enlarged for inspection and +Z travel is marked. Geometry is not separately posed for the review. Surface shading is Blender diffuse, not Unity Standard/Crystal emission. It is explicitly **Blender preview from managed factory output**, not Unity capture. A first visual check caught the vertical contract window reading poorly from above; authored mesh now faces the combat camera without rotating simulation.

## Validation boundary

The dedicated runner compiles actual Friendly/BasicShot/Hostile/Make, CanLaunchFromMuzzle, AlignBodyFlight, BindVisualOrigin and OnDestroy with real resource bytes, decoder, old VisualMeshRecipes and new adapter. Unity/resource APIs, renderer and muzzle bridge are doubles. Source-side snapshot fixes the exact reviewed pre-F5 Projectile.Update hash; existing independent production movement/contact/pause suites exercise that unchanged method. New tests compare authored-enabled/disabled simulation fields and root TRS, target/volley, hostile color, all tier trail settings, cache/material ownership, all six missing/corrupt paths and exact old meteor envelope.

Area tests execute the actual cosmetic Spawn block and age/movement block extracted from CombatArea, not a whole copied damage loop. They cover quadratic fall/release, first-only trap, clipping,12/20/32 lease limits, denial, higher priority eviction and cleanup. The whole area's damage simulation is outside this new fixture and unchanged by this patch. Full guard/passive/40-skill/Boss audit belongs the separate parent inventory work, not six-mesh completion claims.

Raw output is in `validation/`. Unity Editor/player/device rendering, native physics, actual platform package size and frame-time validation remain unavailable here. Managed checks and API compilation cannot close those engine/device acceptance gaps.
