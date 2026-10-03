# G01 review — consistent skill-slot actionability

Baseline: authorized frozen integration `608e659` (this fix uses an independent core worktree/branch; frozen v1 was not changed).

Desktop `DesktopSkillOpportunityCaption` now consumes `SkillOpportunityWindow`, matching its timer row. Mobile `DrawMobileSkillAvailability` reuses its already-read window for the executable caption and suppresses its green ready dot when an existing window is blocked. Neither surface reads the older ready-only skill observation for its executable caption.

The production window query is unchanged: for a summoner with a live command window, sufficient energy and no cooldown, no legal target retains the actual remaining time with `BlockReason="无目标"` and `Actionable=false`. A living legal target remains actionable.

Evidence:
- `desktop.log`: 43 actual DrawHotbar assertions; blocked summoner retains only the clock, without executable in-slot caption or emphasis border. Legal target restores the current-time caption and border. Compiled old query reproduces the defect and fails the intended oracle.
- `mobile.log`: 34 actual slot-draw assertions, recording caption geometry/color and fill colors. Blocked summoner has one muted clock, no green executable caption and no green ready dot. Legal target restores green caption with current time. Independent compiled mutants restore the old query and ready dot, failing their respective oracles.
- `window-runtime.log`: actual production window/host assertions include sufficient-energy/CD-ready summoner with no target, then a living target; existing window/result negative controls remain.
- `source.log`: 22 surface contracts.
- Attempt1/attempt2 logs preserve fixture/negative-oracle corrections while migrating tests to the shared window. Previous tests incorrectly counted the new independent clock as an executable caption; final assertions distinguish geometry and color instead of weakening the actionability requirement.

No runner registration is needed: these are extensions to registered suites. No runtime damage, cooldown, energy, targeting dispatch or save behavior changed. Managed GUI recordings and host fixtures are not Unity rendering/device validation. The parent owns synchronized platform integration and frozen v2 full checks.
