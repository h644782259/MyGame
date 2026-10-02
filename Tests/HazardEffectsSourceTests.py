from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda f:(r/'Assets/Scripts'/f).read_text()
t=read('Combat/EnemyAttackTelegraph.cs'); c=read('Combat/CombatEffects.cs');e=read('Combat/ElementalCombatVfx.cs')
assert 'line!=interruptMark' in t and 'line.startColor=line.endColor=Danger' in t
assert 'clock.positionCount=count' in t and 'interruptMark.enabled=value' in t
assert 'respectCover:true' in c and 'CombatSight.FillAreaBoundary(boundary' in c
assert 'CombatSight.FillAreaBoundary(circle' in read('Combat/SkillTargetingController.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/FilledSkillVfx.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/ElementalFieldVisual.cs')
assert 'CombatSight.VisualFootprint' in read('Combat/CoveredAreaParticles.cs')
assert 'DecorationBudget.Particles(EffectPreferences.EffectsScale)' in e and 'rate * EffectPreferences.EffectsScale' in e
assert 'DecorationLease.Attach(obj,1)' in e and 'ReducedEffects?4:7' in e
assert 'DecorationBudget.DeathMotes' in read('Combat/EnemyDeathDissolve.cs')
assert 'private void OnDisable(){Release();}' in read('Combat/ElementalFieldVisual.cs')
assert 'private void OnDisable(){if(budget!=null)budget.Release(ref leased);}' in read('Combat/DecorationLease.cs')
assert 'fire.gameObject.SetActive(false)' in e and 'StopEmittingAndClear' in e
assert 'DecorationLease' not in t
print('PASS: 14 hazard/effects source contracts (not rendering validation)')
