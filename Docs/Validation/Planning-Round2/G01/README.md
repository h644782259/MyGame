# G01 — Persistent opportunity clocks and transient results

Parent package: offense core commit `64c2800d0f6df6508e9d54e4236a2d9978fd1f33`.

## Implemented boundaries

`CombatOpportunityState` now separates Window (exists), Remaining (authoritative time), Actionable, and BlockReason. Its new MasteryCombo kind joins counter, shatter, reignite, burn finale, poison detonation, and empowered contract. BurnCash and EmpoweredHit remain result kinds, with real event receipt identifiers.

The existing `SkillOpportunity` resolver and execution gates remain intact. `SkillOpportunityWindow` observes the same release footprint plus the intended marked target when that target is temporarily unreachable. It shows the underlying status clock with a blocked reason during resource shortages, cooldowns, movement recovery, missing targets or line-of-sight rejection; it never grants a skill or guarantees delivery. Poison/burn ownership checks remain authoritative. Basic counter and mastery clocks use the same model and target/reach gating. Pause, death, owner retirement and expired/current-target changes are re-read without cached status timers.

Desktop slots and mobile buttons have a dedicated clock row independent of their original availability/failure captions. Unavailable clocks use muted text. Desktop basic heading marks blocked clocks with `·待`; the mobile attack control keeps counter at the top and mastery at the bottom, separate from its center failure caption. The desktop bar grows by 24 pixels with explicit clock-row spacing. Mobile clocks use the existing gap below skill buttons, keeping the first row away from the Boss health strip.

`CurrentCombatOpportunity` no longer returns early for results. `CurrentCombatResult` draws a separate short result line. `CombatResultChannel` suppresses replay of the same event receipt after observed pause/owner/epoch/target changes; repeated result draws do not mutate a window or extend its timer. Actual gameplay clocks remain owned by combat state; no new balance or persistence mutations.

## Evidence

- `channels.log`: 222 production window/result-channel assertions. Compiled negative controls reproduce the old result masking, old ready-only observation, and false-actionable blocked-window defects; each fails the intended assertion.
- `desktop.log`: 39 production DrawHotbar observations, including simultaneous shortage and window clock; removing the separate clock fails a compiled control.
- `mobile-slot.log`: 31 production slot observations, including simultaneous rejection and clock and minimum compact inter-row geometry; removing the clock fails a compiled control.
- `shatter.log`: 43 original production availability/geometry checks plus original negative controls remain passing.
- `core-regression.log`: 241 offense core/payoff/area checks and two negative controls remain passing.
- `source.log`: 22 opportunity/free-command source contracts.
- `guid.log`: final unique/meta audit passes. `guid-attempt1.log` preserves a malformed new GUID caught by the audit and corrected before commit; existing GUIDs were not changed.

Register `Tests/OpportunityChannelsRound2Tests.py` with the standard dotnet argument in the integrated cloud runner. Run it alongside the modified existing desktop/mobile suites and existing `ShatterAvailabilityTests.py`. This package intentionally does not edit shared runner registration.

## Unverified

All executions are managed tests with declared Unity/drawing/physics substitutes or source/geometry contracts. No Unity Editor/player or device was run. Actual font metrics, readability, overlap under every touch preference and translated label, physics occlusion, pause-frame rendering, and Windows/iOS/Android devices still need Unity acceptance. The source changes do not constitute device acceptance.
