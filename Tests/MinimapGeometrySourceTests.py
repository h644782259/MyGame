from pathlib import Path
r=Path(__file__).resolve().parents[1]
s=(r/'Assets/Scripts/UI/GameUI.Minimap.cs').read_text()
assert 'terrainMapRevision != WorldTraversal.Revision' in s
assert 'terrainMapRadius != radius' in s
assert 'WorldTraversal.IsOpenWater(point, .16f)' in s
assert 'terrainMap.SetPixels(terrainMapPixels)' in s
assert 'terrainMap.Apply(false, false)' in s
assert 'if (!session.InDungeon && session.CurrentHub == 0)' in s
assert 'Vector3[] river=' not in s
assert 'new Vector3(12,0,-3)' not in s
assert 'terrainMapKey' not in s
assert 'if (terrainMap == null)\n                    terrainMap = new Texture2D' in s
mobile=(r/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
world=(r/'Assets/Scripts/World/WorldBuilder.cs').read_text()
assert 'MapDot(map,new Vector3(0,0,11),jade,5*TouchRatio)' in mobile
assert 'MapDot(map,new Vector3(0,0,-16),jade,5*TouchRatio)' in mobile
portal=world[world.index('private static void Portal('):world.index('private static void Crystal(')]
assert 'glow = r.Material(new Color(.32f, .91f, .77f), true)' in portal
print('PASS: 13 minimap cache/geometry/gate source contracts (no rendered map tested)')
