"""Town grouping contract; runtime fade policy is tested with BuildingOcclusionGroup."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/World/WorldBuilder.Hubs.cs').read_text()
a=s.index('for(int index=0;index<HubSettlementPlan.BuildingCount;index++)')
b=s.index('\n   if(quarry)\n',a)
block=s[a:b]
assert 'building.transform.SetParent(parent,false)' in block
assert block.count('Primitive(building.transform,')==8 and 'Primitive(parent,' not in block
assert 'BuildingOcclusionGroup.Configure(building.transform,p,new Vector2(HubSettlementPlan.BuildingWidth,HubSettlementPlan.BuildingWidth))' in block
assert block.index('Arcade wall pilaster')<block.index('BuildingOcclusionGroup.Configure')
assert 'building.transform.localPosition' not in block and 'building.transform.localScale' not in block
assert 'Weathered wall footing' in block and 'new Vector3(.045f,.3f,1.2f)' in block
assert 'WorldTraversal' not in block
assert s.count('HubSettlementPlan.RegisterTownNavigation(hub);')==1
print('PASS: eight town building part recipes grouped per building; shared solid dimensions and zero group transform preserved')
