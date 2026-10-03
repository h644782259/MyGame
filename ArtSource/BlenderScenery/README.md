# Blender scenery batch

Finite coverage: existing supply crate, wayfarer tent and ember campfire exports activated independently of the experimental hero; new hollow ceramic pot, fractured rubble and open-canopy tree integrated at the existing WorldBuilder and DestructiblePropFactory call sites. Procedural roofs, dynamic room layouts, danger markers and water remain under their existing systems.

Original Emberfall geometry authored by this batch's `build_scenery.py` using Blender 4.3.2 primitives and explicit geometry; no downloaded assets, Meshy, purchase, external textures or third-party art. The three reused pilot props retain their existing `ArtSource/BlenderPilot` provenance and editable source; they are not regenerated or copied.

| New export | Triangles | Materials/submeshes | Added textures | FBX bytes |
|---|---:|---:|---:|---:|
| WayfarerPot | 480 | 2 | 0 | 20,940 |
| FracturedRubble | 60 | 1 | 0 | 16,972 |
| OpenCanopyTree | 564 | 2 | 0 | 32,316 |

Total new FBX input: 70,228 bytes; final player package/compression size is unmeasured. All tiers use these bounded opaque meshes (ceiling 1,024 triangles each), without alpha foliage, particles, per-instance textures or high-tier mesh duplicates. Materials use the world's shared material cache and existing visual-surface settings. Each new asset is one mesh renderer with one or two material slots. Reused tent/crate/fire remain 1,588/1,228/842 triangles with the existing shared pilot atlas; no duplicate atlas is added. No claim of measured mobile frame-time improvement.

`Emberfall-Scenery.blend` contains editable meshes plus clearly named review camera/stage; FBX exports are created before review placement. Deterministic asset paths and checked-in .meta GUIDs remain stable on rebuild. FBX axes are -Z forward/Y up, with Blender source in meters/Z up. `validation.json` records bounds and slots from FBX round trips. Rubble embeds about 6 cm into the ground, preventing a floating base; tree base is within 5 mm of ground.

Rebuild from repository root:

```
blender -b -t 4 --python ArtSource/BlenderScenery/build_scenery.py
blender -b -t 2 --python ArtSource/BlenderScenery/validate_exports.py
DOTNET=/path/to/dotnet python3 Tests/BlenderSceneryProductionTests.py
```

The only placement changes are visual children: destructible radius, level, health/recovery initialization, path blocking, destruction ownership and original model Transform remain unchanged. Tree position/size/seed rotation and the original .22 m navigation trunk registration remain authoritative. The whole imported tree is registered as one camera-occlusion hierarchy. Campfire retains original light parameters and stone navigation circles; its existing folded flame mesh receives only a small procedural pulsing inner core. The authored ring is aesthetically different from old individual rocks, while legacy circle coordinates/radii remain authoritative. No damage range, timing or growth values are edited.

`BlenderSceneryArt.Enabled=false` before world creation restores procedural paths. Missing model, missing/unsupported atlas, missing geometry, unexpected new-material names or colliders in model resources fail closed to the old art. The hero pilot flag is not modified. Existing loaded worlds are not rebuilt automatically by toggling the flag.

Evidence: `Scenery-New-Preview.png` was visually inspected as Blender-only studio preview of the new assets, not a Unity capture. `build.log`, `validate-exports.log`, `validation.json` and `budget.json` retain the Blender export evidence. Managed logs cover actual loader success/failure against API doubles, plus retained fallback geometry (51 assertions), real managed occlusion registry (17), and scene presentation (276) with mutation controls. These are not Unity importer, GPU, engine play-mode, Windows/iOS/Android device or performance acceptance. Unity material remapping, imported scale/orientation and in-game composition still require engine review; procedural fallback remains available.
