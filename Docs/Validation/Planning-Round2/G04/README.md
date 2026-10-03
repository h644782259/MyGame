# G04 — Draft change summary and practice result readability

This commit follows package 5 and its confirmed-health-loss follow-up. The camp draft now has an independently scrollable fixed summary above the editing body. It lists changed skill ranks, mastery allocations, points refunded/invested/remaining, core before/after and any disabled/degraded core capability. The body highlights changed entries in gold and allows unchanged entries to be folded/expanded. Undo, cancel and existing atomic apply/save remain intact.

Changed skills render before/after effects directly from `GameBalance.SkillEvolution`, cooldown from `GameBalance.EffectiveCooldown`, and energy from `GameBalance.SkillEnergyCost`. Changed mastery descriptions come from `BuildCatalog.MasteryDescription`; no parallel balance tables were introduced. Existing stat preview continues to use actual before/draft `GetStats`. Trial controls remain on this draft screen.

Practice retains the complete frozen configuration internally for comparison. The user-facing output now uses a frozen readable summary: class, level, skill/mastery allocation, core/tier, equipment and variant, rarity and upgrade level. It no longer displays the complete profile JSON. Both A and B summaries are captured at the start, not recomputed from the current character.

A narrow lifecycle correction is also included: all original scene roots are snapshotted before publishing practice ownership. A snapshot enumeration failure therefore cannot enter teardown and mistake original objects for temporary objects. Its injected exception case runs against the actual Practice partial.

Evidence:
- `draft-tests.log`: 295 existing/expanded actual service and desktop/mobile draft assertions, 136 preset assertions, four compiled negative controls. Includes changed summary, authoritative skill-effect text, folding, undo/cancel and exact HP/energy/CD preservation after apply.
- `practice-tests.log`: 26 actual service/record assertions and four compiled negatives, including transferred three points/lost core summary, readable configuration and real save failure.
- `session-tests.log`: 30 actual Practice lifecycle and actual Player.Initialize boundary assertions, including snapshot failure before handoff; two compiled negatives.
- `windows-api.log`: full runtime sources compiled against pinned Unity 2021.3.33 reference API.

These checks use managed boundary doubles/API assemblies, not Unity engine or mobile device execution. Touch scrolling geometry/readability, real actor lifecycle, input cancellation and device screenshots remain engine acceptance items. The root integration should run all three platform reference builds and the full validation matrix on the combined changes. No new runner name is required: existing CampBuildDraftProductionTests and the two CampPractice runners contain the added tests.
