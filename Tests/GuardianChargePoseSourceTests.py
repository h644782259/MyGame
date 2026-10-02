from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/p).read_text()
e=read('Assets/Scripts/Combat/EnemyController.cs');m=read('Assets/Scripts/Combat/CombatModel.cs')
assert 'EnemyActionPose.Select(preparing,activeChargePose,attackAnimation)' in e
resolve=e[e.index('private void ResolveAttack'):e.index('private void FinishAttack')]
assert 'activeChargePose = chargeTime > .001f;' in resolve
assert 'activeChargePose = false;' in e[e.index('private void FinishAttack'):e.index('private void FinishAttack')+120]
assert 'activeChargePose = false;' in e[e.index('private void CancelAttack'):e.index('private void CancelAttack')+380]
charge=e[e.index('            if (chargeTime > 0)'):e.index('            else if (preparing)')]
assert charge.index('DamageTarget(damage*1.35f)')<charge.index('AnimateModel(1,.6f,hurtTime>0)')<charge.index('FinishAttack()')
assert 'walkingDisplacement +=' not in charge
assert 'EnemyActionPose.Charge(enemyActionPhase)' in m and 'chargeBrace*23f' in m and 'chargeBrace*58f' in m
assert 'private void OnDisable() { CancelAttack();' in e
print('PASS: 8 Guardian active-charge production wiring contracts')
