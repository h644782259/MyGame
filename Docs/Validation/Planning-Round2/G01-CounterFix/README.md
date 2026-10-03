# G01 review follow-up — single counter clock and unseen pause receipts

Based on G01 `b0a635087e57810fed3f0ee2c46d2dc73538bc01`.

1. Removed the duplicate legacy `BasicOpportunity()` draw from `MobileControls.DrawAvailability`. The counter's only clock now consumes `BasicOpportunityWindow().Actionable`; it stays muted when target/reach/LOS prevents execution. Center rejection and lower mastery clock remain separate.
2. `CurrentCombatResult` observes the actual latest receipt while blocked instead of its last rendered cache. `LatestCombatResult` and `BurnCashFeedback` accept an optional read-only `includeBlocked` flag for suppression; default calls retain their existing blocked behavior. Thus a receipt produced immediately before pause, without an intervening draw, is suppressed on resume.

Evidence:
- `draw.log`: 32 assertions execute the complete production DrawAvailability/LabelControl through a recording GUI. No-target, too-far and occluded observations at .86/1.0 touch scale produce exactly one muted counter clock, no legacy readiness query, and coexist with rejection/mastery captions. Ready and expired observations are covered. Compiling the removed old draw back in fails the exact GUI draw-count oracle.
- `pause-receipt.log`: 19 assertions execute the production UI result query, runtime LatestCombatResult, BurnCashFeedback and CombatResultChannel. Covers actual receipt → no draw → pause → resume with empty/previous cache, fresh later events, target changes and expiry. Compiled old last-rendered-cache behavior fails the resume oracle.
- `channels-regression.log`: 222 production window/channel assertions plus the existing three compiled negative controls.
- `source.log`: existing 22 source contracts updated to require one authoritative counter draw.

Register `Tests/MobileBasicWindowDrawTests.py` and `Tests/CombatResultPauseReceiptTests.py` with the normal dotnet argument. UI observations are managed recording boundaries; actual Unity frames, input and devices remain unverified. No damage/resource/timing/save behavior was changed.
