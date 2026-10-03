# Reusable spell surfaces — batch 4

Nine original Blender-authored surface refinements replace the base mesh selection in `FilledSkillVfx.EnsureAssets`. Existing placement, anchored cover clipping, animation curves, motion footprint reservations, shader, priority leases, object counts, reduced/mobile caps, damage, timing and growth remain unchanged. Each missing/malformed buffer individually falls back to its existing `FilledVfxRecipes` shape.

These are bounded refinements to existing identity silhouettes, not nine new skill designs. The Arcane lattice is intentionally retained procedurally because changing its beam tessellation alone did not justify replacement. Danger boundaries, weapon trails, live targeting, dynamic beam endpoints, field scheduling and physics remain runtime systems.

| Shape | Existing → authored triangles | Intended change |
|---|---:|---|
| Crescent | 292 → 196 | Fewer longitudinal segments; preserve closed diamond section and UV hot outer edge |
| Crystal | 12 → 44 | Additional faceted shoulder distinguishes the ice spear; +32 triangles |
| Flame | 216 → 140 | Reduce radial sides; preserve curved flame contour and normalized height UV |
| Sword | 38 → 46 | Blade shoulder taper; +8 triangles |
| Lightning | 96 → 96 | Taper branch endpoints |
| ArcaneShard | 24 → 24 | Taper broken strut end |
| Rupture | 108 → 108 | Taper fault tips |
| Arrow | 60 → 42 | Replace open V head with filled diamond head |
| Vine | 132 → 132 | Taper branches and stems |

All nine runtime buffers total 25,852 bytes, zero textures and the same shared material. Maximum reusable base mesh is 196 triangles (previously Crescent 292); per-effect renderer budgets are unchanged: 14 desktop / 10 mobile / 7 reduced. A particular Crystal or Sword instance has more triangles; this is not a blanket performance claim. `budget-comparison.json` is measured from executing the actual C# old recipe methods and reading new buffers, including per-axis bounds. Source buffers stay within old bounds with floating-point tolerance. Anchored clipping may subdivide faces under its existing independent budget.

UV semantics preserve the shader's identity cues: Crescent outer edge 1 / inner edge 0 / ridges .6, Flame normalized height, Crystal brightest at tip (new shoulder .55), other volumes original projected X + clamped height mapping. The `.blend` contains editable meshes in Blender Z-up; buffers explicitly export Unity Y-up. Strict shared `AuthoredActorMeshes.Decode` owns decoding; FilledSkillVfx cache owns and destroys its decoded meshes. Deterministic metas preserve resource GUIDs.

Run from repository root:

```sh
blender --background --python ArtSource/BlenderSpellBases/build.py
python3 ArtSource/BlenderSpellBases/validate.py
python3 ArtSource/BlenderSpellBases/compare_budgets.py /path/to/dotnet
python3 Tests/AuthoredSpellBasesProductionTests.py /path/to/dotnet
blender --background --python ArtSource/BlenderSpellBases/preview.py
```

Raw logs are committed. The managed loader test executes the actual production adapter and decoder, including real data success and missing/malformed fallback. Legacy allocation suites explicitly stub only the new resource boundary to exercise unchanged procedural fallback; they are not authored-resource success evidence. `Spell-Volumes-Preview.png` is a Blender geometry contact sheet, not Unity rendering or transparency/device acceptance. Engine/device validation remains pending.


## Dedicated Ice / Fire primary follow-up

Two dedicated primary resources improve actual `Impact` composition rather than enlarging every repeated shard or thrust: IcePrimary is a shouldered central spear with four basal facets (68 triangles, 2,236 bytes); FirePrimary contains three split tapered tongues (204 triangles, 5,916 bytes). The total eleven buffers occupy 34,004 bytes. Existing nine base refinements remain as described above. Both dedicated meshes stay inside the previous primary's base bounding envelope.

The existing release still shows contact immediately. Visual-only motion 13 rises from 45% height to full height in .09 seconds, holds until .42 seconds, then contracts to 62%; motion 14 rises from 55% to full height in .07 seconds, holds until .23 seconds, then contracts to 48% height and 76% radial scale. Radial contraction preserves the existing anchored certified rays. No damage callback or new impact flash accompanies these visual phases; original lifetime and impact/area rules remain unchanged. Dedicated-resource failure falls back to the previous mesh and motion.

For these authored Ice/Fire effects only, repeated ornaments shrink to 60% scale, use 55% opacity and fade between .42 and .62 seconds. Existing reservations remain conservative, all tier counts unchanged, and the primary and contact remain first allocated. This keeps identity visible on the reduced tier. Other types and legacy fallback are unaffected.

`Tests/AuthoredSpellIntegrationTests.py` executes the real resources → real shared decoder → real `FilledSkillVfx` → real `AnchoredImpactMesh` / `CombatVisualLease` chain with managed scene/Unity API doubles. It covers six effect kinds, desktop/mobile/reduced tiers, wall/open contexts, sampled animated clipped vertices, individual missing/corrupt fallback, lease saturation, pause, epoch/death/session/end/expiry cleanup, owned-mesh retirement and source-cache reset ownership. Three negative controls disable resource loading, exceed reduced budget, and bypass clipping; each must fail. This is stronger than the old null-resource boundary but still not Unity engine validation.

`Primary-Samples.json` exports actual production mesh/TRS/material-opacity outputs for reduced mobile Ice and Fire at 0/.10/.30/.65 seconds. `Primary-Beats-Preview.png` renders those outputs in Blender; geometry, transform and alpha are production samples, while lighting and material/shader remain Blender approximations. It is not Unity capture. Reproduce the sequence with:

```sh
SPELL_SAMPLE_OUTPUT="$PWD/ArtSource/BlenderSpellBases/Primary-Samples.json" python3 Tests/AuthoredSpellIntegrationTests.py /path/to/dotnet
blender --background --python ArtSource/BlenderSpellBases/preview_primary.py
```
