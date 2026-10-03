# Preserve failed chest draws across actual staged loads

Base: `608e65914d3fece96fe8fe886022e53887a758a3`.

Parent review identified an integration gap: the earlier reward suite called `p.Load()` on the same ProgressionService, but `GameSession.ContinueAdventure` uses `SaveSlotTransition.TryStage`, which constructs a replacement service. Instance-local pending draws were lost, allowing an alternate choice after a failed chest write and same-role menu load. The earlier source-only integration review did not establish this boundary; the new production test does.

## Change

`ProgressionService` now retains private, nonserialized draw snapshots per exact save-file path. `SaveSlotTransition.TryStage` carries a copy of that map into the successfully loaded candidate, then restores only the matching role's draw. A→B→A transitions preserve both roles independently; the map is copied so an abandoned candidate cannot clear the original service's context. The stored roll snapshots are privately owned and immutable after registration, and restoring one makes a fresh private receipt copy.

Restoration requires a pending unopened chest, no committed reveal, matching clear count and matching pending tier. A successful chest transaction and explicit NewGame clear that path's entry. Existing saved receipts take precedence and cannot be reopened. Failed storage writes still change neither durable nor live currency/collection. No fields were added to GameProfile and no extra storage write/journal was introduced. Six first-difficulty reward bits, migration and old receipt rules remain unchanged.

The context follows the normal SaveSlotTransition chain and direct in-service slot switches. A wholly new service without that context (including process restart) cannot recover a draw that never reached storage. No promise is made otherwise; no reward was granted on that failed write. This supersedes the earlier Rewards README's statement that leaving a slot cancels its pending draw.

## Reproduction and results

`DOTNET_CLI_HOME=<writable scratch> python3 Tests/RewardRevisionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet`

The existing registered runner now includes `RewardLoadTransitionTests.cs` and production `SafeSaveFlow.cs`, requiring no additional cloud check registration.

-37 actual SaveSlotTransition assertions: same-role replacement, repeated replacement after another failed write, changing RNG, preserved id/amount/rarity/choice, no live events or file/currency/collection mutation during preflight, committed receipt replay, A/B isolation, missing target, candidate-context independence, explicit NewGame and restart boundary.
-1514 reward revision assertions remain green, including old receipt/schema and six first-clear rules.
-41 existing SafeSaveFlow checks,212 chapter transaction,917 chapter presentation,103 adventure,149 save idempotence and480 upgrade checks pass; existing mode reward coverage also executes.
-The precise baseline seam is restored by removing `current.CarryPendingChestContextTo(staged);` from actual production SaveSlotTransition. The test then fails on `same-slot real staged replacement retains failed choice`, with raw exception output retained. Existing actual baseline chest behavior and three reward negative controls remain required and passing.

`reward-load-transition.log` is the final raw run. `pre-save-flow-regression.log` is the earlier passing snapshot before adding the existing41-case save-flow regression. No history logs or frozen validation-v1 files were modified. This is managed production/filesystem validation with existing JSON shims, not Unity or device execution.
