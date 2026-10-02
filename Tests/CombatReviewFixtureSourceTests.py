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

assert 'CombatReviewConfigurations.CreateAll()' in s
assert 'CombatReviewBuildSetup.Apply(game.Progression,config,expected)' in s
setup=(r/'Assets/Editor/CombatReviewBuildSetup.cs').read_text()
assert 'service.Profile.level!=1' in setup and 'Path.GetFullPath(service.SaveDirectory)!=Path.GetFullPath(isolatedDirectory)' in setup
assert 'service.CollectLoot(item)' in setup and 'service.Equip(item.id)' in setup
print('PASS: complete growth catalog and shared isolated Editor setup wiring')
