from pathlib import Path
r=Path(__file__).resolve().parent.parent/'Assets/Scripts/Combat'
p=(r/'PlayerController.cs').read_text();pet=(r/'SummonedCompanion.cs').read_text();charge=(r/'SkillChargeController.cs').read_text()
assert 'if (!TraversalStartedThisFrame && wantsBasic && !suppressBasic' in p
basic=p[p.index('private void BasicAttack()'):p.index('private bool Melee(')]
assert basic.index('TraversalStartedThisFrame || model.BasicActionBlocked')<basic.index('GameAudio.Play(SoundCue.Attack)')<basic.index('model.PlayAction(')
command=pet[pet.index('private void Command('):pet.index('private float AttackMultiplier')]
assert 'if (preservePoint)\n            { commandedPoint' in command and 'preservePoint && commandedTarget == null' not in command
acquire=pet[pet.index('private EnemyController AcquireTarget()'):pet.index('private void Update()')]
assert acquire.index('if (ValidTarget(commandedTarget)) return commandedTarget;')<acquire.index('if (commandTime > 0 && hasCommandPoint) return null;')<acquire.index('EnemyController focused = Owner.FocusTarget;')
assert 'if (commandTime <= 0) hasCommandPoint = false;' in pet
assert 'if (!ValidSnapshotTarget()) TargetEnemy = null;' in charge
free=pet[pet.index('public static bool SetFreeFocus'):pet.index('public static EnemyController ExplicitFocus')]
assert 'pet.hasCommandPoint = false;' in free
assert 'Commands.TryConsume' not in free and 'commandTime =' not in free and 'commandMultiplier =' not in free
print('PASS: 8 dodge/basic and captured-target-death priority source contracts (no PlayMode claim)')
