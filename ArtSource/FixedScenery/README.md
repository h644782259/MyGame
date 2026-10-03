# F4 fixed scenery — bounded six-group delivery

27 original Blender 4.3.2 modules, authored by the project build script. No purchased/generated external models, textures, rigs, or materials. Runtime data total **322,596 bytes**; highest individual mesh **384 triangles**; all assets single-submesh, zero textures. `budget.json` records exact vertices, triangles, bounds, hashes and source bytes. The editable `.blend` is about 602 KiB. Low quality uses the same bounded meshes and existing palette; no extra material instances are created by the loader.

Coverage:

1. Pillar: stepped chamfered base, fluted taper, capital, collar, band. Original five objects, sizes, 1.4 navigation box and two occlusion marks remain.
2. Portal: segmented cut-stone fixed frame and tiered plinth. Jade moving inner light, crystals, motes, focus and interaction remain original runtime builders.
3. Workshop: ten raised tile courses, shaped eaves and ridge, recessed chimney cap. Original seven roof pieces remain on original BuildingOcclusionGroup. Observatory dome/spire already have a useful silhouette and are retained.
4. Town facilities: inset shelves, waisted anvil and stump, lectern, concentric observatory dais. Original forge gantry and live astronomical crown retained. NPC station furniture uses the same modules, no duplicate artwork.
5. Four facilities: one shared pedestal and four small identifying fixed crests beneath original animated progression crystals. Original progress, palette, names and navigation unchanged.
6. Three NPC roles: shared faceted tunic, sleeve, boots and head, merchant brim/crown and smith apron. Existing arm transforms, idle components, hammer and chart animation unchanged. No face or skeleton replacement.

Production wiring is confined to three calls in WorldBuilder: primitive mesh substitution by exact existing name; Gate frame mesh with original line fallback; optional fixed crest. Meshes are cached and destroyed on subsystem reset. `Enabled=false`, missing data or rejected decoding retains original meshes; missing crests are simply absent. Existing transforms, material ownership and registry markings remain authoritative. Stable checked-in GUIDs are preserved; `guids.json` restores missing resource metas. No navigation, collision, damage, timing or interaction fields are changed.

## Rebuild and evidence

From repository root:

```
blender -b -t 2 --python ArtSource/FixedScenery/build.py
python3 Tests/FixedSceneryProductionTests.py /path/to/dotnet
blender -b -t 2 --python ArtSource/FixedScenery/render.py
python3 Tests/SceneryPresentationProductionTests.py /path/to/dotnet
```

`production.log`: 136 checks executing actual decoder on every resource and actual factory recipes for all six groups; full enabled/disabled navigation and occlusion membership equality, decoder truncation/header rejection, successful mesh attachment and no new collider. Managed Unity API boundaries, **not Unity engine acceptance**. `legacy-scene.log`: existing 276 production scene assertions plus compiled negative controls. Its explicit new loader boundary preserves the scope of existing procedural-fallback checks; the new suite owns successful-resource coverage.

`factory-0..5.png`: Blender renders of world-space geometry and palette exported from actual production factory execution with managed Unity boundaries, at a fixed camera direction. Not Unity screenshots, GPU/shader validation, or gameplay acceptance. Labels are a declared text boundary. Existing costume chart and astronomical crown geometry now execute the real CostumeMeshLibrary and CostumeRecipes, alongside actual changed resource bytes. Portal runtime line geometry is included; no animation is claimed. Workshop image crops the full actual town to one actual building; observatory image selects central dais/crown; full factory data is retained in compressed JSON. NPC and station images show the actual shared three-role assembly.

| Full executed factory | Renderers | Distinct material instances | Authored triangles instantiated |
|---|---:|---:|---:|
| Pillar | 5 | 2 | 636 |
| Portal | 38 | 3 | 620 |
| Quarry town incl. three NPCs | 241 | 23 | 5620 |
| Observatory town incl. three NPCs | 230 | 24 | 2928 |
| Four facility plinths/crests/crystals | 16 | 5 | 888 |
| Three NPCs and working stations | 56 | 8 | 2072 |

Counts include retained renderers/materials; new loader allocates **zero** materials. Each changed module still uses one palette material; full-town 23/24 counts are the existing aggregate palette, not a module budget. Full town totals are larger than the selected review screenshot. Instanced triangles are deliberately distinguished from unique resource budget. `factory-snapshots.json.gz` is review data, outside Resources; not shipped gameplay content. Source `.blend`, script, budget, GUID manifest and raw logs are retained. Unity Windows/iOS rendering, low-device performance and on-device occlusion fade remain parent acceptance tasks; no engine test is asserted here.
