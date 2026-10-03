# F3 weapons: source integration, bounded tier/fashion geometry

The original `ArtSource/BlenderPilot/Emberfall-Pilot-Props.blend` contains the joined **StarcoreSword**. `build.py` reads that exact editable source, splits its connected blade / swept guard / wrapped grip / pommel components, normalizes their mesh frames and exports four rigid modules. Source SHA and all six original source sockets are retained in `budget.json`. The original `.blend`, FBX, atlas and GUIDs are unchanged; the default-off FBX is not counted as live integration.

Actual default path: `CombatModel.Hero` → `ApplyWeaponArt(null)` and `ApplyEquipment` → `ApplyWeaponArt(weapon)`. A null/base weapon or the exact unupgraded common level-1 初行长剑 uses all four source pieces. They reuse existing renderers, original wrist hierarchy and palette. Missing/malformed any one piece gives the complete old sword. Other gear retains its original tier blade and guard; this does not enable the incomplete imported character. `WeaponModules.Enabled=false` plus the normal refresh restores saved original meshes/scales. Shared new meshes are cache-owned, original owned meshes retain their original owner, and repeated equipment changes do not own/destroy shared resources.

Blade length remains `WeaponStructure.SwordTip - SwordRoot`; tip/root are not inferred from FBX bounds. Original public sword anchors (pommel, grip, guard, root, tip) and ribbon root/tip attachment remain unchanged. The source's sixth `Anchor_Emission` is retained in provenance only: the current public runtime enum has five sword anchors and the ribbon consumes root/tip; no new emission gameplay/attachment API is invented. Bow string, arrow nock, draw timing, caster wrist/core/prong identity and both caster classes are retained. Held-arrow mesh simplification is only inside Bow rig; flying projectiles are untouched.

The remaining four modules are a bowed six-sided joint, a shared six-sided shaft/ornament profile, an eight-triangle faceted gem, and a 72-triangle mechanical hoop. Inspection found old small cubes cost 300 triangles, small spheres 384, and each existing weapon fashion hoop 576. Those small details exceeded the requested 800-triangle set budget. Their existing names, part counts, placement, tier/upgrade rules and material palette are retained; lower tessellation replaces the shapes. T4 sword's original 14-triangle blade and 76-triangle guard are kept. Highest mechanical weapon ornaments keep the same ring identity; back wings and fashion catalog are not changed.

## Measured budget

Eight modules, 8–140 triangles each (cap 512), **51,504 runtime source bytes**, zero added texture/material/renderer. Unity-built/compressed player package delta is unmeasured without the Editor. T4 cases include upgrade rank 10. Total visible weapon meshes, including held arrow and weapon fashion, excluding back wings:

| Class | T1 | T1 + highest fashion | T4 | T4 + highest fashion |
|---|---:|---:|---:|---:|
| Swordguard | 364 | 508 | 414 | 558 |
| Arcanist | 116 | 332 | 364 | 580 |
| Ranger | 406 | 550 | 574 | 718 |
| Summoner | 116 | 332 | 364 | 580 |

`instance-budgets.json` contains all 32 before/after measured cases. Before highest T4 sets were 8,982 / 8,636 / 10,166 / 8,636 triangles respectively. These are actual factory mesh-index totals, not Unity profiler measurements. No damage, range, collision, progression or timing code is edited.

## Evidence and reproducibility

- `Factory16-Before/After.png`: 4 classes × T1/T4 × none/highest existing fashion, visible renderers only. `WeaponClose-*` isolates all 16 weapon sets; `WeaponDetail-*` gives larger readable starter/T4 sword, bow and staff views. Cameras match within each pair. Full boards export mesh renderers; the detail pair additionally draws the actual LineRenderer bow path/width as a Blender curve. This curve is review-only.
- Actual production `ClassColors`, `ClassColor` and `RarityColor` are extracted into the managed factory harness; images do not use the older fixture's substitute colors. Lighting and pixels are Blender, not Unity.
- `production.log`: 85 actual factory, resource decoder/adapter, 16-case bounds/anchors/budget/fallback checks and two compiled controls (wrong blade tip, skipped source sword).
- `contact.log`: same real resources plus full production AnimateHero/AimArm, caster pose code and real ribbon Sample. 1,680 before/after contact-anchor checks across 16 combinations and seven phases; actual bow string/nock draw agreement. Fresh pose sampling uses no prior recovery history; PR35 owns recovery/cancellation integration.
- `legacy-equipment.log` and `legacy-fashion.log`: 7,934 equipment and 128 common/rare structure assertions with existing compiled negative controls. These deliberately disable the optional new visual adapter; they validate fallback construction, not new geometry.
- `Validation/*-compile.log`: pinned Unity 2021.3.33 API compilation for Windows/iOS/Android defines. This is not Unity 6 execution, importer/device acceptance, FPS or Frame Debugger validation.

```
blender -b -t 2 --python ArtSource/WeaponModules/build.py
# Or export hand-edited canonical meshes, retaining meta GUIDs:
blender -b -t 2 ArtSource/WeaponModules/WeaponModules.blend --python ArtSource/WeaponModules/export_blend.py
python3 Tests/WeaponModulesProductionTests.py /path/to/dotnet /tmp/f3-review
python3 Tests/WeaponContactProductionTests.py /path/to/dotnet
blender -b -t 2 --python ArtSource/WeaponModules/render.py -- /tmp/f3-review/geometry.json
blender -b -t 2 --python ArtSource/WeaponModules/render_details.py -- /tmp/f3-review/geometry.json
```

Worktree: `codex/blender-f3-weapons`; remote main read-only fetched as `d347bdf5dd3778d88c6dd9519fdb86ee0c70f4c9`. Work remains based on the delegated frozen PR35 checkout. No remote merge or push performed. Pixel inspection confirmed the starter's wrapped grip/swept guard, retained T4 fangs and forging marks, faceted bow joints, caster core/crown and ring fashion identities. Unity/device visual acceptance remains open.
