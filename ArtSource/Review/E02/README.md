# E02: status feedback follows actual state

`EnemyStatusEffects.ConsumePoison` now retires the enemy's sustained poison particle/shape channels synchronously after a successful consumption. It does not destroy the aura or its reusable channel objects, and it does not touch fire or independently owned impact/tail objects. A same-frame `Poison` refresh can reactivate the retained channel; duplicate cast receipts do not clear the refreshed state. Stored damage, stack threshold, tick schedule and receipt logic are unchanged.

The third confirmed Arcanist basic hit now uses orange `灼触` feedback when the actual specialization applies Burn. Freeze/FrostMark retain their existing cold `霜触` feedback. No damage coefficient, proc cadence, cooldown, energy return or status duration changed.

`StatusFeedbackProductionTests.py` compiles the full actual EnemyStatusEffects, actual OnBasicAttackHitTarget, actual ElementalEnemyAura, actual OnEnemy/ClearPoison entry points, and real proc/tick/receipt rules. Unity components and visual-object creation are managed boundary recorders. Checks cover immediate consume, wrong owner, duplicate receipts, same-frame refresh/Update, continued ticks, retained fire/independent object, third-hit burn/frost dispatch, energy and unchanged immediate HP. Two compiled negative controls remove poison clearing or restore the false frost label and must fail their exact assertions. This is production control-flow evidence, not Unity rendering/lease/device validation.

Raw logs: `status-feedback.log`, `burn-finale-regression.log`, `basic-timeline.log`.

A separate coordinated E04 change in the same EnemyStatusEffects file caches CombatModel and lets its knockdown adapter animate eligible enemies before the original 72-degree fallback. E04 owns its pose implementation/acceptance; the unrelated burn fixture returns false at that boundary.
