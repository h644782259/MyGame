# Published planning + art: intermediate targeted check

Initial combined source head: `3f30a9a77199152a03fb87342b5cc01e98fb11ba` (published PR33 companion priority, PR34 hard/heroic star sweep, PR36 death earnings/retry over newest main and art). Fixture-only correction/retest head: `aa8f9e6601d18f7bf2eaaa147248eae355fa6d9a`.

**Final: seven targeted suites and three pinned API builds passed.** This is an intermediate combined check, not the full aggregate/final A/B/C freeze. Pending-loot followup `325544e` is absent and untested here. These are managed production-source checks and pinned Unity 2021.3 API compilations, not Unity 6 Editor, rendering or device validation.

| Scope | Entry | Result |
|---|---|---|
| PR33 companion intent/priority | CompanionIntentProductionTests.py | Pass, existing compiled negative controls included |
| PR34 chapter/star sweep policy | ChapterCombatProductionTests.py | Pass after fixture dependency correction; star length/speed/followup and other compiled negatives retained |
| Sweep capsule/cover safety | BossSweepCapsuleProductionTests.py | 231 checks + old center-ray negative passed |
| PR36 chapter host persistence/retry | ChapterHostProductionTests.py | 460 checks + 12 compiled negatives passed |
| Chapter result/retry UI | ChapterEntryProductionTests.py | 344 replay checks + existing negatives passed |
| Chapter return/time scale | ChapterReturnTimeScaleTests.py | Pass |
| Actual combined journey | IntegratedJourneyTests.py | 3,684 assertions, 8 class journeys, 72 chapter attempts + 6 negatives passed |
| Full runtime source APIs | win-api.log, ios-api.log, android-api.log | Restore/build all exit 0 |

Every raw log identifies its source head and command. `report.json` records timestamps, exit codes, runtime-source hashes and pinned-reference hashes. API builds and the other six suites ran on the initial combined source. The subsequent three-line change affects only `Tests/ChapterCombatProductionTests.py`; all runtime source hashes were verified unchanged, so the successful API builds were not needlessly repeated. Only ChapterCombat was rerun after that fix.

Two initial test launches failed before compilation because their SDK inherited a read-only `/home/agent/.dotnet` location. The `.initial-environment-failure.log` files preserve those failures. Retrying just those tests with a writable temporary `DOTNET_CLI_HOME` passed capsule safety and exposed the separate ChapterCombat fixture error. `ChapterCombatProductionTests.initial-fixture-failure.log` preserves that compiler failure: the actual boss now calls `EnemySilhouetteArt.ApplyAnchors`, but this combat-rule fixture did not declare the art dependency. This was an existing integration gap, not evidence that PR34 overwrote a previous correction.

The fix declares an explicit no-op anchor **art-only** boundary inside the generated rule fixture. It changes no production source and preserves all PR34 rule assertions and negative controls. This suite does not validate new meshes; the separate actual F2 and combined actor-art suites own that coverage.

Reproduce the complete targeted set (at a chosen checkout head):

```sh
python3 Docs/Validation/Published-planning-art-targeted/run.py /path/to/dotnet /path/to/pinned/UnityEngine/lib/net45
```

`run.log` and `retry.log` retain the earlier failed passes rather than hiding them. `final-summary.log`, `report.json` and the final per-suite logs record the final outcome. `retry_environment.py` is the retained historical recovery command and checks the initial source head before reuse.
