# Existing source-contract failures (read-only audit)

Audited on integrated pre-PR22 head 69eef83882444a27a085b5400ca0040fdb4cdcfb, with all five failures reproduced at clean c5a734e. No runtime behavior loss confirmed and no tests changed. These scripts are not among the aggregate runner checks.

- CombatOpportunitySourceTests: targetReason.Length==0 now intervenes in the literal; the typed opportunity query remains and explicit refusal takes priority.
- EnvironmentPresentationSourceTests: authored tree colors changed and recipes moved; Wood/Foliage categories still feed WorldResources.ApplySurface. Existing scenery fixture does not record material categories; actual material wiring deserves a separate test.
- CombatReviewSourceTests: companion callback became a block with telemetry; locked mark still precedes damage, then the companion callback. Policy tests alone do not prove full projectile event order.
- TownBuildingGroupSourceTests: direct Primitive call count fell from eight to seven when roof construction moved to a seven-part helper under the same building root.
- HubSettlementSourceTests: direct occluder flag count fell from six to five; helper and BuildingOcclusionGroup own registration. Generic three-part fixture does not prove complete authored-town assembly.

Future maintenance should replace literal counts with actual production behavior checks and targeted compiled negative controls, preserving the original constraints. No Unity or complete live-town rendering was executed in this audit.
