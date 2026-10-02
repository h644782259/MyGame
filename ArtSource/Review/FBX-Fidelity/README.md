# FBX fractional sampling evidence

Frozen Windows `43039f5f121fadec57b3744cc38a833b80b237db`, iOS `719564e2265c5a79a17300a6f084623c0ab1ce7f`, Android `b1104bbd05e4f7f360ace83ec6d8ccc16ed1b0e1`. Bases are merged PR26 Windows `9ff275279b553695d7e0057f11e9e265dff615ff`, iOS `d78d8516555791673345f6a46623796e7c729b4b` and Android `6ec16d8602e59225ea12987da24c141bf0eb7812`.

Actual Blender 4.3.2 scoped evidence is complete. The frozen full198/API/parity checks are running; no terminal aggregate claim is made at this checkpoint.

Read `Experiment/README.md` for the measured domains, rejected experiments, exact output hash, file/key cost and native Unity boundaries. Original experiment scripts/reports/logs are retained unmodified. `Inputs/` retains the prior production-policy sampled states and original FBX. The unchanged packed source .blend remains in this branch at `ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend`. Scratch scripts retain their actual execution paths; those paths are provenance, not an assertion that they exist on another host. The candidate's standalone exporter accepts explicit input/output paths.

The final helper-output.fbx is the committed candidate byte for byte. It is linked to the 4923-sample strict-tested hardened-mixed artifact by exact imported curve data (including interpolation and handles) and complete compared static invariants. Three nonfinite-coordinate controls and three helper failure controls passed. Baseline/full-step and global half/quarter-step failures remain failures. Tests use discrete Blender reimported samples; Unity clip resampling/compression, rendering, gameplay and device memory/performance remain unverified. Pilot remains default-off.

## Independent portable reproduction

`Portable-Reproduction/reproduce.py` takes explicit source/evidence/output/Blender paths and stages copies; it does not edit the source checkout or original reports. It was actually run against checkpoint `e9521eec075778f60886ba6d67cd7de56b48db06` (41 manifest-listed files), repeating the 4923-point baseline FAIL/candidate PASS, six controls, static and curve equality and raw FBX counts. `actual-run/run-summary.json` and raw command logs record the terminal result. Generated repeated FBX/.blend/PNG files are omitted here; their hashes and scripts are retained.

Re-exported bytes differ from the committed FBX; actual export timestamps differ and are documented, but no claim is made that timestamps are the only byte difference. Imported animation curve data and all compared static fingerprints match the archived candidate exactly. This is semantic reproducibility within the stated Blender checks, not binary determinism or Unity acceptance.
