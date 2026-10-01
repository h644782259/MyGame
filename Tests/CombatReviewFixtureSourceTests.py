from pathlib import Path
r=Path(__file__).resolve().parents[1]
s=(r/'Assets/Editor/CombatReviewFixture.cs').read_text()
assert s.index('SessionState.SetString(SaveKey,') < s.index('EditorSceneManager.OpenScene(')
assert 'SaveFilePath).StartsWith(expected+Path.DirectorySeparatorChar' in s
assert 'state!=PlayModeStateChange.EnteredEditMode' in s and 'PreviousSave' in s
assert 'rows>=100000' in s and 'started>=180' in s
assert 'CombatReviewEvents.Observed-=Record' in s
assert 'observed_enemy_hp_delta' in s and 'may aggregate several hits' in s
assert 'float.MaxValue' not in s and 'TakeDamage(' not in s
print('PASS: 7 review isolation, bounded logging and observation-label contracts')
