"""Read-only original Blender vs shipped FBX composed-localTRS proof.
Requires candidate-samples.json emitted using actual production policy first.
"""
import bpy,json,hashlib,sys,math
from pathlib import Path
from mathutils.kdtree import KDTree
args=sys.argv[sys.argv.index('--')+1:];root,out=map(lambda v:Path(v).resolve(),args[:2]);sys.path.insert(0,str(Path(__file__).parent));from layered_pose import LayeredPose
blend=root/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';fbx=root/'Assets/Resources/BlenderPilot/Vanguard.fbx'
data=json.loads((out/'candidate-samples.json').read_text());samples=data['samples']+json.loads((out/'candidate-stop-samples.json').read_text())['samples'];groups=('Body','Clothes','Armor','Head','Back','Sword');anchors=('Grip','Guard','BladeRoot','Tip','Pommel','Emission')
def capture():
 dg=bpy.context.evaluated_depsgraph_get();parts={}
 for name in groups:
  o=bpy.data.objects['Vanguard_'+name].evaluated_get(dg);m=o.to_mesh();parts[name]=[tuple(o.matrix_world@v.co) for v in m.vertices];o.to_mesh_clear()
 points={n:tuple(bpy.data.objects['Anchor_'+n].matrix_world.translation) for n in anchors};return parts,points
bpy.ops.wm.open_mainfile(filepath=str(blend));sampler=LayeredPose(bpy.data.objects['Vanguard_Rig']);reference=[]
for s in samples:sampler.sample(s);reference.append(capture())
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx));sampler=LayeredPose(next(o for o in bpy.data.objects if o.type=='ARMATURE'));meshError=socketError=0
for s,(expected,sockets) in zip(samples,reference):
 sampler.sample(s);actual,actualSockets=capture()
 for name in groups:
  assert len(actual[name])==len(expected[name])
  for a,b in ((actual[name],expected[name]),(expected[name],actual[name])):
   tree=KDTree(len(b))
   for i,p in enumerate(b):tree.insert(p,i)
   tree.balance();meshError=max(meshError,max(tree.find(p)[2] for p in a))
 for n in anchors:socketError=max(socketError,math.dist(sockets[n],actualSockets[n]))
assert meshError<.0001,meshError
assert socketError<.0001,socketError
result={'samples':len(samples),'meshGroups':6,'sockets':6,'maxVertexErrorM':meshError,'maxSocketErrorM':socketError,'sourceBlendSha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'fbxSha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'productionSamplesSha256':hashlib.sha256((out/'candidate-samples.json').read_bytes()).hexdigest(),'UnityImportVerified':False,'pass':True};(out/'candidate-fbx-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
