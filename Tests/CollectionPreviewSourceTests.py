from pathlib import Path
r=Path(__file__).resolve().parents[1]
read=lambda f:(r/'Assets/Scripts/UI'/f).read_text()
p=read('CollectionModelPreview.cs');ui=read('GameUI.CollectionPreview.cs');base=read('GameUI.cs')
assert 'CombatModel.Hero(' in p and 'model.ApplyEquipment(' in p and 'model.ApplyFashion(' in p
assert 'finally {UnityEngine.Random.state=random;}' in p
assert 'c.enabled=false' in p and 'behaviour.enabled=false' in p
assert 'camera.enabled=false' in p and 'camera.cullingMask=1<<PreviewLayer' in p and 'light.cullingMask=1<<PreviewLayer' in p
assert 'new RenderTexture(384,480,16)' in p and 'state.ShouldRender(true,Time.frameCount)' in p and 'texture.IsCreated()' in p
assert 'texture.Release()' in p and 'stage.SetActive(false)' in p
assert 'ReleaseCollectionPreview();' in base and 'private void OnDisable(){ReleaseCollectionPreview();}' in ui
assert 'EquipmentComparisonPresentation.Receipt(reward)' in ui
assert 'DrawChestRewardModel(' in read('GameUI.Rewards.cs') and 'DrawChestRewardModel(' in read('GameUI.MobileRewards.cs')
assert 'EquipmentComparisonPresentation.Changes(' in base and 'EquipmentComparisonPresentation.Changes(' in read('GameUI.MobileInventory.cs')
assert 'DrawPersistentMechanismDetail(' in base and 'CalcHeight(' in read('GameUI.EquipmentComparison.cs')
assert 'owned&&!current' in ui and 'owned&&!equipped' in ui
assert 'StrongestFashion(slot)' in ui and '属性来源' in ui
assert '.EquipFashion' not in p and 'ProgressionService' not in p and '.Save(' not in p
print('PASS: 14 real-model, isolation, cleanup, bounded-render and persistent-comparison wiring contracts')
