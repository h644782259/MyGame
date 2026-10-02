from pathlib import Path
r=Path(__file__).resolve().parent.parent
read=lambda p:(r/'Assets/Scripts'/p).read_text()
rig=read('Combat/LargeBossRig.cs');boss=read('Combat/LargeExpeditionBoss.cs');state=read('Core/LargeBossPhaseState.cs')
assert 'motion.FirstAngle' in rig and 'motion.SecondAngle' in rig and 'age*(charging' not in rig
assert 'LargeBossMotion.Pose(phase,remaining)' in rig and 'encounter.BeamWorldAngle' in rig
assert 'Quaternion.Euler(0,BeamWorldAngle,0)' in boss
assert 'Remaining = WindupSeconds; BeamAngle = 0;' in state
assert 'Radial beam emitter' in rig and 'float swing=' in rig and '*(1-brace)' in rig
assert 'Color tint=new Color(1,.24f,.12f,.85f)' in boss and 'boss.CanBeSkillInterrupted?new Color' not in boss
assert 'symbol.enabled=!live&&boss.CanBeSkillInterrupted' in boss and 'timer.enabled=!live' in boss
print('PASS: 7 boss motion/actual beam alignment/danger-boundary source contracts')
