# Blender art pilot — bounded source candidate

This is an opt-in prototype, not an engine/device acceptance claim. The procedural presentation remains the default. No Meshy or third-party AI/service/assets were used. Original geometry, atlas and motion were authored by repository scripts in Blender 4.3.2.

## Assets and sources

`ArtSource/BlenderPilot` contains editable packed `.blend` projects, deterministic authoring scripts and measured reports. `Assets/Resources/BlenderPilot` contains runtime FBX, two shared 1024×1024 atlases, one Built-in Standard material and Unity metadata. No render cache, `.blend1`, native authoring files or duplicated texture copies belong under `Assets`.

Geometry is in metre units: Vanguard helmet top 2.25 and crest 2.535, matching the existing game fixture. Body, Clothes, Armor, Head and Back are explicit mesh groups on one 19-bone rig. Armor pieces are rigidly weighted; tabard and mantle hinges blend two weights. Five clips: Idle, Move, Basic, Hit, Skill. No root motion or animation events. Sword is a separate independently exported model and also a separate rigid skinned mesh in the hero; anchors Grip/Guard/BladeRoot/Tip/Pommel/Emission are authored.

UVs deliberately use padded face islands within palette tiles; shared overlap is intentional. Base colour uses sRGB. MetallicSmoothness uses linear R metallic/A smoothness, with cloth kept rough. Faceted imported normals plus calculated Mikk tangents are used; no normal-map texture is required or claimed. Texture compression/platform quality and first Unity import remain acceptance items.

## Integration and limits

In Unity use **Emberfall → Art Pilot → Enable for Play (base outfit only)** before creating a character. The option is Editor-session-only and never touches player saves. Disable selects the procedural path; new editor/player startup defaults off. Animation module is explicitly declared. Windows and iOS Editor versions are not changed.

`CombatModel.Hero` remains the game/preview factory. The imported unit-scale visual child owns its skeleton. Automatic Animator/Animation execution is disabled; the adapter alone samples imported legacy clips. Procedural joints are separate and skipped while the imported visual is shown. Unsupported states restore the original renderers rather than drive the imported bones. Collection previews use their isolated `previewTime` and existing factory; no combat dispatch occurs.

The base pilot supports idle, forward locomotion, basic attack, hit recovery and representative earth-rupture skill 7. Any equipment beyond the exact unupgraded common level-1 starter kit, any fashion, strafe/backstep, jump/landing, charge, death, moving basic/skill attacks, and other skills use the existing procedural presentation. Matching starter items retain their gameplay stats while using the authored base outfit. Other equipment preview candidates retain the accurate original gear/fashion representation. These fallbacks can visibly switch silhouettes; the pilot is not a production complete animation set.

Basic hit still commits synchronously through the existing gameplay path. `PlayAction` starts at existing phase 0.52; manual animation samples that exact phase in the same commit call. It never starts a new clip at zero, adds windup, defers damage, changes cadence or alters collision reach. Ribbon sockets come from the visible imported skeleton. Its single authored basic swing uses a fixed matching ornament direction; procedural swings retain their alternating direction. Tier-0 sword local distances remain grip 0, guard .16, root .20, tip 1.30, pommel -.17; authored presentation orientation may differ.

Camp tent and campfire replace only decorative presentation at their original positions (outside the wilderness walking circle). No collider or traversal obstacle is added. The existing camp light remains. The decorative coffer remains distinct from the breakable crate. The new crate is only under its original `Intact prop` child: break/dispose/blocking radius/rewards remain the existing host's responsibility. Imported shared materials/meshes are not added to `WorldResources` destruction ownership.

## Validation boundary

Native Blender renders and FBX import/export checks are evidence of authoring output, not Unity frame appearance, device performance, combat feel, or mobile readability. Review final measured JSON and test logs. Unity 6 import, legacy clip names/bindings, material import, socket alignment in engine, touch/device camera scale, batching/memory and complete gameplay transition testing are pending. A real old/new Unity screenshot comparison is pending; do not substitute a reconstructed reference and call it an engine capture.

Rebuild: `blender -b --factory-startup --threads 4 --python ArtSource/BlenderPilot/build_pilot.py`, then `build_vanguard.py`, then `validate_and_preview.py`. The default render output is `/workspace/scratch/blender-pilot-preview`; override `PILOT_PREVIEW` for authoring render location. Build validation output is intentionally separated from runtime assets.

Validation detail: FBX roundtrip checks topology, UV presence, bone/action counts and finite vertices. The 240-frame deformation and grip-error scan samples the editable Blender source rig, not a roundtripped FBX rig; Unity deformation remains unverified.

Additional roundtrip evidence: `validate_fbx_motion.py` reloads the exported Vanguard.fbx, samples all five imported actions (240 frames), and compares all six deformed mesh groups and six moving sockets against the authored source at matched times. Symmetric vertex-distance maximum is 1.21e-6 m and socket maximum 9.63e-7 m. This now validates the reimported Blender skeleton and skin, while Unity clip bindings, material import and scale remain pending. See `validation-fbx-motion.json` with the exact FBX hash.

Packaging cost: even with the runtime pilot disabled by default, Resources assets remain packaged. Two 1024-square RGBA textures with full mip chains represent approximately 10.67 MiB before platform compression; runtime source files total about 1.897 MB. These are inventory measurements, not Unity build size, resident GPU memory or device performance.

Integrated review regression: 97 actual adapter/commit/death/respawn/equipment/moving-attack assertions, 55 resource-readiness checks and 14 compiled negative controls pass together. These include missing material/texture/shader/mesh and empty/unbound clips. Readiness sampling restores transform state before the first visible frame. A source-only independent review found no remaining blocker in the loader or extended finale-tail paths; this is not visual acceptance.
