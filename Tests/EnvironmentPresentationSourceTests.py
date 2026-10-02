from pathlib import Path
root=Path(__file__).resolve().parents[1]
r=lambda f:(root/'Assets/Scripts'/f).read_text()
w=r('World/WorldBuilder.cs');linked=r('World/WorldBuilder.LinkedRooms.cs');e=r('World/WorldBuilder.Environment.cs');v=r('Combat/ProceduralVisuals.cs')
for literal in ['new Color(.055f,.25f,.33f),false,VisualSurface.Water','new Color(.43f,.32f,.215f),false,VisualSurface.Wood','new Color(.24f,.22f,.21f),false,VisualSurface.Wood','new Color(.2f,.37f,.32f),false,VisualSurface.Foliage','new Color(.29f,.47f,.48f),false,VisualSurface.Cloth']:
    assert literal in w
assert 'new Color(.06f,.27f,.35f),false,VisualSurface.Water' in linked
assert 'surface == VisualSurface.Water ? .82f' in v
assert 'ApplyEnvironmentLighting(dungeon,hub,light,rim);' in w
assert 'if(!dungeon)BuildHubLightPools(root.transform,hub);' in w
assert 'BuildPortalFocus(parent,r,p);' in w
assert 'EnvironmentLightProfile.TownAccentLights' in e
assert 'new Vector3(.15f,.009f,.20f)' in e
for forbidden in ['WorldTraversal.','AddComponent<Collider','AddComponent<Rigidbody','TakeDamage','CameraOcclusionSurface']:
    assert forbidden not in e
assert 'light.shadows=LightShadows.None' in w and 'light.renderMode=LightRenderMode.ForceVertex' in w
print('PASS: explicit environment materials, bounded light pools, flush portal focus source contracts')
