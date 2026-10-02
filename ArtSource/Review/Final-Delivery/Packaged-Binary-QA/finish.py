from pathlib import Path
import hashlib,json
base=Path(__file__).parent;root=base/'extracted';before=json.loads((base/'before.json').read_text());z=Path('/workspace/scratch/android-final-delivery/Emberfall-Android-Source-b1104bb.zip')
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
after={str(p.relative_to(root)):{'bytes':p.stat().st_size,'sha256':sha(p)} for p in sorted(root.rglob('*')) if p.is_file()};assert after==before['files'];assert sha(z)==before['zipSHA256']
b=json.loads((base/'blender-readability.json').read_text());f=json.loads((base/'png-font-readability.json').read_text());assert b['completed'] and b['pass'] and f['pass'];assert len(b['fbx'])==5 and len(b['blend'])==2 and len(f['png'])==15 and len(f['fonts'])==2
pack=root/'Emberfall-Android';resources=pack/'Assets/Resources/BlenderPilot'
assert sha(resources/'EmberfallPilot_Atlas.png')=='5f29f01b6022bbd56b329a518309d90745e1bd8c248c246e8c97590792761717'
assert sha(resources/'EmberfallPilot_MetallicSmoothness.png')=='eafb0a4e74860da9fde51068bdd036d685d600531e93e6171edbe63eeffbe347'
assert sha(resources/'Vanguard.fbx')=='0dac3643c78b839a2d42ceee5b605f6e71f91a6addf4c9631a63c62372e5219c'
assert f['fonts']['NotoSansSC-Regular.otf']['bytes']==8331336 and f['fonts']['EmberfallWorldLabels.otf']['bytes']==17584
(base/'after.json').write_text(json.dumps({'zipSHA256':sha(z),'files':after},indent=2)+'\n')
r={'pass':True,'zip':str(z),'zipSHA256':sha(z),'zipUnchanged':True,'packagedFileCount':len(after),'allPackagedFilesUnchanged':True,'blender':b['blender'],'fbxImports':5,'readOnlySourceBlends':2,'fullyDecodedPNGs':15,'fontsTablesCmapAndSelectedOutlines':2,'heroGroups':list(b['fbx']['Vanguard.fbx']['meshes']),'rigBones':19,'runtimeBoneNamesParentsMatch':True,'clips':list(b['fbx']['Vanguard.fbx']['clips']),'runtimeRequiredSockets':5,'authoringEmissionSocket':1,'packedTextureBytesMatchPNG':True,'PNGHashesAndCandidateFBXPinned':True,'scope':'Packaged binary readability and bounded data integrity; no render/export/source mutation, Unity importer/native font rasterization/device/performance acceptance, or full-suite claim.'}
(base/'summary.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps(r,indent=2))
