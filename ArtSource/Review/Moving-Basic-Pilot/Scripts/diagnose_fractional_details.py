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
 points={n:tuple(bpy.data.objects['Anchor_'+n].matrix_world.translation) for n in anchors}
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
 points.update({'bone:'+b.name:tuple((rig.matrix_world@b.matrix).translation) for b in rig.pose.bones});return parts,points
bpy.ops.wm.open_mainfile(filepath=str(blend));sampler=LayeredPose(bpy.data.objects['Vanguard_Rig']);reference=[]
tests=[(kind,s) for kind in ('layered','Move','Basic','Idle') for s in samples]
def apply(sampler,kind,s):
 if kind=='layered':sampler.sample(s)
 else:sampler.clip(kind,s[{'Move':'move','Basic':'progress','Idle':'idle'}[kind]])
for kind,s in tests:apply(sampler,kind,s);reference.append(capture())
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx));sampler=LayeredPose(next(o for o in bpy.data.objects if o.type=='ARMATURE'));meshError=socketError=0
byKind={kind:{'mesh':0,'socket':0,'bone':0} for kind in ('layered','Move','Basic','Idle')};perTime={};perTimeSocket={}
for (kind,s),(expected,sockets) in zip(tests,reference):
 apply(sampler,kind,s);actual,actualSockets=capture();detail={'index':s['index'],'scenario':s['scenario'],'time':s['time'],'progress':s['progress'],'upperWeight':s['upperWeight']};frameMesh=0;frameSocket=0
 for name in groups:
  assert len(actual[name])==len(expected[name])
  for a,b in ((actual[name],expected[name]),(expected[name],actual[name])):
   tree=KDTree(len(b))
   for i,p in enumerate(b):tree.insert(p,i)
   tree.balance();e=max(tree.find(p)[2] for p in a);meshError=max(meshError,e);frameMesh=max(frameMesh,e)
   if e>byKind[kind]['mesh']:byKind[kind]['mesh']=e;byKind[kind]['meshAt']=dict(detail,group=name)
 for n in anchors:
  e=math.dist(sockets[n],actualSockets[n]);socketError=max(socketError,e);frameSocket=max(frameSocket,e)
  if e>byKind[kind]['socket']:byKind[kind]['socket']=e;byKind[kind]['socketAt']=dict(detail,name=n)
 for n in sockets:
  if n.startswith('bone:'):
   e=math.dist(sockets[n],actualSockets[n])
   if e>byKind[kind]['bone']:byKind[kind]['bone']=e;byKind[kind]['boneAt']=dict(detail,name=n)
 perTime.setdefault(s['scenario']+':'+str(s['index']),{})[kind]=frameMesh
 perTimeSocket.setdefault(s['scenario']+':'+str(s['index']),{})[kind]=frameSocket
def quantile(values,p):
 a=sorted(values);x=(len(a)-1)*p;i=int(x);return a[i]+(a[min(i+1,len(a)-1)]-a[i])*(x-i)
distribution={kind:{metric:{'p50':quantile([v[kind] for v in table.values()],.5),'p95':quantile([v[kind] for v in table.values()],.95),'max':max(v[kind] for v in table.values()),'samples':len(table)} for metric,table in [('vertex',perTime),('socket',perTimeSocket)]} for kind in byKind}
extra=max((v['layered']-max(v['Move'],v['Basic'],v['Idle']),k) for k,v in perTime.items());report={'diagnosticCompleted':True,'strictLayeredThresholdMeters':.0001,'strictLayeredPass':byKind['layered']['mesh']<.0001 and byKind['layered']['socket']<.0001,'byKind':byKind,'perTime':perTime,'perTimeSocket':perTimeSocket,'distributionMeters':distribution,'maxLayeredExcessOverSameTimeUnlayeredEnvelope':extra,'note':'bone means world bone-head position; integer and subframe sampling domains remain separate'};(out/'fractional-fbx-details.json').write_text(json.dumps(report,indent=2));print(json.dumps({'byKind':byKind,'maxExcess':extra}));
