# Vanguard moving-basic evidence

This is native Blender source reconstruction, **not Unity gameplay footage**. Runtime candidate `49a356eb2df9951fc07f9758f3eea4055b0b9c30` composes existing clips; no source blend, FBX, texture, socket, attack timing, root-motion or gameplay asset is changed by this evidence package. Baseline `6afcb19b3c293f521279c3290d25920ad750f116` has the same tree as frozen `d1e1606`.

## Current acceptance boundary

The same-frame facing admission defect found in the first candidate is fixed in frozen Windows `dcd75ac8a96806788868696f05cffcf4a4d583f4` (iOS `d8533c6c`, Android `134e617`). The corrected registered suite passed **197/197**, with no source changes during the run, and all three cached API builds/parity/protection checks passed. [Raw reports and supplemental tests](Validation/README.md) preserve both the historical 196-check result and the new run without conflating their coverage.

The videos still depict the original49a straight, fixed-facing reconstruction. [Source/input equivalence](Validation/Render-Equivalence/Render-Source-Equivalence.md) verifies identical sampler/assets and exact locked trajectory/layer inputs for dcd; reuse does not cover turns or Unity execution. The strict fractional FBX threshold below remains **FAIL**. Source review and managed checks are not overall visual/device acceptance.

Open [Review.html](Review.html) locally for an offline video/still review surface. [The existing Unity preview inspection route](Validation/Import-Preview-Readiness/Preview-Runbook.md) describes base-kit/session setup and Full/Weapon/Back inspection; actual Editor captures remain pending.

## Review

Use `Review/Comparison-combat.mp4` first. Its Chinese captions use the full system NotoSansCJK TTC index2, not the project subset font. Each panel retains its native800×600 resolution; only a64px caption bar is added outside the panels. No zoom, retiming or interpolated frames. The two separate `*-combat-native.mp4` files contain untouched native frames. Close480×360 videos are supplementary motion diagnostics. `Review/clean-contact/Comparison-contact64.png` independently renders contact24 with64samples for a clearer static comparison; it does not replace or upgrade the4sample video.

Vanguard baseMoveSpeed is6f in `ProgressionService`; `PlayerController` submits accepted walking to `SetLocomotion` before `BasicAttack`/`PlayAction`, and basic attacks do not add a movement stop. The controlled6s sequence has accepted forward displacement6m/s and nine basic commits, with actual120Hz managed production update state exported at24fps. Production `PlayAction` starts directly at contact.52 and uses the actual.46s cooldown, quantized to the simulation tick. There is no invented windup. More than two leg cycles are present (approximately14.9 in the full sequence). All three equipped starter items are Common,level1,upgrade0,mechanicNone: 初行长剑、初行战衣、初行护符; no fashion. The optional pilot is explicitly enabled for this comparison and remains default-off in the game.

Baseline uses original native `Pilot_Move` when eligible, and actual production procedural starter geometry/`AnimateHero`/`LocomotionPoseState`/recovery/cloth methods while moving basic forces fallback. Unity hierarchy/math APIs are managed doubles; fixtureRandom.value=.5 makes procedural breathing phase reproducible (3.14 radians from.5×6.28). Blender reconstructs mesh normals. Candidate uses unchanged native clips and the **actual frozen production `Layers` helper outputs**, with corresponding local position/scale Lerp and quaternion Slerp. All19 bones share an Idle/Move base using existing smoothedSpeed; Root and9 lower bones preserve that sampled base. Basic upper9 retain weight1 through.75, then use1−SmoothStep to reach the current base by1.0. Sword is rigidly skinned Hand.R; six source anchors follow that hand (runtime consumes five).

In reviewed consecutive reconstructed frames, the candidate retains the same authored silhouette while leg phase continues through contact/recovery/recommit. Frames35→36 intentionally jump the upper body directly back to contact; this is not a claim of fully continuous upper motion or elimination of all snapping. AtSpeed0, stationary-basic lower bones now retain sampledIdle base rather than the old full-bodyBasic lower identity; this is a presentation change, not a timing change.

No Unity importer, native `SampleAnimation`, rendering, device performance, enemies, damage, collisions, VFX or sound are validated here. No IK is added: external6m/s translation and fixed stride amplitude do **not** establish planted feet or removal of foot sliding. Source dynamic observations must not be described as engine playtesting or an overall art-quality approval.

Primary camera matches AdventureCamera defaults: verticalFOV48°,distance19×1.2041595,pitch48.36646°,yaw0°,targetheight.7. Close camera is orthographic4.7 atoffset(5,−8,4.2),target(0,0,1.1). Both follow the same externally translated owner at a fixed relative offset; AdventureCamera follow smoothing is not executed. No Root bone movement is invented. Identical neutral gray diffuse material(.42,.44,.47),roughness.68,metal/specular0 and directional lights2.5/.75/1.6 are used. Cycles CPU1, noOIDN/adaptive sampling, zero path bounces;8samples close/4combat. Low-sample alias/noise remains visible and is not a geometry or motion improvement claim.

Review consecutive frame indices23–25 for first contact,28–30 for gait wrap,30–35 for late recovery,36 for next contact,123–126 for final recovery/return. `review-frame-indices.json` records exact times/phases. Seven optional stop/restart stills use actual `LocomotionPoseState.Advance`: accepted displacement stops1.4–2.4s, phase pauses andSpeed decays, then movement resumes. Independent attack timing is the same actual exported `PlayAction` schedule, not a hand-selectedSpeed test.

## Existing material and preview inspection

