# F6 defensive identity and sustained-state lifecycle

No new mesh, texture or material assets. Actual guard activations (Vanguard4, Arcanist5, Summoner5) and all four classes' genuine slot8 defensive triggers use existing `ProtectionCage` bytes instead of generic crescent preparation. Counter response remains a short actual-response identity and does not masquerade as a new sustained guard.

`AdvancedSkillVfx.Protection` owns a fresh, non-pooled anchor and a predicate of the real guard/passive timer. Guard and passive are separate per-owner channels; reapplication retires only the previous anchor in that channel. No pooled component is stored. Cancellation disables the hierarchy synchronously before deferred destruction. Same-frame pooled-child return/rerent is covered so destroying its former anchor cannot affect its new use. Epoch/death/state checks execute even with paused/zero delta; elapsed visual time advances only with gameplay.

Enemy fire/poison aura now reads the bound actual `EnemyStatusEffects` state. Its old wall-time fallback could expire during an input-blocking menu while status schedules remained paused, or leave a glow after source-epoch invalidation cleared DOT. Bound auras now keep paused active statuses and retire when the actual status ends/death occurs or its bound state source disappears. The fallback deadline remains only for unbound decorative callers. No status damage, stacks, duration, hit timing or control changed.

Evidence:

- `production.log`: actual three guard construction branches × rank1–3, actual four-class passive method, actual Protection/Charge/mesh/lease chain, pause/cancel/refresh/follow/death/epoch, missing mesh fallback and two compiled negative controls.
- `status.log`: real poison/burn status→aura chain; pause beyond old wall-time deadline, source-epoch invalidation, death and missing bound status during pause; existing consume/reapply and burn/frost feedback checks retained.
- `api-compile.log`: all runtime sources compile for Windows, iOS and Android defines against pinned Unity API references, zero warnings/errors; no Unity execution.
- `related-regression.log`: five related production suites, including real authored loading, callsites, dense finales, arrow generations and burn settlement.
- `Runtime-Samples.json` and `Defense-Actual-Construction.png`: sampled actual production mesh transforms/opacity rendered in Blender with a neutral illustrative material. Caption Guard0=Summoner, Guard1=Vanguard, Guard2=Arcanist; Passive is a real slot8 trigger. These are not Unity shader screenshots. No new `.blend` asset is needed; editable geometry remains `ArtSource/BlenderSkillIdentities/Four-Skill-Identities.blend`.

All gameplay and generation checks use managed Unity API doubles. Deferred destruction and hierarchy disable are explicitly modeled for lifetime tests, but cannot prove Unity engine rendering/device performance. The aggregate run in `full-validation.log` was intentionally stopped after passing checks so the parent can run one consolidated full validation across all F branches; it is an incomplete run, not a success claim. Targeted final checks/API compilation are recorded separately.
