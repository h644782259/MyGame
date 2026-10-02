# Integration validation handoff

## Frozen first candidate: 06b1750

The registered suite passed **196/196** under default JIT from 2026-10-02 19:02:12.860154 UTC to 19:19:34.825840 UTC. The source-change array is empty. Cached Unity 2021 API compilation passed for Windows, iOS and Android, each using 256 runtime source files. The nine synchronized paths, 516 runtime/meta files and 69 art/pilot asset files match; each mobile platform retains all 24 protected files against both its round baseline and the previous original ledger.

See [the raw handoff](06b1750/final-handoff.json), [aggregate report](06b1750/final-report/report.json), [full stdout](06b1750/full.log) and [parity/protection report](06b1750/parity-protection.json). The aggregate report SHA256 is `799d323c2df0e7051abd63ecd8c28f84ea34d4742a7db90670cd0570ce8b72de`. The adjacent manifest hashes the copied raw files. Paths inside original reports intentionally retain their execution locations.

These passing tests **did not cover** the independently found same-frame facing/admission defect: movement is recorded before a target-facing rotation, so stale smoothed direction can admit unsupported moving basic attacks. A separate guard fix and integration test are in progress. Independent review also found that the isolated-preview clock assertion observed the last Move sample rather than Idle time; its oracle will be corrected without changing the already-correct production preview clock. Do not infer an overall feature GO from this candidate's passing suite.

Frozen heads: Windows `06b1750cf6936051b2f2d5f21589b23bb1ebc241`, iOS `d8597236519a1cd01caf67274c95e1d0204094ea`, Android `f2fcbf876edc9abeb9216720efcaf700783829ab`. No source head was modified during this validation.

## Limits

Registered checks are not every repository test script. Managed engine boundaries and cached API compilation do not validate Unity 6 import/native sampling, real scene behavior, native allocations, device gameplay or platform packages. The adjacent Blender reports separately describe actual authoring renders and FBX comparisons, including the retained strict fractional-time failure.
