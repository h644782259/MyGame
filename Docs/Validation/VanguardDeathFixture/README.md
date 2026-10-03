# Vanguard death integration fixture repair

Base: `b32543fc4867fec16f931ac44f5d571359d9f189`. The first unchanged targeted execution is preserved in `before.log` with EXIT=1: generated DeathSessionProbe lacked PreserveWorldLoot and Progress.LastError required by the actual extracted OnPlayerDied method.

Only Tests/VanguardRecoveryIntegrationTests.py changed. Persistence in this presentation/recovery test is an explicit configurable boundary: successful preservation permits one Save, failed preservation exposes its error and permits no Save. Actual OnPlayerDied and UpdateTimeScale are still extracted unchanged from GameSession.cs. Existing real factory, authored bytes, action cancellation, equipment, counter thrust and paused terminal death-pose assertions remain. Four added assertions verify successful save count, paused death on persistence failure, error propagation without Save, and idempotent repeated death callback. Actual file/receipt durability belongs to DeathLootPersistenceProductionTests and is not claimed by this boundary.

`after.log`: EXIT=0; 62 integration assertions and all four original compiled negative controls passed (skip authored pose, skip recovery, skip counter thrust, old scaled-time death). Existing managed fixture unused-field warnings remain visible.

Command before and after:

```sh
DOTNET_CLI_HOME=/tmp/vanguard-death-fixture-cli DOTNET=/workspace/shared/emberfall-tools/dotnet/dotnet python3 Tests/VanguardRecoveryIntegrationTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
```

Read-only shared-fixture audit: ChapterHostProductionTests uses ChapterHostFixture's existing PreserveWorldLoot boundary and real ProgressionService.LastError; IntegratedJourneyTests and ChapterReturnTimeScaleTests reuse that same fixture and real service; DeathLootPersistenceProductionTests extracts the actual PreserveWorldLoot. No same missing dependency was found in those adapters. They were not rerun here.

No production or registry changes; no new full aggregate. This is managed numerical TRS and lifecycle validation, not Unity engine execution. Parent retains the first frozen aggregate and performs the next freeze.
