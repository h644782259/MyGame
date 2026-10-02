# Production scenery presentation regression coverage

`SceneryPresentationProductionTests.py DOTNET_EXECUTABLE` executes all three named partitions and compiles the shared managed harness once before running the current source. Each mutation then compiles independently and must fail its named behavioral assertion. Aggregate validation should register this **one** driver, not also the three wrappers.

The original standalone script paths remain runnable with the same positional .NET executable argument (or `DOTNET` environment variable):

| Legacy entrypoint | Production partition |
|---|---|
| `EnvironmentPresentationSourceTests.py` | `environment` |
| `TownBuildingGroupSourceTests.py` | `town` |
| `HubSettlementSourceTests.py` | `hub` |

`EMBERFALL_TEST_SOURCE_ROOT` optionally selects the production source checkout. Assertions and fixture remain test-owned. No runtime source is changed by this batch.

## Why the old scripts failed

The old environment script searched for tree color literals in `WorldBuilder.cs`; authored trees now live in `WorldBuilder.AuthoredScenery.cs` and have different bark/leaf colors while retaining Wood/Foliage categories. The town script counted eight direct `Primitive(building.transform,...)` expressions; the actual workshop roof now has seven helper-generated parts. The hub script required six direct `cameraOccluder:true` expressions; complete registration is now supplied by the roof helper and `BuildingOcclusionGroup.Configure`. None of these counts proves final production hierarchy or registration.

## Original coverage retained and strengthened

| Original oracle | Replacement evidence |
|---|---|
| Explicit timber/tree/tent material categories | Execute actual Tree → authored branch tree, ten-plank bridge recipe and procedural Tent fallback. Actual WorldResources cache and ProceduralVisuals.ApplySurface run. Test-only observation records the actual surface argument, distinguishing Foliage from Cloth even when current shader floats match. Renderers must use the intended category; actual shader floats are also checked. RGB values are not golden values. |
| Brook/Courtyard water and wilderness bank links | Three clearly identified source-call guards remain for layout builders outside this harness. They do not claim those complete layouts execute. Actual Water smoothness and bank reed categories execute separately. |
| Lighting-profile factory hook and bounded accents | Actual WorldBuilder.Build calls real ApplyEnvironmentLighting and BuildHubLightPools for hubs0/1/2 and the dungeon branch. Check key/fill/fog policy application, exactly two town accents, none in dungeon, NPC-relative placement and actual unshadowed ForceVertex point-light settings. |
| Portal-focus factory hook and flush appearance | Actual BuildTown → Portal → BuildPortalFocus produces six correctly sized, opaque Metal insets. Direct focus/pool calls must not register traversal/occlusion or physics components. A noncombat damage-dispatch source guard remains for this visual-only helper file. |
| All building parts under identity roots | Execute complete quarry and observatory towns through WorldBuilder.Build. Every building root has the intended parent and identity transform. Actual walls match the plan's center/width, all seven roof-helper parts remain inside the root, and observatory dome/pilasters/spire are retained. Building decorations cannot escape to the town root. |
| Building group configured after all parts | Every actual upper renderer, including late ornaments and the doorway, must own the correct real BuildingOcclusionGroup and be present in the real bounded CameraOcclusionSurface registry. Real Advance must fade the entire group, leave four low footings opaque, show the correct solid-footprint outline and restore all original materials. |
| Solid sizes, low footings, no extra navigation | Actual HubSettlementPlan registration calls are recorded. Every visible wall matches exactly one registered rectangle. Full-town totals admit only authored building/forge-or-dais/NPC solids. Roof and ground cosmetics cannot introduce extra navigation. Existing `HubSettlementGeometryTests` remains the separate production pathfinding/reachability suite. |
| NPC positions and navigation calls | Actual GameSession.HubNpcPosition, BuildHubNpcs and HubSettlementPlan execute; each rendered NPC uses the shared authored position and registers its body and role-prop footprint exactly once. |
| Merchant/smith/exchange props and material categories | Actual NPC factory creates role props and binds both working arms plus the exchange dial. Skin/Cloth/Wood/Metal categories are observed on the corresponding renderer materials. |
| Pause-safe, allocation-free NPC idle | Actual HubNpcIdle.Initialize/Update execute. Pose/age freeze at zero and negative delta; active updates resume without allocating Unity objects. Repeating identical simulation state/timestep under a different wall clock must produce identical poses, including the exchange dial. |
| Cosmetic ground/bank detail | Execute both town-ground recipes and bank decoration; no traversal or registry additions. Bounded road-edge/mosaic/reed output remains present. |

## Controls and boundaries

Compiled negative controls cover lost tree categories, omitted environment profile/portal calls, shadowed accents, cosmetic traversal injection, roof parts parented outside the building, missing/wrong navigation registration, missing group binding/late spire admission, translated building roots, missing NPC navigation/arm binding, mismatched NPC positions, paused wall-clock animation and active wall-clock animation.

Two compiled **behavior-preserving** controls must pass: change the bark RGB value without changing its Wood category; remove redundant roof `cameraOccluder` flags while the real final building group still registers every roof part. These specifically demonstrate that the obsolete source-shape assumptions are gone.

The harness executes production town, environment, authored scenery, material cache/settings, NPC idle and building/camera registry implementations. Unity hierarchy/TRS/renderer-bounds/material/light APIs are managed doubles. Traversal is a call recorder, not an alternate pathfinder. Cosmetic mechanical-ring mesh generation, optional imported-prop loading, unrelated wilderness/dungeon layouts, camp facilities and breakable layout factories are explicit boundaries. The actual bridge recipe is extracted with its unrelated gold road-edge material supplied at the boundary. No Unity rendering, GPU fades, device cost or new reachability claim follows from these tests.
