# Planning round 2 — five implementation packages

Baseline checked against remote main on 2026-10-03: Windows `5d85e47b9fab2489d5b06963a0b896ec19112740`, iOS `d3a4aab185eb368f5a4a7aba67b43678bfea508f`. Both original checkouts clean. New isolated worktrees only; previously revoked work and package delivery are out of scope.

1. Break-enemy mastery: noncritical 0.6/1.0 attack payout, retain 6s readiness and 6/4s cooldown, one cast qualification, separate ready/payout feedback.
2. Venom amulet: mutually exclusive single physical narrow arrow variant, actual first collision including walls, 2.4/3.6/4.8 attack direct damage, ordinary three-stack burst retained once, progression/save compatibility.
3. Rewards: weapon/wing/supply choice without cosmetic odds inflation; supply 1.5x integer-rounded coins; immutable old receipts; six independent difficulty first-award bits and evidence-based legacy migration.
4. Coordination: one designated Hard Redrock room only; at most two high-threat admissions, seeded 0.35–0.6s warning separation, fair queue and lifecycle cleanup without stat changes.
5. Camp practice: actual isolated static/moving/front-and-back combat; legal loadout/draft snapshot, no persistent rewards or live resource mutation; 10/60s actual metrics and comparable-configuration A/B receipts.

Each package has an independent implementation commit and raw test evidence. Integration preserves Windows/iOS platform configuration and Android conditional source compatibility. No ZIP, Library mutation, new Android repository or Android push. Main merging remains with the parent reviewer.

## Environment and validation limits

.NET SDK 8.0.425 and pinned Unity2021 API references are present. Unity Editor is absent in the current execution environment. Managed production-source fixtures, source contracts and platform-defined API compilation are available; they do not establish engine physics, JsonUtility, input/rendering or Windows/iOS/Android device acceptance. Historical reports will remain untouched; new raw logs and baseline failure comparisons belong in this directory.

## Cross-package constraints

Reward migration must use evidence of historical forced sequential difficulty unlock (ChapterProgression.CanEnter and completion guards), never infer progress from absent/invalid fields. Practice must not use BuildDraft preview's real saveDirectory as writable persistence; reward/save paths require complete isolation. Distinct package worktrees prevent concurrent source rewrites; final checks run against a frozen integrated source tree.
