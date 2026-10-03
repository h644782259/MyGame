# Defense evidence output isolation

Baseline: `13ffda9bc711a51a16261dd77bdf55234b21610e`.

The default DefenseIdentity production test previously exported into tracked `ArtSource/DefenseIdentity/Runtime-Samples.json`. The frozen-run worktree was inspected read-only: `side-effect.json` records the before/after hashes and bounds, and `original-side-effect.patch.gz` preserves its raw diff. No historical sample is updated here.

The suite now writes inside its own temporary directory by default. An explicit second positional argument after the SDK selects a persistent JSON destination (its parent directory must exist). FinalBodyEnvelope supplies that argument directly, replacing its fragile literal-path source rewrite. Production sources, assertions and negative controls are unchanged.

Both commands in `after-verification.json` passed; raw logs and compiled negative-control logs are retained here. FinalBodyEnvelope exported 28 actual F6 snapshots and tested 59 combined body/action samples with zero sampled noncoplanar triangle crossings. The actual old-height recompilation and old persistent-motion controls still fail their intended oracles. ArtSource git status is empty after both suites, and the archived JSON is byte-identical to HEAD.

These are targeted managed production-path checks with Unity doubles, not Unity runtime, device, transparency or game-camera acceptance. No aggregate rerun was performed.
