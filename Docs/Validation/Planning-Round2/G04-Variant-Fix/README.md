# G04 fix: equipped mechanism-aware rank previews

Baseline: `608e659` in isolated branch `codex/planning-round2-draft-variant-fix`. No changes were made to the root frozen checkout or its pending five fixture repairs.

`BuildDraft.SkillChangeEffects` now obtains the old rank from the actual owner's `SkillEffectSummary` and the new rank from the isolated draft service. Both evaluate the worn, class-valid mechanism and actual unlocked selected variant. A bag-only, wrong-class or locked forged B does not activate the override.

For Venom B, the complete rank effect is replaced with the actual single narrow projectile coefficient (240/360/480% attack), first intercepted target/wall rules, no fan/multitarget explosion/spreading, and the retained single ordinary poison detonation. It never inherits the original fan's seven/nine-arrow, piercing or blast upgrades. Venom A retains the original fan rank and A propagation effect.

Frost Echo and Cinder Trail describe only the selected A/B modifiers and retain the applicable rank attacks. Their damage/radius values share the same BuildCatalog functions used in live combat. Active elemental specialization overrides are shown explicitly. Returning Blade B displays the counter stance instead of claiming A ricochets; summon mechanisms explicitly show the replacement cap/inheritance rule from the authoritative catalog. Energy and cooldown still come from GameBalance.

Shared-rule extraction is behavior-preserving: concentrated venom coefficients, frost opening/echo/radius multipliers, cinder direct/radius/tick multipliers retain their prior values. Existing combat methods now call these shared rules, rather than the preview maintaining a second numeric table. Two managed projectile fixtures import the actual coefficient method required by that shared dependency; they do not duplicate its implementation.

## Evidence

- `variant-effects.log`: 84 assertions against actual ProgressionService, equipment collection/equip/unlock transactions and BuildDraft, including B 2→3/3→2, both old/new effects, unchanged disk/profile, missing/wrong/locked gear, caster A/B and specialization, counter B, summon cap and authoritative energy/CD. Three compiled behavioral controls restore the old before/after generic summaries or bypass the unlock check; all fail their specific assertions.
- `CampBuildDraftProductionTests.log`: pre-existing draft UI/service/preset and negative controls pass.
- `ConcentratedVenomProductionTests.log`, `ConcentratedVenomLaunchTests.log`: actual contact, projectile construction and persistence tests plus their old-behavior controls pass.
- `BlockedCombatUpdateProductionTests.log`, `AuthoredProjectileProductionTests.log`: existing simulation pause/cosmetic regression checks and controls pass.
- `UNITY_STANDALONE_WIN.log`, `UNITY_IOS.log`, `UNITY_ANDROID.log`: full runtime compile against the pinned Unity 2021.3.33 API, zero warnings/errors.

Managed tests and API compilation are not Unity engine or device acceptance. No claim of actual Unity/Win/iOS playthrough, screenshot or device performance is made. Register `BuildDraftVariantEffectsProductionTests.py` in the root integrated validation runner; this isolated fix does not edit that runner.
