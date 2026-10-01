#!/usr/bin/env python3
"""Production wiring; no Unity or animation acceptance is claimed."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
read=lambda n:(root/'Assets/Scripts/Combat'/n).read_text()
p=read('PlayerController.cs');m=read('CombatModel.cs');fx=read('CombatEffects.cs')
b=p[p.index('private void BasicAttack()'):p.index('private bool Melee(')]
assert b.index('attackCooldown = Mathf.Max(.18f, attackCooldown)') < b.index('model.PlayAction(-1,true,attackCooldown)') < b.index('Melee(')
assert 'CombatProjectile.BasicShot(' in b and 'StartCoroutine' not in b and 'Invoke(' not in b
assert 'BasicActionTimeline.Contact(heroClass == HeroClass.Ranger)' in m
assert 'Time.frameCount != actionStartedFrame' in m
assert 'BasicActionTimeline.ArrowVisible(t, acting)' in m and 'BasicActionTimeline.BowDraw(t)' in m
assert all('model.CancelAction();' in tail[:100] for tail in p.split('CombatEpoch++;')[1:])
assert 'session.IsDead || session.CombatEnded || session.Player.CombatEpoch != epoch' in fx
assert 'if (IsDead) return;' in p and 'if (session.InputBlocked)' in p
print('PASS: 8 basic action timing wiring contracts')
