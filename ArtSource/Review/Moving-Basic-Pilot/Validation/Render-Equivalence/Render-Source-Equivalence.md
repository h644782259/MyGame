# Moving-basic render source equivalence — dcd75ac

The existing straight, fixed-facing Blender reconstruction can be reused as evidence for the corresponding supported inputs of Windows `dcd75ac8a96806788868696f05cffcf4a4d583f4`. This is not a new render, turning demonstration, Unity gameplay recording, or overall feature acceptance.

## Source comparison

Rendered candidate: `49a356eb2df9951fc07f9758f3eea4055b0b9c30`.
Checked candidate: `dcd75ac8a96806788868696f05cffcf4a4d583f4` in `/workspace/emberfall_moving_pilot`.

Git-object byte comparison passed for both `Assets/Scripts/Combat/BlenderPilotVisual.cs` and `Assets/Scripts/Core/BlenderPilotPosePolicy.cs`, and the complete 69-path set under `Assets/Resources/BlenderPilot` (16 files) plus `ArtSource` (53 files). Both file sets and all 71 file contents are identical. `byte-equivalence.json` records each path, byte count, and SHA-256. This includes the source blend, FBX, atlas/material assets, and art-source scripts; it does not assert every repository file is unchanged.

The changed runtime admission adapter records accepted walking in owner-world directions, checks it in the current owner-facing basis after FaceAim, and clears that record on reset. Its zero accepted displacement case is supported so legal stopping retains gait decay. Those changes are outside the identical sampler/asset files and are exercised below.

## Actual production input probe

`probe.py` derives a scratch managed harness from the frozen `Tests/PilotFacingCommitProductionTests.py`. It compiles the actual dcd production adapter, sampler, policy, locomotion state, PlayAction/AnimateHero path, SetLocomotion/ResetLocomotion, and extracted Player.Update accepted-walking/aim tail → FaceAim → BasicAttack visual commit. BasicAttack is deliberately truncated before VFX/damage dispatch. Existing fixture Unity transforms, clip data, resources, input/aim/traversal and other dependencies remain test boundaries; this is not a Unity importer or complete PlayerController execution.

3172 assertions passed:

| Controlled sequence | Ticks at 120 Hz | Raw guard admits | Authored model visible | Locked output states at 24 Hz | Max phase/speed/action-progress/layer-weight error |
| --- | ---: | ---: | ---: | ---: | ---: |
| Forward 6 m/s, fixed forward facing | 720 | 720 | 720 | 144 | 0 |
| Same input, no accepted walking from 1.4 s to 2.4 s, then restart | 720 | 720 | 720 | 144 | 0 |

Both sequences make the same nine actual basic visual commits at 1, 1.4666667, 1.9333334, 2.4, 2.8666668, 3.3333335, 3.8000002, 4.266667 and 4.7333336 seconds. The probe calls the raw facing method at every tick in addition to checking final pilot visibility; it does not infer admission only from the low-speed/nonacting bypass. All four actual `Layers` results (IdleTime, MoveTime, SpeedWeight, UpperWeight) match the locked JSON float inputs exactly. Assertions use 1e-6, while measured maximum error is zero. Results are in `trajectory-equivalence.json` and `probe.log`.

### Controlled origin and retained failed diagnostic

The locked render exporter supplied accepted local walking displacement directly. An initial probe instead accumulated fixture world positions and subtracted them in the actual walking block. At tick 55, its phase was 0.9968145 versus locked 0.99681824 (roughly 3.7e-6), failing the strict layer-input assertion through ordinary single-precision world-coordinate subtraction; admission remained supported up to failure. `accumulated-world-float-diagnostic.log` preserves that failed run; it is not relabeled PASS.

The final probe explicitly resets the controlled owner's world position to zero before each tick, retaining fixed forward rotation and the exact locked accepted delta. This leaves the actual walking → current-facing → visual-commit ordering exercised, while testing the intended locked-input equivalence without claiming identical unrestricted world-position integration. No production code or asset was changed and no tolerance was loosened.

## Reproduce and limits

Run in the existing workspace (scratch dependencies and frozen checkout must remain available):

```sh
python3 /workspace/scratch/render-equivalence-dcd75ac/probe.py
```

The script asserts the exact dcd HEAD before compiling. Dotnet is `/workspace/scratch/dotnet/dotnet`; input JSON paths and SHA-256 values, fixture sources, script hashes and output hashes are in `probe-hashes.json`. The small generated C# harness is retained under `build/`; `run_locked.py` is the generated driver. No evidence worktree, candidate source, or asset was edited.

Reuse is limited to the already-rendered straight, fixed-facing clip and stop/restart diagnostics. It does not prove unsupported 90°/180° turn behavior visually, actual Unity clip import, GPU/device performance, contact/damage/VFX dispatch, or absence of foot sliding. Separate facing tests and aggregate acceptance belong to the integration owner. Existing fractional FBX strict-threshold failure remains unchanged; byte equivalence does not convert it into a pass. No overall feature GO is issued here.
