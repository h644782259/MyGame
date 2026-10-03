# Forty-slot visual coverage closure — F6

Baseline: PR35 `0daa0f2dec8b53f695888032e3972b24a8145773`. This finite audit covers the existing four classes × ten slots, shared ordinary projectiles, real status channels, fields and existing bosses. It does not create new skills, speculative passive casts or forty duplicate meshes. F6 does not edit `CombatEffects.cs`; ordinary projectile bodies, meteor kernel and Neutral trap primary belong to the parallel F5 branch. Rows identify that dependency explicitly rather than claiming it is present at this commit.

**Acceptance boundary:** actual production control flow + authored bytes execute in managed Unity doubles; exported mesh samples are rendered by Blender. This is not Unity import/rendering, real shader, player footage or device performance acceptance. “Retain” means a named, connected existing presentation is intentionally preserved with the listed regression evidence. It does not mean every rank/pose/camera was inspected in Unity.

## Implemented gaps

1. Vanguard guard4, Arcanist guard5, Summoner guard5 previously called default Rune and therefore generic crescent preparation. Their actual activation blocks now call `AdvancedSkillVfx.Protection`, which selects the existing authored `ProtectionCage`. Guard counter response selects that identity for its short existing response lifetime.
2. All four slot8 defensive passives select ProtectionCage **only inside actual `TryDefensePassive` after its existing rank/cooldown/health gates**. `GameBalance.IsPassive(3/8)` still prevents active casting. Slot3 receives no invented effect.
3. Protection owns a fresh non-pooled anchor. Guard/passive are separate per-owner channels. Refresh retires only its previous channel anchor, never an unrelated skill's castId. A predicate of the real guard/passive timer cancels early even during pause/zero delta. The anchor stores no pooled visual reference. Synchronous hierarchy disable releases the child lease before deferred destruction; a previously returned/re-rented child is detached and unaffected.
4. Sustained Fire/Poison aura uses its bound `EnemyStatusEffects` state, not wall-clock lifetime. The old deadline could expire while InputBlocked paused actual status schedules, or outlive source-epoch invalidation. Pause now keeps active status feedback; actual expiry/consume/invalidation/death or missing bound state clears it. Damage/tick/status schedules are untouched. Unbound decorative callers keep the existing deadline fallback.

No new runtime meshes, materials or textures are added by F6. Missing ProtectionCage still uses existing procedural Charge fallback, with identical state ownership. Mesh source remains `ArtSource/BlenderSkillIdentities/Four-Skill-Identities.blend` and deterministic identity builder.

## Runtime routing and evidence key

- **P** `PlayerController.CastSkillCore`: common rank/cost/cooldown/passive checks, actual release, slots0–2 and guard branches. `SkillChargeController` controls only the existing eligible charge previews/release/cancel; no new windup was added.
- **A** `AdvancedSkillSequence.Configure/Update` dispatches Vanguard/Arcanist/Ranger/Healing, owns accepted target/origin and existing event schedule. `SpawnTail` remains separate area ownership.
- **S** `SummonerSpell.Cast/Update` owns impulse, thorn area, contract spawn and gravity target/tick/finisher. `SummonedCompanion.CastContract` owns actual partner success.
- **C** `CombatArea` uses authoritative startup/tick/finish schedule and `SkillVisualRecipe`; **B** `CombatProjectile` owns launch, movement, collision and volley receipts. These remain unchanged by F6.
- **V** `FilledSkillVfx`/`BlenderSkillVfx` provide actual authored geometry; dynamic `CombatSight` clipping remains authoritative. Pool generation/lease priorities and confirmed-finales contract remain.
- **D** new `DefenseIdentityProductionTests`: extracts the *actual* three guard blocks and entire `TryDefensePassive`; runs Protection → Rune → Charge → real bytes → real lease/pool. Covers 3 guards×3 ranks, 4 passives, unlearned/cooldown rejection, refresh/reapply/independent channels/follow, pause/zero delta, early end, death/epoch, immediate lease release, detached same-frame re-rent and missing-asset fallback. Two compiled negative controls reject missing identity or ignored state.
- **T** `StatusFeedbackProductionTests`: actual basic-hit/status/aura chain and real tick/receipt rules; consume/reapply, Burn/Frost feedback, paused wall-time, owner-epoch invalidation, death. `BurnFinaleProductionTests` retains existing due-drain/claim/delivery/no-double-tick checks.
- **I** existing `AuthoredSpellIntegrationTests`, `SkillIdentityCallsiteProductionTests`, `DenseFinaleProductionTests`: loaded identity, cover/low-tier budget, true health-loss lightning/contract endpoint and actual heal/charge event paths. `ArrowBatchGenerationProductionTests` guards stale pooled handles; `PlayerFinaleTailProductionTests` guards finale/tail identity separation.
- **R** existing `SkillDamageBudgetTests`, `CombatTimingSourceTests`, skill/target/traversal tests verify existing rank budgets and source contracts. Source-only assertions are not mislabeled as executed visual/player tests.
- **G** existing `BossSweepCapsuleProductionTests`, `LargeBossShutdownTests`, `LargeBossMotionTests` plus knockdown geometry checks retain boss/rig contracts. Their logs distinguish executed production geometry from engine doubles.

