# PR45 admission and desktop entry regression fixes

The earlier isolated counter fixture admitted every basic, while the independent mobile fixture disabled ReturningBlade. Together they missed a real interaction: the old mobile pinned-basic range rejected a 3.7m target before the counter could approach. The desktop workshop also retained an Arcanist-only button condition, preventing sword users from unlocking or freely toggling the new variant there.

`ReturningCounterReady` now requires Vanguard, the unlocked B variant and both live perfect-dodge counter timers. Only that state uses `ReturningCounterRules.Predict` in the actual mobile button/rejection reason. The same predictor supplies the execution landing. It uses the existing ground-path/player-clearance approach and checks actual melee reach from the landing. Ordinary/default/expired basics retain the original admission range. A blocked pinned action returns before cooldown, animation or opportunity consumption.

The desktop mechanism workshop now uses the same `BuildCatalog.HasMechanicVariant` eligibility as the mobile workshop. Existing camp, cost and persistence gates remain in the service.

Final raw logs in this directory:

- `counter.log`: 40 production assertions combining actual MobileFocus pin lifecycle/reason/admission with BasicAttack, Melee, enemy perfect-dodge registration/confirmation and WorldTraversal/CombatSight. Includes the 3.7m 175% hit, blocked-path rejection without spending, expired/empty/default range and valid short-range counter. Six compiled negative controls cover the former admission bug, accidental global extension, removed bounce exclusion, reverted window, widened thrust and blocked predictor acceptance.
- `desktop-persistence.log`: actual extracted desktop mechanism-workshop GUI branch drives unlock B → A → B through the real progression service; seven button assertions and a compiled Arcanist-only regression control. Also 14 persistence and 2,204 existing progression-growth assertions. GUI delivery is a managed shell, not rendered UI.
- `mobile.log`, `facing.log`: existing adjacent production suites pass with their negative controls (56 and 66 assertions). Their optional ReturningBlade boundaries remain explicitly disabled; the combined counter suite now owns that interaction.
- Three platform API logs and source-hash report: Windows/iOS/Android compile against pinned Unity 2021.3.33 references, zero warnings/errors. Source hashes match the runtime changes.

No full aggregate, Unity engine/device test or remote operation was performed. These follow-up checks supersede the prior isolated admission evidence; earlier art acceptance is unaffected.
