# B08 directed growth navigation

A camp route's missing-skill action now initializes the skill owner before selecting the exact detail and resetting only its detail scroll. First visits therefore cannot be reset back to the mobile list by owner reconciliation. Back returns to the originating workshop tab with its existing scroll; owner/slot mismatches cannot restore another character's route. Ordinary skill-list navigation remains separate.

Desktop equipment comparison keeps existing numeric scores and now shows the current and candidate mechanism benefits/costs together in the visible 85px summary region. A lost active mechanism is explicitly labeled. Concise text reuses `MechanicBadgePresentation`; longer descriptions remain available as tooltips. This does not change scoring or equipment eligibility.

The selected goal's measured status has a fixed region, its qualified action has a fixed 48-unit control, and candidates have their own scroll with at least one 48-unit viewport. Long status/failed-action text scrolls independently without moving the action. Selecting a new goal or performing its action returns status text to the top; candidate position stays intact. Costs and prerequisites still come from the progression service, leaving later economy changes independent of layout.

Validation: RouteSkillNavigationProductionTests.py executes the real route methods, owner reconciliation and ClosePanel, and rejects list-only/missing-return mutations. ProgressionGoalLayoutTests exercises 1000 geometry cases including long prerequisite text. GrowthNavigationSourceTests checks consumer ordering and rejects the old single-scroll header. Existing skill, inventory, chapter and collection regressions were rerun with narrow shell interfaces updated for the new route hook. Unity 6 typography, perception and physical touch remain unverified.

A coordinated B09 hook changes the HUD scale call to `HudLogicalScale.For`; the helper preserves the previous formula for valid dimensions and is owned by the world-label workstream.
