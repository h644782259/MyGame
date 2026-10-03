# E03 — four skill visual families

This batch adds four independent identity silhouettes, not a claim to redesign every skill. All geometry is original and editable in `Four-Skill-Identities.blend`; `build.py` deterministically exports strict Unity-axis buffers with stable GUIDs. It reuses the existing shared material, gameplay calls, cover clipping, tier limits and lifecycle. No textures or external art sources.

| Family | Geometry | Runtime integration |
|---|---|---|
| BladeSlices | Three separated closed blade slices, 372 triangles | Existing `Crescent` release/advanced finale uses one clipped primary instead of two broad crescents. Vanguard pending charge uses this identity. Approved starting whirlwind/groundshock remain their separately implemented presentation. |
| ForkPulse | Central bolt with two side forks and secondary branches, 124 triangles | Lightning `Impact` primary; advanced chain-lightning charge; actual enemy endpoint only after `Health` decreases. The original dynamically calculated lightning line remains. No-target cast does not emit a fake hit endpoint. |
| ContractSigil | Central diamond, mirrored open brackets, terminal diamonds, 100 triangles | Summoner first active release and confirmed enemy contact; existing actual companion contract emergence and mark finisher through `Impact(Summon)`; pending contract charges. |
| ProtectionCage | Four open arches and a crown diamond, 280 triangles | Healing preparation and actual heal ticks. Replaces generic beam decorations after `Heal` executes; final-rank energy restoration continues. No extra healing or damage occurs. |

Runtime buffer total: 25,792 bytes, four shared meshes, zero textures, existing single shared material. Blade primary uses 372 triangles versus previous two authored crescents' 392 and reduces their two renderers to one. Other families use one identity renderer at contact/preparation. Reduced mode retains the same identity primary; no increased 14/10/7 part limits or global lease limits. Source triangle counts exclude runtime cover subdivision, which remains under existing independent caps.

Preparation is owned by the existing cancellable charge/Rune parent. `SkillChargeController` still clears that parent on cancel and after actual release; no cosmetic timer triggers damage. Family motion is visual only: blade contracts after release; contract settles over .08 seconds; fork opacity pulses within its .6-second contact lifetime; protection settles over .1 seconds. Anchored families only contract radially, retaining certified cover rays. Dynamic damage boundaries and beam endpoints remain runtime calculations. Missing identity data returns to the previous charge or impact mesh; missing direct contact identity allows the original caller presentation.

The new contact gates use actual health reduction for lightning and the Summoner first-active enemy feedback. Their initial release visuals are ActionBody priority; confirmed enemy contacts and heal-event cues use RealContact priority. Contract summon-emergence still requires an actual partner. No cooldown, rank, energy, damage, stun, healing amount or scheduled event time was changed.

## Evidence and reproducibility

`Tests/AuthoredSpellIntegrationTests.py` loads actual committed resources through the production decoder, component, clipping and lease code. It now also checks all four identity families in preparation/release/hold/fade, mobile/reduced and near-wall/open conditions, missing-data fallback and source-mesh ownership. The suite includes mutation controls proving resource loading, reduced budgets and clipping are necessary. `Tests/SkillIdentityCallsiteProductionTests.py` executes the actual `Healing` method for all four hero classes through the loaded ProtectionCage pipeline, preserving healing and rank-3 energy continuation, plus source assertions on confirmed enemy endpoints. Existing contract snapshot tests still pass the new optional charge intent argument.

`Runtime-Samples.json` contains actual production geometry, transforms and material alpha on reduced mobile. `Four-Identities-Phases.png` is a visually checked Blender render of those sampled outputs, with independent Blade/Fork/Contract/Protection rows and preparation/release/hold/fade columns. Blender lighting/material shading approximates the production shader; it is **not Unity footage or device acceptance**. No before/after video was produced.

```sh
blender --background --python ArtSource/BlenderSkillIdentities/build.py
python3 ArtSource/BlenderSkillIdentities/validate.py
IDENTITY_SAMPLE_OUTPUT="$PWD/ArtSource/BlenderSkillIdentities/Runtime-Samples.json" python3 Tests/AuthoredSpellIntegrationTests.py /path/to/dotnet
python3 Tests/SkillIdentityCallsiteProductionTests.py /path/to/dotnet
blender --background --python ArtSource/BlenderSkillIdentities/preview.py
```

Raw build, validation, integration, callsite and preview logs accompany the sources. Unity shader appearance, actual depth sorting, allocation/performance on devices and gameplay visual acceptance remain unverified.


### Callsite review follow-up

`SkillIdentityCallsiteProductionTests.py` now additionally executes the complete production `SkillChargeController` and `AdvancedSkillVfx` with real loaded identity meshes, checking Begin, pause, cancel, release, epoch cancellation and component disable for all four families. Every terminal path leaves no active typed effect or lease; release commits once. A mutation removing actual ClearEffect calls must fail.

The suite also extracts and executes the actual `ChainLightning` method and Summoner slot-0 body with an observable `IdentityContact` rendering boundary. Cases cover damaging hit, immunity, actual zero input damage, pre-existing corpse, killing blow and no target. Only actual health reduction routes a RealContact identity; first-active release remains ActionBody. Dynamic lightning still renders its calculated line when there is no confirmed damage, but no fake hit endpoint is recorded. Mutations removing either health-decrease gate must fail. The pre-existing real Healing pipeline checks remain. These are explicit managed/API-boundary tests, not Unity execution. No production behavior changed in this verification-only follow-up.
