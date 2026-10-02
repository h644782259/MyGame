# Integration validation handoff

## Frozen first candidate: 06b1750

The registered suite passed **196/196** under default JIT from 2026-10-02 19:02:12.860154 UTC to 19:19:34.825840 UTC. The source-change array is empty. Cached Unity 2021 API compilation passed for Windows, iOS and Android, each using 256 runtime source files. The nine synchronized paths, 516 runtime/meta files and 69 art/pilot asset files match; each mobile platform retains all 24 protected files against both its round baseline and the previous original ledger.

See [the raw handoff](06b1750/final-handoff.json), [aggregate report](06b1750/final-report/report.json), [full stdout](06b1750/full.log) and [parity/protection report](06b1750/parity-protection.json). The aggregate report SHA256 is `799d323c2df0e7051abd63ecd8c28f84ea34d4742a7db90670cd0570ce8b72de`. The adjacent manifest hashes the copied raw files. Paths inside original reports intentionally retain their execution locations.

These passing tests **did not cover** the independently found same-frame facing/admission defect: movement is recorded before a target-facing rotation, so stale smoothed direction can admit unsupported moving basic attacks. That guard defect is fixed and covered by the corrected dcd75ac run below. Independent review also found that the isolated-preview clock assertion observed the last Move sample rather than Idle time; its oracle is corrected in dcd75ac without changing the already-correct production preview clock. Do not infer an overall feature GO from this candidate's passing suite.

Frozen heads: Windows `06b1750cf6936051b2f2d5f21589b23bb1ebc241`, iOS `d8597236519a1cd01caf67274c95e1d0204094ea`, Android `f2fcbf876edc9abeb9216720efcaf700783829ab`. No source head was modified during this validation.

## Corrected frozen candidate: dcd75ac

The fresh registered suite passed **197/197** under default JIT from 2026-10-02 19:37:40.867539 UTC to 19:53:26.844751 UTC. Source changes during the run: `[]`. The added facing-commit production fixture executes accepted walking before current FaceAim and BasicAttack visual commit; the isolated preview oracle now observes the composed Idle pose and rejects a wall-clock mutation.

[Raw handoff](dcd75ac/final-handoff.json), [aggregate report](dcd75ac/final-report/report.json), [full stdout](dcd75ac/full.log), [API report](dcd75ac/platform-api-report.json) and [parity/protection report](dcd75ac/parity-protection.json) are preserved bytewise. Aggregate report SHA256: `f47a0f94ee3d2e12585051cb8531926f901242d4dcca9f5d973e2695491ea9a3`.

Actual cached Unity 2021 API compilation passed for all three source variants, 256 runtime files each. All 13 synchronized paths, 516 runtime/meta files and 69 art/pilot files match. Each mobile variant retains all 24 protected files against both its round baseline and original ledger. Final rehash found no candidate changes; all three tracked worktrees were clean.

Frozen heads: Windows `dcd75ac8a96806788868696f05cffcf4a4d583f4`, iOS `d8533c6cca80a5fdbba3639fc1c9a725b2d99928`, Android `134e617944aee99f16aac29b30e984f97bc57c30`. Runtime manifest SHA256: `f2c717aa5858cc7df8d248a82e3a13627136982146fdf546adf85f50b1e5df52`.

Supplemental checks, outside the registered 197, are archived separately:

- [Render source/input equivalence](Render-Equivalence/Render-Source-Equivalence.md): 71 identical files and 3172 managed production-chain assertions; 288 fixed-facing rendered states match exactly. The initial accumulated-world-position diagnostic failure is retained with its explanation.
- [Single-commit cooldown followup](Facing-Cooldown-Followup/README.md): 90 assertions and two compiled negatives; cooldown values are explicit host inputs, not execution of the full Update.
- [Importer callbacks and preview runbook](Import-Preview-Readiness/Preview-Runbook.md): 59 callback assertions and five compiled negatives; importer/asset APIs are managed doubles, not actual Unity imports.

## Limits

Registered checks are not every repository test script. Managed engine boundaries and cached API compilation do not validate Unity 6 import/native sampling, real scene behavior, native allocations, device gameplay or platform packages. The adjacent Blender reports separately describe actual authoring renders and FBX comparisons, including the retained strict fractional-time failure.
