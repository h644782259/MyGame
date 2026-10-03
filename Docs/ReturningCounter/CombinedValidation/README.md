# Planning and authored-art fixture compatibility

Tested combined source head `0288463` in the isolated `planning-art-fixture-compat` worktree, including all six planning branches and F1–F6. No production or test source edits were needed: `Tests/EquipmentCompositionPilotBoundary.cs` already provides the explicit optional-pilot `SetBlenderPilotVisible` boundary used by these generated fixtures. Real F1/F2/F3, factory, action and recovery calls in the integrated suite remain intact.

Each command used:

```
DOTNET_CLI_HOME=/tmp/planning-art-compat-cli python3 Tests/<name>.py /workspace/shared/emberfall-tools/dotnet/dotnet
```

All six commands exited 0; unedited stdout/stderr is in the corresponding log.

| Suite | Result |
|---|---|
| IntegratedActorArtProductionTests | 281 integrated assertions, five compiled cross-layer controls, treant contact inspection |
| ActorSilhouetteF1ProductionTests | 12 real resources, 44 actual factory/equipment/pose combinations, role and attachment negative controls |
| WeaponContactProductionTests | 85 factory assertions, two compiled controls, 1,680 actual pose/anchor comparisons |
| EnemySilhouetteAnimationTests | 215,960 actual factory/Animate assertions across five identities, four phases and F2 enabled/disabled |
| ReturningCounterProductionTests | 23 actual basic/melee/dodge/ground assertions, three compiled controls, blink registration source contract |
| ReturningCounterPersistenceTests | 14 variant persistence checks plus 2,204 existing progression-growth assertions |

These are managed production-source checks with explicit engine substitutes, not Unity execution or device acceptance. No full aggregate, RestrictedHealing test, rendering or platform build was run in this follow-up. No push or merge performed.