## Complete 40-slot disposition

Preparation means existing aim/charge/activation cue where applicable; it never adds a damage delay. “End” refers to the existing sequence/area/pool retirement or actual state termination, not a new gameplay event. The slot names below match `GameBalance.SkillNames` exactly.

|Class/slot|Name|Preparation / actual impact or sustained identity / end|Disposition and evidence|
|---|---|---|---|
|Vanguard 0|旋风斩|P release → `BlenderSkillVfx.TryPlay(false)` + original melee; rank follow-ups in C; short mesh life|Retain accepted blade preview integration; I/R|
|Vanguard 1|裂地冲击|P release → `TryPlay(true)` + original melee/knockdown; original rank echoes; short mesh life|Retain ground-shock; I/R and knockdown production checks|
|Vanguard 2|剑刃风暴|P → C Steel scheduled field/finisher; Steel maps to authored Sword, not BladeSlices everywhere|Retain existing connected sword body and field retirement; I/R|
|Vanguard 3|剑术精研|Passive stats; no preparation, impact or independent lingering cast|Retain no-cast rule; P/IsPassive/R|
|Vanguard 4|圣盾反击|True guard activation → ProtectionCage; real counter response short identity4; guardTime=0/replace/epoch/death retires anchor|**Fixed state-bound identity**, D; damage/guard timer unchanged|
|Vanguard 5|破军突进|A case5 real accepted dash lane → HitLine + existing landing/tail cues; sequence ends|Retain dynamic lane and accepted displacement, no baked reach; I/R|
|Vanguard 6|生命战旗|A Healing scheduled heal → existing identity4 at real heal event; short event effect ends independently|Retain already-authored healing identity; I actual heal/rank3 energy|
|Vanguard 7|大地崩裂|A case7 actual travelling fault points → beam/falling-blade/HitArea/launch; final burst then retire|Retain dynamic fault point schedule and authored impact; I/R|
|Vanguard 8|不屈意志|No active cast; actual low-health/rank/cooldown gate → ProtectionCage; passiveTime end/retrigger retires|**Fixed true-trigger identity**, D; no invented cast|
|Vanguard 9|终焉裁决|A case9 main judgment first → actual sword array beats; registered finale/independent tail retirement|Retain existing main/secondary ordering and protection priority; I/R|
|Arcanist 0|冰霜新星|P → C IcePrimary impact/echo; rank3 B shards; actual frost/Burn specialization control path unchanged|Retain primary; shard body dependency F5; I/T/R|
|Arcanist 1|陨星术|P → C original .7/1.1 startup and falling weathered-rock body → FirePrimary impact/aftermath; C ends|Retain scheduling/fire body; meteor-kernel refinement dependency F5 (baseline already WeatheredRock); I/T/R|
|Arcanist 2|奥术风暴|P → C Lightning scheduled pulse/finisher using existing lightning identity; C end|Retain connected fork/lightning body, cover and budgets; I/R|
|Arcanist 3|奥能亲和|Passive stats, no cast or visual timer|Retain no-cast rule; P/IsPassive/R|
|Arcanist 4|雷霆锁链|A ChainLightning actual targets/dynamic links; endpoint ForkPulse only after actual health decrease; short contacts retire|Retain E03 true contacts; I executed callsite|
|Arcanist 5|冰晶护体|P true guard → ProtectionCage with existing Burn/other tint; real guard pulses/stride unchanged; guardTime ends anchor|**Fixed state-bound identity**, D; no false ice assertion for Burn|
|Arcanist 6|奥术回流|A Healing actual recovery beats → ProtectionCage contact; event life ends|Retain true healing event; I|
|Arcanist 7|虚空漩涡|A case7 accepted target/real pull → existing Arcane lattice/strut impacts; sequence/tail ownership ends|Retain dynamic pull and existing lattice (procedural intentional mesh); I/R|
|Arcanist 8|法力屏障|No active cast; real defense trigger → ProtectionCage, actual energy/control branch unchanged; passiveTime end|**Fixed true-trigger identity**, D|
|Arcanist 9|天灾终章|A case9 actual specialization/beat selects ice/fire recipe; real final settlement confirms finale; tail separate|Retain alternating/type-true identity and burn settlement; I/T/R|
|Ranger 0|扇形箭|P actual fan directions → B ordinary arrows/volley budget; collision/lifetime ends each shot|Retain actual directions/timing; authored ordinary ArrowBody dependency F5 (ArrowRain is separate); R|
|Ranger 1|震荡陷阱|P → C Neutral startup/echo/control; original area end|**F5 shared Neutral trap primary dependency**; F6 does not claim old generic presentation complete; R|
|Ranger 2|天幕箭雨|P → C ArrowRain actual beats → authored synchronized arrows; area/arrow batch ends|Retain existing arrow timing, low-tier/cover; I/R|
|Ranger 3|弱点洞察|Passive stats, no cast|Retain no-cast rule; P/IsPassive/R|
|Ranger 4|逐风步|A case4 accepted backdash/mobility → B follow-up arrows and existing optional tail; timers end|Retain dynamic movement; ordinary arrow body dependency F5; R|
|Ranger 5|毒蔓牢笼|A case5 → C Poison/Vine scheduled damage and optional finale; real poison aura cleared on consume/state end|Retain authored vine/field, **fixed aura state lifetime**; T/I/R|
|Ranger 6|森林祈愿|A Healing real recovery beats → existing ProtectionCage contact; event ends|Retain actual healing path; I|
|Ranger 7|幻影连射|A case7 actual locked target/accepted muzzle → B scheduled piercing/homing arrows; target mark on real hit; volley ends|Retain actual mark/shot dispatch; ordinary arrow body dependency F5; R|
|Ranger 8|灵风庇佑|No active cast; actual defense trigger → ProtectionCage; invulnerability/speed stays original; passiveTime end|**Fixed true-trigger identity**, D|
|Ranger 9|万箭归星|A case9 generation-safe ArrowBatch actual beats/final impact + rank3 ordinary radial B; retire/tail separate|Retain pooled generation/finale chain; F5 ordinary radial body dependency; I/R|
|Summoner 0|灵能冲击|S release ContractSigil; additional endpoint only after actual health loss; short contacts retire|Retain actual release/contact distinction; I|
|Summoner 1|荆棘牢笼|S → C Poison recipe/Vine from ThornStartup/Duration/Interval; original control and area end|Retain vine structural family with existing class tint/status semantics; no claim poison DOT inferred solely from recipe; I/R|
|Summoner 2|灵狼契约|S → CastContract Wolf; summon impact only if actual partner exists; separate companion lifecycle|Retain actual contract success/ownership; I and companion production tests; appearance F1|
|Summoner 3|灵魂共鸣|Passive stats, no cast|Retain no-cast rule; P/IsPassive/R|
|Summoner 4|星灵契约|S → actual Spirit partner spawn; companion real attacks use B; summon event ends, partner persists|Retain contract success; ordinary ContractBolt dependency F5, appearance F1|
|Summoner 5|灵魂护盾|P true guard → ProtectionCage; existing guard/share semantics; real guardTime ends visual|**Fixed state-bound identity**, D|
|Summoner 6|回春共鸣|S → A Healing actual owner/partner recovery beats → ProtectionCage event; event ends|Retain real healing/partner dispatch; I|
|Summoner 7|引力印记|S fixed accepted point/default preparation + actual pull/tick traversal; finisher after drained scheduled targets → Summon/ContractSigil impact; retire|Retain dynamic target/tick/finisher ownership; generic preparation is decorative fallback, not a missing damage identity; I/R|
|Summoner 8|灵体庇护|No active cast; actual defense rank/cooldown trigger → ProtectionCage; passiveTime/epoch/death clears|**Fixed true-trigger identity**, D|
|Summoner 9|远古树灵|S → actual Treant partner success → Summon impact; independent partner state|Retain contract/partner ownership; complete appearance F1, no new summon timing; I|

