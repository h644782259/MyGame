# Blender rigid actor modules — finite batch 2

**Evidence correction (E review):** the original `Constructions/` images and original construction inventory preserved from the first PR exports include some renderer-disabled legacy parts. They are historical authoring evidence and must not be used to infer actual equipped mesh visibility or precise occlusion. The exporter now filters `Renderer.enabled`; corrected evidence is saved separately under `EnabledReview/`, without overwriting the historic images. These new pictures are still managed-construction Blender renders, not Unity captures.

Original scripted geometry made in Blender 4.3.2, with no Meshy, paid assets, external artwork or textures. This batch replaces selected **rigid mesh pieces**, not complete characters or animation clips. Production uses the existing factories and existing joint transforms, source materials, equipment dimensions and palette. The incomplete Vanguard FBX pilot remains separately default-off.

## Covered construction calls

| Module | Exact production pieces | Coverage |
|---|---|---|
| Cuirass | Breastplate capsule | Vanguard, goblin and guardian base torso |
| ClothTorso | Breastplate, explicitly selected in BuildHero | Arcanist, Ranger, Summoner base torso |
| BarkTorso | Breastplate, explicitly selected in Companion | Treant torso |
| CuirassPlate | Raised breastplate | Vanguard equipped armor across existing tiers |
| Pauldron | Pauldrons, Shoulder shell, Single leather shoulder | Existing shared base shoulders, Vanguard equipment, Ranger |
| ClothShoulder | Tailored shoulder mantle | Arcanist/Ranger equipment |
| Helmet | Helmet, Leather Cap | Vanguard and goblin |
| Boot | Boot, Grounded claw | Existing humanoid boots and large-boss claws |
| WolfHead / WolfTorso / Paw | Wolf head / Spirit wolf torso / Paw | Wolf companion |
| SpiritCore | Star spirit, Spirit Core, Exposed star heart, Arcane Crystal, Focus crystal | Companion, wisp enemy, large boss, base/equipped caster staff |
| Slime | Slime Body | Slime enemy |
| Hammer | Great Hammer | Guardian and guardian boss |
| Totem | Bound spirit totem | Summoner base outfit |
| SwordGuard | Crossguard, Swept guard | Base and equipped sword |
| BowLimb | Bow Limb, Layered bow limb | Base and equipped bow |
| StaffCrown | Staff Crystal Crown, Crystal prong | Base and equipped caster staff |
| BossHousing / BossPlate | Faceted engine housing / Curved shell plate | Large expedition boss chassis and animated core armor |

Shared base parts naturally cover multiple actors. This does not claim a unique whole-character redesign per actor. Existing sword blades, bows' attachment/string endpoints, staff shafts, eye/face feedback, cloth simulation, class costumes, tier/upgrade/fashion insignia, enemy weapons other than guardian hammer, foliage, boss rings/power rails and all other unmapped pieces remain their existing authored/procedural geometry. No textures/materials/renderer count added. No collider, reach, stats, timing, animation events, root motion, navigation or camera rules changed. Actor geometry does not globally replace ProceduralVisuals, so world and runtime hazard meshes are unaffected.

## Runtime and rollback

`AuthoredActorMeshes.Apply` runs after the original mesh constructor; exact name AND primitive type select a module. Missing, truncated, oversized, non-finite, invalid-normal/index payloads leave the original mesh. The versioned `EFM1` binary contains Blender-produced Unity-axis vertices, flat normals, UVs and indices. It avoids engine-dependent FBX hierarchy/axis extraction. Mesh topology is authored offline, not rebuilt from a runtime shape recipe. Cache owns these shared meshes and releases them on subsystem reset; actor/world disposal never owns them.

Set `AuthoredActorMeshes.Enabled=false` before rebuilding actors to use the original geometry. Existing actors must be recreated; the switch does not restore live instances. Startup defaults true. All other existing material and renderer behaviors remain on the same GameObjects, including recoil, fade, costume hide/show and equipment replacement. Stable UUID-derived `.meta` GUIDs are created only if missing.

Every module retains the original primitive origin and full axis-aligned envelope: cube/sphere 1×1×1, capsule/cylinder 1×2×1, before existing local scaling. Thus the existing body/gear/joint dimensions remain authoritative. This preserves bounds, not identical visual surface shape.

## Budgets and evidence

`inventory.json` records all 20 assets' triangles, flat-normal vertices, bounds, SHA-256 and bytes. Total is 2,062 triangles and 222,936 bytes runtime source, 0 new texture bytes and 0 new materials. These are source/mesh counts, **not Unity package size, draw-call timing or device memory measurements**. No separate LOD is added: every module is low-poly, and original material/renderer/feedback budgets remain unchanged.

