# Death-screen ground-loot persistence

Narrow follow-up to PR36 baseline `6b712c7ceaadd9fc70956af558c443a1593d660a`. The original death handler saved only the profile; runtime pending ground equipment was first preserved on Respawn/exit. A process exit on the death screen could therefore discard that equipment despite the death notification.

`OnPlayerDied` now calls the existing checked `PreserveWorldLoot` before its final profile save. Accepted pickups are retired only after the service has written their identities. Inventory/pending capacity overflow uses the existing bounded recovery mailbox. A failed preservation keeps the exact pending pickup and visible error, and skips the unchecked final save. Respawn/exit retain their existing retry preflight. No new receipt, reward, autosell rule, capacity or retry seed behavior is introduced. The existing summary is built in the same position; this change does not add an item-count field to the recap.

Validation:

- `DeathLootPersistenceProductionTests.py`: 27 assertions execute actual OnPlayerDied, TryCollectGroundLoot, CollectRemainingDungeonLoot, PreserveWorldLoot and Respawn methods extracted from source, with actual ProgressionService writes in isolated temporary directories. Scene transitions/audio/UI are bounded doubles. Tests include death→disk reload before respawn; full inventory/pending→recovery; duplicate death/reopen/respawn; blocked `.tmp` writes preserving old primary/backup and exact pickup; and two drops where a real Changed callback blocks saving after the first commit, then retry grants only the second.
- The original direct-save death method is compiled as a negative control and fails the expected lost-ground-identity assertion.
- Existing real service receipt suite:184 assertions passed. Existing integrated journey:3684 assertions across8 fresh/legacy classes and72 actual chapter attempts, plus its five compiled negative controls. Related receipt/transition/save-flow source contracts passed.
- Test registered in `Tools/cloud-validation.py`. Raw outputs are adjacent. No whole aggregate was rerun here; no Unity/native filesystem implementation or platform player was executed.

Reproduce: `python3 Tests/DeathLootPersistenceProductionTests.py /path/to/dotnet`.

A failed disk write cannot make data crash-safe; the failed item remains in the live scene for retry and the error is shown. Successful death preservation is durable before the handler returns. Sequential accepted items remain committed even if a later item fails, and retry does not regrant them.
