# Chapter C round review scope

This round builds on the existing camps, trials, five-room expeditions, chapter nodes and tactical objectives. It does not create a second parallel dungeon or reward system.

| Request | Concrete change | Primary managed verification |
| --- | --- | --- |
| A | Entry-level XP budget, registered death shares, atomic completion and shared first-core eligibility | ChapterExperience, ChapterHost, ChapterReturnTimeScale |
| B | Explicit mobile enemy intent, cancel-safe tap gestures, captured charge identity | MobilePinnedTargetProduction, ProductionTouchLifecycle, BlockedCombatUpdate |
| C | Hard/Hero interruption versus anchor exposure; one locked-bearing follow-up | ChapterCombatProduction, LargeBossPhaseState, BurnFinaleProduction |
| D / C01 | Direct Star boss room, saved result snapshots and actual exit-visual gate | ChapterHost, ChapterEntry, ChapterReturnTimeScale, LargeBossShutdown |
| E / C02 | Independent simultaneous seals, mirrored short/long approaches, one tactical marker owner | ChapterSealRouteProduction, TacticalLiveVisual |
| F | Fixed incremental, affordable and catch-up reforge targets | ReforgeSelection, EconomyGrowth, EconomyGoalUi |
| C03 / C04 | Accepted-event feedback and explicit effect producer priorities | BurnFinaleProduction, ShatterAvailability, EffectPriorityProduction, ElementalPriorityProduction |
| C05 / C06 | Distinct nonwarrior tier structures and pose-following rigid companion attachments | ClassTierStructureProduction, EquipmentCompositionProduction, CompanionAppearance, CompanionIntentProduction |
| C07 / C08 | Actual-skill charge family and persistent node selection with truthful progression | CasterChargeProduction, HeroPoseCommit, ChapterEntryProduction |
| C09 | Node atmosphere/composition and clipped, phase-driven hazard bodies | ChapterNodeWorldProduction, ChapterCombatProduction |
| C10 | Local collection preview clock/actions/cloth and structural common/rare weapons | CollectionPoseIsolationProduction, CollectionPreviewPresentationProduction, PreviewClothProduction, WeaponFashionStructureProduction |

The default validation runner includes the new production tests. Its report records source hashes and detects source changes during execution. Compiled negative controls must first compile, then fail their specific behavioral oracle; unrelated build failures are not accepted as evidence.

No Unity Editor was available in this environment. Managed tests and cached Unity 2021 API compilation do not validate Unity 6 behavior, rendered readability, animated intersections, device input feel, GPU performance or builds. Seed/route/formation statistics establish deterministic constraints, not subjective gameplay variety. Those checks remain a required downstream playtest boundary.

Review as drafts. Do not merge or publish here. Shared scripts/tests/docs are synchronized while platform settings, fonts and iPad configuration remain owned by their existing repositories. Android stays a local source candidate; no Android repository or signing/upload credentials are required.
