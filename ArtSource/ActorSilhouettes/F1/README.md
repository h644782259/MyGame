# F1: finite class and companion silhouette closure

Scope is Arcanist, Ranger, Summoner, Spirit companion and Treant companion. No new identity, Animator, collider, damage range, timing or movement system. This independent batch starts at review baseline `0daa0f2`. No third-party inputs, Meshy, purchases, new textures or materials. Blender 4.3.2 sources are original scripted geometry.

## Decisions after inspecting complete constructions

| Identity | Retained after inspection | Targeted authored change |
|---|---|---|
| Arcanist | The existing bent pointed hat, brim, high collar, staff sockets and dynamic TailoredCloth already give a readable mage identity. | Front/embroidered robe strips gain a folded section and pointed/scalloped hem, preserving separate spine attachment and existing equipment logic. |
| Ranger | Existing hood feather, bow wrist/limbs/string, chest wrap, harness and quiver placement remain. Chest-wrap overlap with the far-side bow is not newly diagnosed as a defect. | Open-faced hood shell; hollow, raised quiver lip; feathered spare arrows in the original three sockets. Actual draw pose is executed by production AimArm/BowDraw. |
| Summoner | Existing branch layout, moonstone, leaf mantle, short ritual outfit, wood totem, dynamic class cloak and tier adornments remain distinct from Arcanist. | Curved tapering antler/branch mesh replaces straight capsules. This is a targeted refinement, not a claim that a new whole character or new leaf-cloak simulation was made. |
| Spirit | Existing faceted core, permanent contract band/seal and rank vanes remain. | Open moon crown and paired triple-lobe wings. Both wings now use CompanionRigidParent, so they follow actual preparation/body rotation without soft core scaling. |
| Treant | Existing BarkTorso, shoulder origins/shells, eyes, belt, contract knot and rank branches remain. No shoulder pivot is moved. | Four-lobed faceted foliage per existing crown socket; bark face, tapered bark limbs/hands and root-like feet in the original articulation. |

## Actual integration and rollback

`CombatModel.Part` calls `ActorSilhouetteF1.Apply` after the existing AuthoredActorMeshes loader, retaining the original MeshFilter, Renderer, joint, scale and palette. The F1 dispatch names are exact. Generic Head/Sleeve/Trouser/Glove/Boot keys apply **only** when the existing `treantCompanion` flag is true; other characters, enemies, scenery and weapons do not receive those replacements. The same named robe strips in equipped armor are upgraded when their existing builder creates them. `ApplyEquipment` and `ApplyFashion` keep their existing ownership and visibility behavior.

There are twelve shared cached meshes in `Assets/Resources/ActorSilhouettes/F1`, 1,042 unique triangles and 112,680 source bytes total. The largest module is the hood at 172 triangles, below the 512-per-piece cap. Per-file counts, hashes and local bounds are in `budget.json`. Added textures/materials/renderers/Animators: **zero**. Mobile low quality uses these same small rigid meshes; this is not a device-performance measurement.

Missing/corrupt data and `ActorSilhouetteF1.Enabled=false` keep the previous procedural/authored filter mesh. The Spirit crown/wing dimensions are carried by the mesh data; original primitive construction scales remain unchanged for fallback. Existing GUIDs are unchanged; new files have deterministic GUIDs. The wing's rigid-parent correction is retained in fallback. Meshes are destroyed only by subsystem cache reset, not by individual actor disposal.

## Evidence and boundaries

`Review/*-Before.png` and `Review/*-After.png` share identical Blender camera, light, framing and poses. Each hero image shows base idle, T1 movement and T4 action; companion images show rank-3 permanent idle, preparation and attack/recall. These are **actual factory/TRS geometry rendered in Blender**, not Unity screenshots. Only active hierarchy and enabled MeshRenderers are included. Class and rarity colors execute actual `GameBalance.ClassColor/RarityColor`, replacing the legacy geometry fixture's placeholder palette. The live Unity Standard shader, realtime bowstring LineRenderer, cloth simulation and device rasterization are not reproduced by these mesh renders.

`export.py` executes the real Hero/Companion factories, actual equipment builders, actual AnimateHero/AimArm/caster pose methods, and copied-verbatim companion Animate branches plus actual CompanionAppearance preparation/recall. It tests 44 combinations (3 heroes × base/T1/T4 × 4 poses, plus 2 companions × 4 poses) without changing body mesh identity. Fresh-frame cancellation recovery and non-F1 Vanguard rendering are explicit disabled boundaries. Before uses the inherited Companion construction from `0daa0f2` and F1 disabled; after loads the real F1 decoder/resources.

The final Spirit resource dimensions were moved into local mesh data to preserve primitive fallback scales; `render-equivalence.log` verifies all 44 final transformed geometries remain within 1e-6 of the rendered versions. No screenshot editing is used.

`ActorSilhouetteF1ProductionTests.py` covers all 12 real resources, decoder acceptance and budgets, cache reuse, role isolation, explicit/missing/malformed fallback, visible runtime use of every module, 44 real construction/pose combinations, tree shoulder/head contact and two compiled negative controls (broad generic-role replacement and incorrect soft-core wing parenting). `attachments.log` measures bidirectional shoulder/body vertex intrusions, including an attack pose where shoulder vertices alone would miss the intersection but body vertices lie inside the shoulder. It does not infer a gap from one-sided sampling.

`equipment-regression.log`: existing 7,934 production equipment/fashion assertions and four compiled negative controls passed. `enemy-regression.log`: actual enemy factory knockdown-floor regression and negative control passed. `win-api.log`, `ios-api.log`, `android-api.log`: full runtime source compiled against pinned Unity2021.3 API references, **not Unity6 editor, rendering or device execution**. No Unity Editor is available in this environment.

## Reproduce

```sh
blender -b -t 2 --factory-startup --python ArtSource/ActorSilhouettes/F1/build.py
python3 Tests/ActorSilhouetteF1ProductionTests.py /path/to/dotnet
python3 ArtSource/ActorSilhouettes/F1/export.py /tmp/f1-before /path/to/dotnet --before
python3 ArtSource/ActorSilhouettes/F1/export.py /tmp/f1-after /path/to/dotnet
blender -b -t 2 --factory-startup --python ArtSource/ActorSilhouettes/F1/render.py -- /tmp/f1-before/actors.json /tmp/f1-after/actors.json ArtSource/ActorSilhouettes/F1/Review
python3 ArtSource/ActorSilhouettes/F1/compile_api.py /path/to/dotnet /path/to/pinned/UnityEngine/lib/net45
```

Blender is fixed to two render threads. The editable `ActorSilhouettes-F1.blend` contains all twelve source meshes with stable names; source-only contact placement does not affect runtime export.