## Sustained states and fields

|State/field|Real source and feedback|Disposition / real end|
|---|---|---|
|Burn / Poison|`EnemyStatusEffects.Burn/Poison`, `ElementalCombatVfx.OnEnemy` → bound `ElementalEnemyAura`; field/particle siblings remain independent|F6 bound actual state; T verifies pause, expiry ownership, consume/reapply, epoch invalidation and death; no broad sibling destruction|
|Freeze / FrostMark|Real `frozenTime/frostMarkTime`; `ConsumeFrost/TryShatter`; basic frost contact already type-correct|Retain real `Summary` → `GameUI` current-target HUD and real impact feedback. No invented persistent ice aura or claimed existing OnEnemy call. Summary removes consumed/expired state immediately on next UI draw. T checks Burn vs frost mechanics; GPU visual not accepted here|
|Mark / Slow|Real `markTime/slowTime` and their gameplay multipliers; Summary current-target text|Retain explicit actual-state HUD; don't infer arbitrary color or add misleading permanent cast|
|Knockdown / Airborne|Real control clocks → existing E04 actual pose / airborne transform|Retain existing production rig/floor tests, not a new aura|
|Steel / Ice / Fire / Lightning / Spirit / ArrowRain / Poison fields|Actual `CombatArea` recipe and schedule → existing connected Filled/Elemental presentation|Retain loaded meshes and intentional dynamic composition; I covers actual resource/fallback/cover/low tier, R covers budgets. Ordinary actor status has separate ownership from area and burst tails|
|Neutral trap|Actual Ranger1 C recipe had marker/generic feedback without filled primary|F5 owns the finite missing-primary repair; do not change its radius/startup here|

