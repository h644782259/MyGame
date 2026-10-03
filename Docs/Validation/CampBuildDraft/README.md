# Camp allocation draft — planning C

Entry: Camp → existing build plans → 局部调整配点 · 临时草稿. The shared responsive dialog serves desktop and mobile. Existing full reset and two build presets remain available.

Learned skills have local −/+ controls for ranks 2/3; rank 1 cannot be refunded and new rank-1 unlocks remain in the skill page. Four mastery tracks have local −/+, level cap, selected-core identity and points to the next core threshold. Returning a selected core below its threshold disables it in the draft; Undo restores the previous core with the allocation. Undo retains the last 128 edits. Remaining points use the existing unified `max(1, level-1)` budget. Stats use the actual `GetStats` implementation on a detached preview service, including current equipment, passive rank and fashion.

No editing action writes, emits Changed, or mutates the real profile. Apply validates against the current character and writes one candidate. “Apply and save A/B” includes the selected preset in that same candidate, so failed disk writes leave both the live allocation and saved presets unchanged. Cancel/Back, hero replacement, character change or closing the camp discards the draft. External profile replacement or in-place mutation invalidates the stale draft. Applying uses the existing Changed → OnProgressChanged → RefreshStats(false) path; it does not refill HP/energy or reset skill cooldowns.

Validation:

- `production.log`: 289 checks executing real ProgressionService, real GameUI.BuildPlans/GameUI.BuildDraft button handlers, and extracted unchanged real GameSession.OnProgressChanged / PlayerController.RefreshStats methods with real SkillRuntime. Exercises levels 35/50/100, three-point transfers, single rank 3→2, rank-1 floor, mastery caps, shared-budget exhaustion, undo/core restoration, stale/cancel gates, failed atomic writes, apply-only, A and B, reload, desktop/mobile routes, and retained HP/energy/cooldowns. The existing 136 build-preset checks run alongside.
- `existing-ui.log`: 20 existing build-plan source wiring contracts.
- `api-compile.log`: complete runtime source compilation for Windows, iOS and Android against pinned Unity API references.

Run `python3 Tests/CampBuildDraftProductionTests.py /path/to/dotnet`. It is registered in cloud-validation. Tests use isolated temporary save directories; UI drawing, platform JSON transport, model/companion rendering are managed boundaries. These checks are not Unity execution, on-device touch validation or rendered UI acceptance. No gameplay numbers or art assets changed.


## PR44 fractional-HP correction

The legacy `RefreshStats(false)` floors living HP at 1. A no-op draft at 0.5 HP therefore healed the player. Draft commits now publish a scoped `IsApplyingBuildDraft` signal **only after successful persistence**; `finally` restores the previous scope, and ordinary nested transactions explicitly use false. During that scope only, the actual player refresh preserves absolute HP with `Min(Health, MaxHealth)`. Dead zero stays zero, and lowering MaxHealth still truncates HP normally. All non-draft floor/heal semantics remain unchanged.

The actual UI → draft service → Changed → session → player refresh tests now exercise 0.5, 0 and 37 HP; no-op applies; failed writes; apply-save A increasing maximum; apply-save B lowering maximum; unchanged energy/cooldowns; ended signal scope; and ordinary floor/explicit-heal calls. Four separately compiled negative controls reject the old floor, missing draft signal, a global floor removal, and a missing finally reset. The two pilot suites extracting the real RefreshStats method use a false draft-signal fixture boundary; their logs are included. No change to the other C features or any art resource.
