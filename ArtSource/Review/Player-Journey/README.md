# Integrated player-journey evidence

This is production-service regression evidence, not a gameplay recording. No production defect was reproduced and no gameplay/runtime code changed. The bounded addition links one earned character/item through chapter objectives, the shared first core, slot training, partial-gold reforge, skill reset, preset restoration, reload and return failure/retry.

Frozen candidates: Windows `d7dfb66bd1043503c3ae57fd0ef4eb221841546c`, iOS `7b0a7f17fbd12bc2896d34c0c1f732ceaf0a1a32`, Android `6ec16d8602e59225ea12987da24c141bf0eb7812`. Bases: merged Windows `76440d9699a61827930342406e2e2b1f75064e3f`, iOS `20e5cdb91d05d1515f559acb21b0ec07bb40a8d1`, Android `134e617944aee99f16aac29b30e984f97bc57c30`.

## Validation status

The frozen 198-check aggregate is still running at this evidence checkpoint. The scoped author result below is complete; no terminal full-suite claim is made yet.

## Scoped author run

`Scoped/journey.log` records **3,684 assertions across eight fresh/legacy class journeys and 72 chapter attempts**. The four classes use the actual normal objective-first route, with no level, currency, unlock or reward-flag injection. The legacy-shaped save is edited on disk before actual LoadSlot migration. Exact generated host sources and hashes are included.

This author run preceded the test commit, so its original manifest honestly records the merged base HEAD; generated-source hashes identify the tested fixture contents. It must not be substituted for the later frozen full-suite report. No original log or manifest is rewritten to invent a commit identity.

All six negative controls compiled and then failed the intended named journey assertion. `Scoped/negative-controls/manifest.json` records exact mutations, expected assertions and restoration hashes; per-control raw stdout/stderr are preserved. Mutants cover skipped XP redistribution, lost level-one migration, missing reforge debit, loss of learned first ranks, preset rollback of slot training, and world destruction on failed load staging. Generated mutant build directories, SDK caches and synthetic save files are omitted; their source is reproducible from the retained original host and recorded mutations.

## Read the observed ledger

[journey-ledger.csv](journey-ledger.csv) opens in a spreadsheet. Each row is parsed directly from a `JOURNEY` record in the original stdout; [journey-ledger.json](journey-ledger.json) retains typed values. Filter `scenario` to compare a fresh and legacy-shaped start, then inspect stage, level/XP, gold, fragments, free/spent points and claim flags. It contains 136 observed fixture stages, not player telemetry or statistical balance evidence. No formulas or hand-edited totals replace the logged values.

The first core is created at level six. After five zero-kill Forest repetitions, the character is level eleven; actual affordable reforge grows the Vanguard weapon to nine or the other selected class relics to ten. A pre-reforge preset preserves that grown item and permanent slot rank. The final post-build chapter ends at level twelve with exactly one claimed core. See the candidate's `Docs/Integrated-Player-Journey.md` for the complete route and reproducible command.

## Validation boundaries

Actual ProgressionService, chapter/session methods and System.IO execute. JsonUtility is the existing System.Text.Json shim. AI/player positions, Unity scene objects, input, destruction, effect cleanup and other engine values are boundaries. Enemy random-loot delivery, world-loot retention and side-event settlement are stubbed: the inventory proof concerns the controlled first-core journey with no delivered random loot. Mastery stays zero; the test proves advanced-skill reset/shared-budget behavior, not nonzero mastery refunds.

Storage refusal is a real empty directory at the `.tmp` path, not disk-full or mid-File.Replace failure. A failed kill save deliberately keeps earned XP/gold live; the pending completion retries once. Corrupt-primary-and-backup staging fails without discarding the current built character or world; restoring the documents permits the same load request to retry. These are managed session/IO observations, not Unity playtesting, mobile controls, graphics or device performance.

The fresh Android toolchain preflight in this folder reports BLOCKED: required Unity/Android components are unavailable. No APK, AAB, installation, licence activation or device result was produced. Final Android source rollup and Library publication remain owned by the parent after selecting the final merged source; this evidence branch is not a release branch.

PR25 remains unchanged and default-off. Its separately retained strict 0.1mm fractional FBX failure is not reclassified by any passing journey test. The historical rendered recipe `49a356eb2df9951fc07f9758f3eea4055b0b9c30` is now also preserved remotely as `codex/moving-basic-render-recipe` in the Windows repository.