## Existing bosses — intentional runtime identity retained

- Guardian normal/boss model + hammer/crown differences remain; `EnemyController` attack selection and `EnemyAttackTelegraph.Circle/Charge/Fan` draw the actual warning and confirmed attack shape. Shared decorative mesh upgrades do not alter warning radius or hit timing.
- Large expedition `LargeBossRig` has distinct astrolabe chassis, core, opening petals and aim-aligned beam emitter. `LargeExpeditionBoss.DrawBeam/ClipBeam/BeamContains` remain dynamic authoritative beam/cover, interrupt symbol and timing arc. No offline replacement of danger contours.
- Actual power anchors and live mask own core exposed/depowered feedback. `LargeBossRig.Shutdown` / `LargeBossShutdownVisual` own shutdown, not an unrelated generic explosion. Existing motion/sweep/shutdown production suites are retained.
- No separate mesh work is justified in F6 merely to increase file counts. Candidate model/anchor refinement is the finite F2 owner, and ordinary hostile projectile identity is F5. F6 changes neither damage nor boss states.

## Deliverables and acceptance

`ArtSource/DefenseIdentity/production.log`, `status.log`, `Runtime-Samples.json`, `Defense-Actual-Construction.png`, `preview.py` and README provide new hookup evidence and reproduction. Existing identity `.blend`/builder/source manifests remain authoritative; zero incremental asset bytes outside evidence. Targeted production regressions and three-platform API compilation are recorded separately and pass. The aggregate raw log is intentionally incomplete: the parent owns one consolidated full run. None is engine verification.

The forty rows above close classification for all existing slots at this branch. External F1/F2/F5 work is explicitly assigned and must be resolved when assembling the full upgrade; it is not silently treated as shipped in F6. Unity import/material appearance, gameplay-camera readability and Windows/iOS device performance remain acceptance tasks. Android receives synchronized source only through parent workflow. No video is generated.