[Four primary source-material stills](Review/Material-Inspection/README.md) compare the unchanged native Blender shader against neutral gray using identical cameras/lights. Existing packed textures equal the shipped PNG bytes; UV mapping and Unity material saved values are recorded. These show existing source geometry/material separation, not a new art redesign, Unity Standard output, runtime preview framing or overall visual-quality GO. The offline review surface includes the Full/Weapon/Back source plates and contact pair.

## FBX result: keep the sampling domains separate

* Existing **integer240-frame** roundtrip remains PASS: max deformed vertex1.20543e−6m; max socket9.62943e−7m. All26 Basic integer frames include13/25=.52.
* New **fractional-time strict1e−4m** layered check is **FAIL**, retained as such. Across288 forward/stop samples, layered maxSword0.000671649m andTip0.000671749m occur atframe30,t1.2500001,phase.7808693.
* Matched unlayered controls locate an existing Basic interpolation difference: maxSword0.000816774m/Tip0.000816668m atframe57,t2.3750002,phase.9808692. Move/Idle remain near1µm. Layered max bone-head error isHand.R0.000343167m atframe58; unlayered Basic0.000343132m.
* Largest per-time layered excess above max(Basic,Move,Idle) error is0.00000045725m(0.457µm), stop/restart frame49. This is a bounded comparison of existing fractional interpolation, **not a reclassification of the strict failed check as PASS**. Source Basic has sparse keys while exportedFBX bakes integer frames; original integer evidence cannot be extrapolated to subframes.


Fractional288-sample distributions, in **micrometers** (per-sample maximum over all six mesh groups / six sockets; linear-interpolated percentiles):

| Recipe | Vertex p50 | Vertex p95 | Vertex max | Socket p50 | Socket p95 | Socket max |
|---|---:|---:|---:|---:|---:|---:|
| Idle | 0.586 | 0.978 | 0.990 | 0.380 | 0.548 | 0.555 |
| Move | 0.713 | 0.985 | 1.129 | 0.477 | 0.756 | 1.001 |
| Basic | 312.110 | 757.489 | 816.774 | 311.987 | 757.322 | 816.668 |
| layered | 123.289 | 589.123 | 671.649 | 123.107 | 588.892 | 671.749 |

`Review/error-peaks/` includes matched native source and reimportedFBX stills for layeredpeak30, original unlayeredBasic control57 and incremental stop49. Their manifest records exact floating-point input phases. They are pose/error diagnostics with the owner recentered, not another gameplay or main-video before/after. In particular, the old Basic control is **not** the main video's procedural-fallback baseline. The diagnostic report explicitly separates `diagnosticCompleted:true` from `strictLayeredPass:false`.

## Reproduce

Use separate checkouts for baseline and candidate, Blender4.3.2, .NET8, ffmpeg. Scripts accept repository/output paths and never write runtime assets. Use a fresh output directory; render scripts resume by filename, so do not reuse outputs after changing source/configuration. Run renders serially, one CPU thread. Example from this folder:

```bash
BASE=/path/to/baseline-6afcb19
CAND=/path/to/runtime-49a356e
OUT=/path/to/empty-output
DOTNET=/path/to/dotnet
python3 Scripts/export_motion.py "$BASE" "$OUT" "$DOTNET"
blender -b --python-exit-code 1 --threads 1 --python Scripts/inspect_rig.py -- "$BASE" "$OUT"
python3 Scripts/export_policy.py "$BASE" "$CAND" "$OUT" "$DOTNET"
blender -b --python-exit-code 1 --threads 1 --python Scripts/validate_integer_fbx.py -- "$BASE" "$OUT"
blender -b --python-exit-code 1 --threads 1 --python Scripts/diagnose_fractional_details.py -- "$BASE" "$OUT"
# Expected nonzero exit: retain the stricter subframe failure; do not suppress it as a pass.
blender -b --python-exit-code 1 --threads 1 --python Scripts/validate_layered_fbx.py -- "$BASE" "$OUT"
# Continue separately after reviewing that expected diagnostic failure.
blender -b --python-exit-code 1 --threads 1 --python Scripts/render.py -- "$BASE" "$OUT"
blender -b --python-exit-code 1 --threads 1 --python Scripts/render.py -- "$BASE" "$OUT" --combat
blender -b --python-exit-code 1 --threads 1 --python Scripts/render_candidate.py -- "$BASE" "$OUT"
blender -b --python-exit-code 1 --threads 1 --python Scripts/render_candidate.py -- "$BASE" "$OUT" --combat
blender -b --python-exit-code 1 --threads 1 --python Scripts/render_candidate.py -- "$BASE" "$OUT" --stop --only 33,36,42,48,57,58,66
blender -b --python-exit-code 1 --threads 1 --python Scripts/render_error_peaks.py -- "$BASE" "$OUT"
python3 Scripts/encode.py "$OUT"
python3 Scripts/render_clean_contact.py "$BASE" "$OUT" /path/to/separate-clean-contact
```

The large per-frame geometryJSON and fullPNG sequences are intentionally generated outside the repository. Compact state/recipe JSON, hashes, native MP4s, selected stills and reports are retained. Production motion and both candidate input exports were regenerated using these parameterized scripts in a separate scratch folder and matched the original SHA256 values exactly. Two parameterized native contact renders also reproduced every RGB pixel exactly; PNG metadata Date/RenderTime differs, so file hashes appropriately differ.

Provenance correction: preliminary no-equipment output was rejected. An interrupted initial Blender child also survived shell interruption; it was explicitly terminated, all possibly affected frames quarantined, and final baseline frames regenerated serially from the locked three-starter motion. Those rejected outputs are absent from this package. `clean-stills-manifest.json` and final artifact hashes identify accepted evidence.
