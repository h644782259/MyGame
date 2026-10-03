# Practice isolation and lifecycle correction

Base: `608e659`; independent worktree, no gameplay damage/radius/timing changes.

- Entry refuses living nearby enemies (12 metres), any active aggro/preparing enemy, or any active projectile before deactivating roots. The explicit notice asks the player to leave combat/wait. This conservative policy prevents opening practice from cancelling a real warning, windup or combo. Snapshot errors also refuse entry before ownership changes.
- Practice snapshots use a private memory-only constructor: no directory, save path or slot ID. Low-level profile writing rejects an absent destination, and practice cannot create a nested persistent draft, stage a save-slot transition or open an inherited chest. The chest guard executes before receipt restoration (including the newer integration implementation).
- SaveBeforeLeaving ends practice and restores original ownership before normal settlement/save. Actual CanQuitSafely still rejects failed saves and retries safely. Mobile background callbacks keep pause semantics and deduplicate saves; OnDestroy restores ownership before releasing the session.
- Original profile/player references and HP/energy/cooldowns remain intact. Actual loot collection is rejected during practice and original pickup/registry survive restoration. Existing root suspension/restoration is retained; companions are not recreated or refreshed. Companion animation rendering is not independently engine-validated here.

## Evidence

- `service.log`: 32 assertions against real ProgressionService, SafeSaveFlow, draft transactions and measured events; 7 compiled mutation controls. Includes null low-level write target, nested draft, slot transition and inherited chest boundaries.
- `session.log`: 61 assertions with actual GameSession.Practice, Player.Initialize, EnemyController.OnDisable/CancelAttack/ClearWarning, quit/pause/focus/destroy/save callbacks and UpdateTimeScale. SetActive invokes actual extracted lifecycle methods; a positive cancellation control proves the old exploit pathway is represented. Four compiled mutation controls reject unsafe entry, original OS-close veto, lost original player and lost random restoration. Actual TryCollectGroundLoot also executes.
- `wiring.log`: supplemental source audit only.
- `api.log`: Windows/iOS/Android API compilation, zero errors/warnings. No Unity engine execution or visual acceptance is claimed.

Existing runners only: CampPracticeProductionTests.py, CampPracticeSessionProductionTests.py, CampPracticeWiringTests.py. No registry additions. The service runner now explicitly compiles SafeSaveFlow.cs. Older SaveBeforeLeaving extraction fixtures need an EndPractice boundary; ChapterHost already has one, and the parent supplies RoomFreeSealHost's throw-on-unexpected-call boundary for integration.

Reproduction (SDK argument): run the three named Python runners; use `/workspace/shared/emberfall-tools/dotnet/dotnet` for the two compiled runners. API command is the existing G02/api_compile.py with repository, reference-assembly directory and SDK arguments. Log exception stacks after positive PASS are intentional negative-control executions, not positive-run failures. Combined integration/aggregate remains the parent's responsibility.
