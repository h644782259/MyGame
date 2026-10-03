# E05 tactical body fittings

Original Emberfall assets made with Blender 4.3.2; no external model, image, font or paid service. Rebuild with `blender -b --factory-startup --python ArtSource/TacticalAttachments/build.py`. The blend remains editable; mesh objects retain their names. Review torso spheres, lights and camera are source-only.

SupplierBackpack: twin faceted energy reservoirs, rear brace and over-shoulder feeds (112 triangles). HuntBadge: forward split-blade badge with a target stone (56 triangles). SupportMantle: separate lateral protective shoulder fins (64 triangles). Total 232 triangles, 25,092 runtime bytes; three shared opaque Standard materials, zero textures. Each live enemy adds at most three MeshRenderers; low quality retains the same compact identity geometry, with no particles, transparency or extra lights. Do not count source blend/PNG/log files as runtime package cost.

`TacticalEnemyVisual.Refresh` reads the existing supplier/hunt/support predicates; only identity presentation is replaced. Contested ground marking is unchanged. Per-body mesh bounds and parent transforms adapt fittings to slime, wisp, humanoid, guardian and boss scaling/animation. Rear/center-front/lateral sockets keep simultaneous states separate. Each loaded fitting suppresses its old line icon. Missing/corrupt resources, unsupported shader, unknown body and the explicit Enabled rollback retain the old individual marker. Loss of support removes the fitting immediately; the original 0.24-second severed line tail remains only in fallback mode. No support range, damage multiplier, control duration or other gameplay value changes.

Shared mesh/material cache is destroyed only at subsystem reset; enemy lifecycle destroys owned fitting objects. Refresh makes no mesh/material/GameObject allocations. Disable, death, epoch mismatch and replacement player hide all fittings. No renderer lists are cached in the owning model by this adapter.

Fittings-front.png / Fittings-back.png / Fittings-combined.png are Blender construction stills, personally inspected, not Unity screenshots or engine acceptance. The normalized review sphere demonstrates sockets; it is not a substitute for Unity per-rig acceptance. Actual managed chain is covered by TacticalAttachmentsProductionTests.py (TacticalEnemyVisual + TacticalAttachmentArt + AuthoredActorMeshes decoder + committed binary buffers). Engine substitutes cannot establish Standard shader build inclusion, GPU appearance, device FPS, Unity batching or clipping in real animation. Those remain Unity/device acceptance boundaries.


## PR35 actual-enemy assembly review

The original sphere diagrams above inspect only standalone authoring and are insufficient to accept enemy integration. `EnemyAssemblies/{Slime,Wisp,Goblin,Guardian,GuardianBoss}.png` now show the real `CombatModel.Enemy` factory, real `TacticalAttachmentArt.Create/Set` adapter and decoded committed resource vertices in identity / supplier / hunt / supported / all-three states. `Combined-back.png` shows the rear equipment on all five body sizes. Construction palette colors and transformed geometry come from production; lighting, shadows and rasterization are Blender. These are **not Unity captures**. Some all-three combinations are deliberate presentation stress cases, not claims that every combination currently occurs in gameplay.

The guardian badge now sits in front of the actual chest plate and raised ember crystal, measured by converting outer-shell bounds into the animated body's local frame. It retains the body parent and original transform scale, so ordinary/large guardian scaling remains inherited. Slime and wisp use a smaller lower-front seal to preserve their eye/face identity. Humanoid support fins are raised and spread over the existing shoulder armour so their identity is not buried in the pauldrons. The supplier socket is unchanged. Each active role still owns one renderer; resource, triangle, material and texture budgets are unchanged.

The HuntBadge first blade is rotated before joining. Export previously forgot that surviving object rotation, producing coordinates inconsistent with the Blender authoring preview. The generator now applies rotation/scale after joining and exports world-space positions plus inverse-transpose normals. A numeric regression checks decoded first-blade center `(-.14,.03,.60)` and collar center `(0,.27,.59)` within 1e-6. All `.meta` GUIDs are unchanged.

Reproduce the new production chain and pixels:

```
python3 Tests/TacticalEnemyAssemblyTests.py /path/to/dotnet
python3 ArtSource/TacticalAttachments/export_enemies.py "$PWD" /tmp/e05-enemies /path/to/dotnet
blender -b --factory-startup --python ArtSource/TacticalAttachments/render_enemies.py -- /tmp/e05-enemies/enemies.json ArtSource/TacticalAttachments/EnemyAssemblies
```

`enemy-validation.log` preserves the raw numeric/40-composition checks and compiled torso-only badge / buried support-fin socket negative controls. Assertions test every one of eight masks for five actual bodies; active renderer count exactly matches the requested mask, both guardian scales clear actual armour/crystal vertices, small body badges stay below real eye vertices, humanoid support fins extend above real shoulder armour vertices, and hide disables every fitting. The earlier `TacticalAttachmentsProductionTests.py` remains the real live-state/component lifecycle test and is rerun in `test.log`; this new static construction test complements it rather than substituting a test-only tactical state model for that existing chain. `enemy-render.log` is the unedited Blender render log. Runtime standard-shader appearance, occlusion and per-device cost remain Unity/device acceptance boundaries.
