# Final combined planning + art targeted check

Checked candidate: `aa2bd006a79d178ae2c5786ba2114c5674ce6c16`, with all planning and art production source combined. No production changes were needed. Only RestrictedHealingProductionTests' fixture construction changed: the shared mobile shell already declares the F6 Beam presentation boundary, so the healing adapter now asserts that exactly one exists instead of adding a duplicate. The first compile failure is preserved in RestrictedHealing-before-repair.log.

All five requested suites passed against these combined sources:

- RestrictedHealingProductionTests: 640 actual cast/sequence/Heal/potion/companion assertions, seven compiled negative controls. The actual healing, charge, cooldown, eligibility, refresh and cancellation behavior remains under test.
- CampBuildDraftProductionTests: 259 actual service and desktop/mobile draft UI assertions; 136 combined respec/build preset assertions; original non-refill post-commit source guard.
- DeathLootPersistenceProductionTests: 184 receipt/retry assertions, 27 actual death/pending/Respawn/service-write assertions; old direct death-save negative control.
- MobilePinnedTargetProductionTests: 56 actual pointer/aim/targeting/charge assertions and all original compiled negative controls.
- DefenseIdentityProductionTests: actual three guard blocks × three ranks, four passive triggers, actual ProtectionCage resource/geometry, pause/state-end/death/epoch, lease release/reuse and missing-resource fallback; three compiled negative controls.

Commands: `python3 Tests/<suite>.py /workspace/shared/emberfall-tools/dotnet/dotnet`, with isolated DOTNET_CLI_HOME; logs adjacent. Source hashes of the combined production tree are recorded in source-manifest.json. Expected negative-control exception traces and failed-save diagnostics remain in the raw logs.

No full aggregate or platform compilation was launched here. These are managed source/fixture tests; no Unity frame loop, shaders, physics, device delivery or player build is claimed. Parent owns the final frozen aggregate and publication.