- `Emberfall-ActorModules.blend`: editable original module scene.
- `ActorModules-Blender.png`: native Blender module sheet.
- `Constructions/*.png`: front three-quarter **Blender renders of actual production factory geometry using managed transform/resource doubles**, four classes in base/equipped configurations, three companions and the large boss. These are NOT Unity screenshots. The companion and boss exports execute their actual construction methods. No old/baseline video was recreated.
- `construction-inventory.json`: each rendered construction's actual loaded module names/counts and geometry hash. Geometry JSON is reproducible intermediate output outside the repository.
- `build.log`, `validation.log`, `constructions.log`, `render-constructions.log`: raw authoring/managed logs.

The construction harness omits gameplay controllers and animation. Its material and TRS API are doubles, and the render deliberately uses neutral gray to inspect silhouettes. It does not validate engine shading, moving joints, camera occlusion, hardware perf or mobile readability. The wolf keeps its existing articulated leg transforms and gait. Its new upper paw caps and torso shoulder/haunch profiles have measured static overlap at all four attachments; `attachment-validation.log` records the exact checks. Moving-joint contact remains unverified. Unity/player import, rendering and Windows/iOS device acceptance remain pending.

## Reproduce

```sh
blender -b --factory-startup --threads 4 --python ArtSource/ActorModules/build_actor_modules.py
python3 Tests/ActorModulesProductionTests.py /path/to/dotnet
python3 ArtSource/ActorModules/export_constructions.py "$PWD" /tmp/actor-constructions /path/to/dotnet
python3 ArtSource/ActorModules/validate_constructions.py /tmp/actor-constructions/geometry.json
blender -b --factory-startup --threads 4 --python ArtSource/ActorModules/render_constructions.py -- /tmp/actor-constructions/geometry.json ArtSource/ActorModules/Constructions
```

Actual loader tests cover malformed data, normal/index/vertex budgets, bounds, exact mapping exclusions, cached reuse and rollback. Existing equipment/fashion composition suites explicitly test the original fallback boundary; they do not silently claim loaded-module coverage. The separate construction exporter executes the real loader. Three-platform API compilation and managed checks are not Unity acceptance.


## Wolf silhouette revision (E01–E06 review follow-up)

The old `Constructions/Companion-0.png` records the earlier block muzzle, rectangular ears and thin lower limbs and is superseded for wolf acceptance. The new `WolfV2/Wolf-pose-0.png`, `Wolf-pose-12.png`, and `Wolf-pose-24.png` show **actual Companion factory vertices** at sampled poses of the unmodified quadruped animation branch, exported through managed TRS and rendered in Blender. These are not Unity captures.

WolfHead now has a longitudinal cheek/skull shape; WolfMuzzle tapers forward from the original bite hinge; WolfEar has a pointed tapered silhouette. WolfTorso separates the broad chest and haunch from the narrow belly. Paw is the thicker foreleg with a distinct ankle/toe; WolfHindLeg carries the larger haunch. WolfTail has a fuller taper. The wolf-only factory scales the legs/ears/tail and seats the eye primitives into the reshaped skull. No joint origin, animation branch, navigation body, damage range, attack timing or gameplay parameter changed. Generic Ear/Muzzle/Tail/Paw names are **not** mapped globally: explicit wolf meshModule keys select them. Other actors' buffers remain byte-identical.

Seven wolf resources: 664 unique triangles / 71,796 bytes. Repeated forelegs, hindlegs and ears yield 908 authored wolf triangles per constructed wolf, plus unchanged procedural eyes. Complete ActorModules inventory: 24 files / 267,480 bytes (44,544 bytes above the preceding batch), zero added textures/materials/renderers. Existing WolfHead/WolfTorso/Paw GUIDs are unchanged. New resources use the same deterministic GUID namespace. Missing/corrupt resource or `AuthoredActorMeshes.Enabled=false` retains procedural geometry at the current cosmetic socket scales.

Reproduce geometry and checks:

```
blender -b --factory-startup --python ArtSource/ActorModules/build_actor_modules.py
python3 ArtSource/ActorModules/export_wolf_motion.py "$PWD" /tmp/emberfall-wolf /path/to/dotnet
python3 ArtSource/ActorModules/validate_constructions.py /tmp/emberfall-wolf/geometry.json
blender -b --factory-startup --python ArtSource/ActorModules/render_wolf.py -- /tmp/emberfall-wolf/wolf-motion.json ArtSource/ActorModules/WolfV2
python3 Tests/ActorModulesProductionTests.py /path/to/dotnet
```

`wolf-attachment-validation.log`: all four leg cap surfaces and both eye surfaces overlap their actual transformed parent body/head. `wolf-motion-validation.log`: 49 sampled actual quadruped branch poses retain limb resources and bite parent, move the jaw, preserve finite vertices and do not move the navigation owner. Hero optional renderer initialization is explicitly disabled in the generic construction exporter; this exporter does not validate Vanguard skinning. These tests cannot establish live Unity collision, exact runtime animation playback, GPU shading or device performance.
