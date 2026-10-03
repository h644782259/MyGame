# Final combined body/protection and weapon-pose closure

Base: `d4dc7ae`. This check closes the isolated-loader gap in the earlier F6 body-clearance report and F3 static weapon matrix. The inherited pure-art 227-pass snapshot predates this correction; it does **not** validate the changed production dimensions. Parent integration owns the final full freeze and platform validation.

## Actual defect and constrained correction

The original maximum-height/shoulder-slab comparison mixed vertices from different heights. It missed actual protection-strut intersections with helmet crests, pointed hats, feathers and summoner crown branches. `before-production.log`, `initial-slab-measurement.log`, `intersections.log` and compressed `before-*` samples preserve those failures. A negative projected slab margin alone is not treated as proof of intersection.

The only production change is in `FilledSkillVfx.Charge` for `protectionEnvelope && identity == 4`: persistent height floor 2.2 → 2.6; width remains 2.6. A dedicated static motion 20 keeps that body envelope at full dimensions from birth, instead of growing from 65% height through the head in the first 0.18 seconds. Existing opacity, lease, owner/state cancellation and pooling behavior remain. Ordinary Charge motion 18 and IdentityContact motion 19 retain their previous dimensions and motion. No gameplay radius, damage, timing, budget, asset, mesh/GUID, material count or texture changed. The source mesh remains editable in its existing Blender source; no replacement source asset was needed.

Offline dimension screening selected width 2.6/height 2.6, followed by fresh execution of the actual changed production code. Offline scaled vertices are never used as final evidence. Search logs include interruption when the accepted minimal parameter choice was reached.

## Production sampling and checks

`Tests/FinalBodyEnvelopeProductionTests.py <dotnet> [output]` prepares the existing integrated actual factory project with real ActorModules, F1, F3/WeaponArt, VanguardActionLibrary/VanguardArt, caster/AimArm and full Recovery methods. It does not use optional new-layer stubs. The older independent whole-body BlenderPilot import stays disabled, as documented by the integrated actor fixture.

The same rigs execute 59 samples: four classes × base/T4 plus highest existing fashion × seven production basic-action curve positions, plus T4 Arcanist/Summoner skill phase .52 and Ranger skill draw phase .28. The seven basic curve positions include pre-contact diagnostic points; gameplay basic attacks still start at their existing contact point. No delayed gameplay attack is introduced. The Ranger skill sample uses the actual visible arrow, bowstring and drawing pose; no Blender-authored replacement pose is used. Loaded blade endpoints, bow grip and string/nock contacts are tested, with fixed body/equipment mesh identities.

Actual F6 guard/passive blocks execute through AdvancedSkillVfx → FilledSkillVfx → real ProtectionCage bytes. Three guards plus four defensive passives are sampled at birth, .09, .18 and .75 seconds: 28 actual state/time samples. Identical triangle geometry is hashed and reused for efficient checking, while all state/time labels and MPB alpha samples remain recorded.

All 59 body/action samples have zero detected noncoplanar triangle/triangle crossings against the actual protection geometry at all 28 samples. The test checks both triangles' finite edges, with analytic crossing/disjoint controls. Minimum same-height head/shoulder lateral profile margin is **0.278953**, and minimum crown height margin is **0.749492**. `clearance.json` records full included-body bounds and per-case included/excluded names. These are sampled numerical checks, not a continuous-time collision proof; exact coplanar/fully contained solids and Unity rasterization are outside this triangle-crossing oracle.

External weapons/casting implements, held shields and fashion wings are excluded **only from protection/body intersection and size criteria**. Protection describes the owner's body, not the weapon's reach, shield silhouette or decorative wing span; using those as size inputs would make the envelope vary with equipment and attack reach. They remain loaded, visible, rendered and weapon-contact-tested. Body clothing, head ornaments, shoulder armor and their articulated geometry remain included.

Negative controls reject disabled F1, skipped F3, skipped Vanguard art, missing defense identity, lost body-envelope selection, stale state ownership, the old shrinking persistent motion, and the old height 2.2. The old-height control compiles actual Filled code, re-exports its meshes, then fails the actual body/effect triangle test; it is not a synthetic shrunken image. Raw negative traces are retained.

Additional targeted regressions: `f1-regression.log` covers 12 real resources/44 factory-pose cases and existing negatives; `f3-contact-regression.log` covers actual weapon matrix plus 1,680 action/contact anchor checks and ribbon sampling. The new combined test is the cross-layer evidence; the older isolated suites are supplementary regressions. No full aggregate was run here.

## Image truth

`Combined-Protection-Actions.png` is one representative sheet with eight actual base/T4 constructions and a same-camera old/new-height comparison of the same caster sample. All samples use one fixed approximately 27-degree observation angle; this is **not acceptance of the game's camera readability**. Gallery placement translates whole samples only. Geometry, poses and contacts are unchanged for presentation.

The final renderer samples actual MPB `_Color.a * _Opacity` and writes it to **Principled BSDF Alpha**, not just the Base Color fourth component. The rendered .75-second guard alpha is 0.35; `render.log` records/asserts BSDF alpha values [0.35, 1]. The chosen guard sample uses the fixture's supplied gold tint. Actor palettes are actual GameBalance class/rarity colors. Unity's full UV-grain/dissolve shader and lighting are not reproduced.

`Raw-Front-Review.png` preserves the first frontal review where front struts obscured faces/weapon details. That initial diagnostic render omitted the MPB tint alpha and showed opaque struts; it is **not** a valid opacity or gameplay-camera preview. It is retained rather than hidden by the final camera adjustment. The final representative sheet corrects the alpha handling and explicitly labels NOT Unity. No video or image editing was used.

## Reproduce

Requires the existing .NET SDK, installed NumPy (2.3.5 here), and Blender 4.3.2.

```sh
python3 Tests/FinalBodyEnvelopeProductionTests.py /path/to/dotnet /tmp/final-body-envelope
blender -b -t 2 --factory-startup --python ArtSource/FinalBodyEnvelope/render.py -- /tmp/final-body-envelope/actors.json /tmp/final-body-envelope/protection.json ArtSource/FinalBodyEnvelope/Combined-Protection-Actions.png /tmp/final-body-envelope/old-height-protection.json
```

Renderer also accepts retained `.json.gz` files. It uses two render threads and 16 samples. `source-manifest.json` records this batch's production/test/export sources and retained sample hashes. The parent will provide the final all-source/tests/tools manifest. No Unity Editor, device, shader or GPU performance validation is claimed.
