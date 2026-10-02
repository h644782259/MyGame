from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):return (root/'Assets/Scripts'/path).read_text()
hubs=read('World/WorldBuilder.Hubs.cs');session=read('Core/GameSession.Hubs.cs');idle=read('World/HubNpcIdle.cs')
assert 'HubSettlementPlan.Building(hub,index)' in hubs and 'HubSettlementPlan.RegisterTownNavigation(hub)' in hubs
assert 'HubSettlementPlan.Npc(index)' in session and 'HubSettlementPlan.RegisterNpcNavigation(index)' in hubs
assert hubs.count('cameraOccluder:true')>=6
assert 'Merchant stocked shelf' in hubs and 'Forged anvil face' in hubs and 'Turning exchange star chart' in hubs
assert 'VisualSurface.Skin' in hubs and 'VisualSurface.Cloth' in hubs and 'VisualSurface.Wood' in hubs
assert 'BuildWaterBankDetail(lowland,r,stream)' in read('World/WorldBuilder.cs')
assert 'if(Time.deltaTime<=0)return' in idle and 'Time.time*' not in idle and 'new GameObject' not in idle
assert 'WorldTraversal.Add' not in hubs[hubs.index('private static void BuildTownGroundDetail'):hubs.index('private static void BuildHubNpcs')]
print('PASS: shared building footprints, NPC registration, role props, pause-safe idle, cosmetic ground detail')
