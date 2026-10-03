# Enabled fixed-scenery integration gap closure

Production snapshot `4de000227408d31febbb1b9b7d13be1275ce20ae`, containing the integrated F series. This supplement changes **tests and evidence only**. No production defect was found in this bounded investigation; no mesh/material/transform/nav/gameplay change is made.

The old 276-check scenery regression intentionally disabled AuthoredFixedScenery and the 136-check F4 suite only required some loaded mesh in each factory. Neither by itself proved every new module and full assembly survived resource fallback or actual occlusion. The new suite directly executes the enabled adapter, real decoder/resource bytes and six actual factories: pillar, portal, quarry town, observatory town, four camp facilities, and all three NPCs/working stations.

## What executes and is asserted

- Independent explicit node-name → resource-key expectations and exact per-factory multiplicities cover all **27 modules**, every repeated roof tile, sleeve, boot, shelf and facility crest. Loaded full vertex/index fingerprints match the expected actual resource. These expectations do not call the adapter's Key to decide what should happen.
- Each module is individually missing and corrupt in **every applicable complete factory**, with every occurrence checked. The failed primitive becomes the original disabled-path geometry/palette/attachment, failed GateFrame becomes the original line, and only a failed optional crest disappears. Other authored modules stay live. Success and failure loads cache once per resource per construction run.
- All rendered parts, including unchanged NPC tools/chart/station props, retain geometry, materials, parent and original local transforms except the documented authored GateFrame scale conversion. Full renderer membership is compared, not only counts of new meshes.
- Production `HubNpcIdle` and `WorldMotion.Start/Update` run three steps and a zero-delta pause after wall-clock advancement. Actual posed transforms agree with the original-art construction. Navigation boxes/circles, physics-component membership and real CameraOcclusionSurface/building-group registration remain identical and stable during motion.
- The repository's actual occlusion class is **CameraOcclusionSurface**, not a class named WorldOccluder. A loaded authored pillar renderer receives a three-slot test binding made from the actual existing stone/band palette references. Its repeated slots share one clone; all distinct slots fade using actual alpha and render-state changes; source alpha is untouched; RestoreAll restores exact references and destroys owned copies. External replacement, disabled/re-enabled registry lifecycle and re-fade execute actual production code.
- That three-slot pillar binding is an explicit API-level stress case: F4 modules ship with one submesh/slot; the test does **not** claim the production pillar normally has three indexed surfaces. Separately, the **unmodified actual workshop building group**, with multiple existing palette materials and new roof/eave/ridge/chimney meshes, fades every upper member together, exposes the real solid footprint, restores every original material reference and hides the footprint on restore.

Final `production.log`: **85,207 assertions across 116 actual factory runs**, plus four compiled behavioral negative controls: wrong NPC sleeve mapping, nav mutation inside enabled mesh application, fading only slot0, and not restoring original slot references. Each control compiles and fails its specific assertion, then the temporary source is restored. The source-link guard line inherited from the harness is explicitly source-only and is not counted as engine execution.

## Boundaries and reproducibility

```sh
python3 Tests/FixedSceneryEnabledIntegrationTests.py /path/to/dotnet
```

Actual `AuthoredFixedScenery`, `CostumeMeshLibrary`, procedural fallback geometry, WorldBuilder/Hubs/Environment/AuthoredScenery, WorldResources, HubSettlementPlan, HubNpcIdle, WorldMotion, BuildingOcclusionGroup, CameraOcclusionSurface and CameraVisibilityRules run together. It does not replace the new adapter with a no-op. Unrelated BlenderPilot/BlenderScenery import calls are **throwing boundaries**; any accidental execution fails. World traversal storage records actual registration requests; pathfinding/native physics are not reimplemented. World text rendering and unrelated root layouts remain explicit boundaries. Unity Object/TRS/Renderer/Material/resource loading are managed doubles with deferred destruction and material-array copying.

This is stronger enabled source integration evidence, **not Unity rendering/importer, GPU blending, native collider behavior, font drawing, realtime performance, package or device validation**. It does not supersede the global integration aggregate, which the parent registers/runs separately. No new screenshot is needed because production geometry is unchanged. `source-inputs.json` fingerprints the production/fixture/resource inputs used for this supplement; existing F4 before/after images and original logs are retained without rewriting them.
