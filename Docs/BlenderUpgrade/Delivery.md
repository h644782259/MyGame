# Frozen PR30–32 art integration

For the current cumulative E01–E06/wolf follow-up, see `Extended-Delivery.md` and the latest `ResourceBudget.json`. The table below is explicitly the separate PR30–32 frozen snapshot.

The initial PR30–32 integration inventory covers: selected rigid pieces across all four heroes, sword/bow/staff and equipment, all three companions, existing enemy families and boss construction; six scenery families; Vanguard opening skills, nine reusable spell volumes and two dedicated Ice/Fire primary meshes. It is an actual source integration, not just a Blender proposal. It does not replace entire characters, all fashions, animation clips or procedural gameplay systems.

| Review batch | Runtime integration | New source resource bytes |
|---|---|---:|
| 1: skills + actors | 20 rigid modules on existing factory joints; whirlwind/ground-shock at existing release; shared strict mesh decoder | 234,560 |
| 2: scenery | Existing tent/crate/campfire reused; new pot/rubble/open canopy at existing factories | 70,228 |
| 3: reusable spells | 9 base refinements plus Ice/Fire primary meshes and visual-only phases; individual fallback | 34,004 |
| Total new runtime resource input | No new textures; original shaders/material systems retained | 338,792 (38 files) |

These are file bytes, not a Unity player package or resident-memory measurement. Actor library: 2,062 triangles across 20 modules, no added renderers/materials. New scenery: 480/60/564 triangles with 2/1/2 material slots. Opening skill: 12 desktop / 8 mobile / 5 reduced parts, max 1,022/68 source triangles for whirlwind/shock. Reusable spell caps remain 14/10/7; some ice/sword shapes add triangles while crescent/flame/arrow reduce them. Detailed per-asset tables, bounds and SHA-256 are in the four ArtSource directories.

## What changed visually, and what did not

- Vanguard opening skills: three closed curved blade volumes with separate narrow hot rims, immediate ground contact plus delayed crest/fault/chip wake. The new 1.15-second tail changes visual rhythm only; it never schedules damage. The existing FilledSpell material/shader remains; Blender emission/studio appearance is not shader parity.
- Actor modules: stepped/tapered armor chest and shoulder profiles, separate cloth/bark torso profiles, shaped guard/bow limb/staff crown, wolf torso/head/paw surfaces, faceted spirit core, slime and boss housing/plates. Existing joints, paint/material palette, cloth layers, faces, many limb shapes and animation remain. Enemy changes mostly reuse these shared body pieces; this is not a separately sculpted redesign of each enemy or every gear/fashion variant. Neutral factory renders show geometry, not final colored game appearance.
- Scenery: a hollow pot with neck/lip, fractured low-poly rubble and thicker overlapping asymmetric canopy lobes are new. Tent/crate/campfire are previously authored models now enabled at actual call sites, not newly refined artwork. World materials reuse the existing cache/palette; the existing pilot atlas is unchanged.
- Reusable spells: crescent/flame tessellation reduction, ice shoulder, sword taper, filled arrowhead, tapered bolt/fault/vine/strut ends. These keep their existing silhouette families and runtime animation. Most are offline export/surface refinements, not bespoke new skill effects. Arcane and all dynamic danger/target geometry remain unchanged.

There is no claim that every project asset has received final art polish, that replacing basic mesh inputs completes all art, or that the existing character designs/animations are artistically accepted. E01–E06 and wolf silhouette follow-up remain separate ongoing work, not included in this frozen runtime snapshot. A distinct Unity/device and visual-quality acceptance boundary remains.

## Evidence and reproduction

- `ArtSource/ActorModules/README.md`: editable blend, generator, actual production factory/resource/TRS construction exporter, 12 new constructed stills, static attachment checks, strict decoder/fallback tests. Final wolf head/legs were adjusted within original bounds after visual review found poor attachment contact.
- `ArtSource/BlenderVfx/README.md`: editable skill meshes, generator, new still, source contracts, actual component/decoder/lease checks (584,457 assertions + four mutation controls).
- `ArtSource/BlenderScenery/README.md`: editable blend, generator, new still, FBX roundtrip, actual loader and retained scenery/occlusion/presentation checks.
- `ArtSource/BlenderSpellBases/README.md`: editable blend, generator, new still, actual old/new recipe triangle/bounds measurements, UV semantic checks, real per-buffer fallback tests. Crescent UV hot-edge loss found in review was corrected before freeze.
- `Docs/BlenderUpgrade/Validation/`: the earlier 205-check report is historical and does not validate the PR31/32 corrections. `Validation/PR32-Frozen/` contains the isolated corrected-stack report: 207 passing checks, no source changes, three define compilations and exact runtime/input hashes.

No baseline effects were recreated and no comparison videos were made, following the user's updated instruction. The original reference MP4 was inspected only to establish the intended skill direction.

## Preserved boundaries and deliberate exclusions

All existing 306 asset GUIDs are preserved. New paths use checked-in stable GUIDs. Shared decoded meshes have explicit cache/reset ownership; imported scenery meshes are never added to World's destruction ownership. Missing or malformed resources preserve existing geometry. Actor/scenery switches restore old art at construction time; they do not rebuild existing live objects. The incomplete original animated Vanguard FBX pilot remains default-off.

No damage range, timing, cooldown, rank, growth, collision, physics, reward, or traversal number changes. Trees remain subject to the existing occlusion hierarchy. Skill motion is inside certified visual footprints; near cover opening skills retain old presentation. Ground shock retains the existing immediate ring/ribbon; its new tail is decorative, not a delayed hit. Dynamic threat boundaries, target outlines, live beam/ribbon endpoints, clipping, water and seeded room topology remain runtime-generated.

Existing detailed fashion attachments, cloth layers, blade structure, procedural architecture/roof modules, and the Arcane lattice are retained where this pass found no concrete silhouette or budget advantage from repackaging them in Blender. Later explicitly requested E01–E06 and wolf improvements are a separate bounded batch.

## Platforms, review and unverified acceptance

Windows and iOS receive byte-identical changed shared files while retaining platform-specific project settings, exporters, README and native build files. Three review branches stack in order: `codex/blender-b1-actors-vfx` -> `codex/blender-b2-scenery` -> `codex/blender-b3-spell-volumes`. Draft PRs are for parent review only; no merge was performed. Merge/rebase review should process them in order, retargeting downstream PRs to main after the previous batch lands.

Android remote Git and connected GitHub reads return repository not found/404; provided Android main could not be independently checked. No repository was created and no login, credential or permission changes were attempted. Shared changes are retained as an Android handoff archive/patch with source hashes; applying them against Android's actual branch remains blocked by access.

Blender renders and FBX roundtrips are authoring evidence. Managed tests use engine doubles; pinned Unity 2021.3.33 API compilation checks C# only. The current environment has no Unity Editor; the official pinned Unity 6000.6 archive download returned proxy403. Unity6 import, material/animation/shader behavior, PlayMode, real JsonUtility, GPU cost, actual game captures and Windows/iOS/Android device builds remain unverified. No engine or release acceptance is claimed.

## Construction image interpretation discovered during review

The original `ArtSource/ActorModules/Constructions` exporter filtered active objects but did not exclude `Renderer.enabled=false`. Thus those gray construction renders can include equipment-hidden parts. They are factory-construction evidence, not accurate rendered-visibility evidence; do not infer an in-game ranger chest/bow occlusion defect from them. A corrected enabled-renderer export is being supplied in the separate E follow-up. This does not change the frozen PR32 runtime code or test inputs.
