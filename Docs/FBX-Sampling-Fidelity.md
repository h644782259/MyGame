# Vanguard FBX sampling fidelity

This changes only the Vanguard FBX export sampling and its measured evidence. The packed source `.blend`, authored curves, skeleton, geometry, sockets, textures, material, Unity importer and runtime code remain unchanged. The pilot remains default-off.

The previous one-frame FBX bake passed 240 integer-frame comparisons but failed a strict 0.1 mm fractional-frame comparison. The older moving-basic proof covered the actual contact-and-recovery domain; expanding to all five complete authored clips also exposed a larger error in Basic's earlier windup. Neither historical failure is rewritten as a pass.

`ArtSource/BlenderPilot/export_vanguard_fbx.py` retains the existing export options and chooses a fixed bake step for each action: Idle/Move 1 frame, Hit/Skill 0.5 frame, Basic 0.0625 frame. It uses a temporary per-action settings copy, restores the Blender exporter function in `finally`, and rejects unsupported Blender versions, exporter signatures, selection sets and action names. Blender 4.3.2 is the measured version. The authoring builder calls the same export function; standalone re-export reads the existing packed source without saving or changing it.

Re-export into a separate output path:

```sh
blender --background --threads 1 --python ArtSource/BlenderPilot/export_vanguard_fbx.py -- --blend ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend --out /tmp/Vanguard.fbx
```

The validation compares six deformed mesh groups and six sockets against the unchanged source. It includes all 240 integer frames, every 1/16 frame and two off-grid offsets per integer interval across all five clips, Basic's additional 1/32-frame midpoints, and the existing 288 production-policy layered states: 4,923 samples in total. The threshold remains strictly less than `1e-4` metres. Captured coordinates must be finite. This is a discrete sampling test, not a mathematical bound over every real-valued time.

Measured maximum deformed-vertex deviations (millimetres):

| Domain | Previous FBX | Denser export |
| --- | ---: | ---: |
| Idle | 0.001235 | 0.001235 |
| Move | 0.001252 | 0.001252 |
| Basic, complete authored clip | 15.582821 | 0.061645 |
| Hit | 0.151463 | 0.038425 |
| Skill | 0.325087 | 0.082373 |
| 288 production-policy layered samples | 0.671649 | 0.002952 |

The candidate's maximum socket deviation is 0.082228 mm. All sampled candidate vertices and sockets remain below 0.1 mm. The final standalone helper output has exactly equal imported curves (coordinates, handles, interpolation and extrapolation) and static asset data to the densely tested mixed export. Its SHA-256 is `0dac3643c78b839a2d42ceee5b605f6e71f91a6addf4c9631a63c62372e5219c`.

The export costs 1,439,388 bytes versus the former 1,018,460 bytes: +420,928 bytes, about 41.3%. The 900 raw FBX AnimationCurve objects remain; raw KeyTime entries increase from 43,200 to 123,300 (2.854×). Blender reimported F-curve keys separately increase from 47,760 to 136,315; these are different counts. No Unity imported-memory or device performance measurement is implied. Global half-frame and quarter-frame experiments remain recorded as failures because the full Basic action still exceeded the threshold.

The strict passing result, exact candidate hash, per-clip maxima, source/static invariants, raw key counts and rejected experiments are recorded in the accompanying fidelity evidence. `validation-fbx-motion.json` is regenerated for the shipped FBX's integer-frame comparison. Older refinement inventories and PR25 evidence remain historical records of their original hashes.

Unity 6 native import, clip resampling/compression, interpolation, binding, material output and runtime/device behavior remain unverified. The importer is deliberately unchanged. A passing Blender roundtrip does not establish that Unity retains this precision or that gameplay feels better. Native Unity acceptance must compare the imported clips at the same fractional times before enabling the pilot beyond its existing bounded review workflow.
