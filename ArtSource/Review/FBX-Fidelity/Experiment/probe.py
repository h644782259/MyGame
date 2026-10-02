"""Scratch only: same source, denser FBX bake; reports strict failures as failures."""
import bpy,sys,json,hashlib,math,time,traceback
import numpy as np
from pathlib import Path
from mathutils.kdtree import KDTree
out=Path(__file__).parent;sys.path.insert(0,str(out));from layered_pose import LayeredPose
repo=Path('/workspace/emberfall_android_player_journey');blend=repo/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';original=repo/'Assets/Resources/BlenderPilot/Vanguard.fbx';inputs=Path('/workspace/scratch/vanguard-moving-basic-baseline')
CLIPS=('Idle','Move','Basic','Hit','Skill');GROUPS=('Body','Clothes','Armor','Head','Back','Sword');ANCHORS=('Grip','Guard','BladeRoot','Tip','Pommel','Emission');THRESHOLD=1e-4
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report={'blender':bpy.app.version_string,'sourceBlendSha256':sha(blend),'baselineFBXSha256':sha(original),'thresholdM':THRESHOLD,'UnityValidated':False,'steps':{},'completed':False}
mixed='--mixed' in sys.argv
path=out/('mixed-report.json' if mixed else 'report.json')
def save():path.write_text(json.dumps(report,indent=2)+'\n')
def capture():
 dg=bpy.context.evaluated_depsgraph_get();parts={}
 for n in GROUPS:
  o=bpy.data.objects['Vanguard_'+n].evaluated_get(dg);m=o.to_mesh();arr=np.empty((len(m.vertices),3),dtype=np.float32);m.vertices.foreach_get('co',arr.ravel());mat=np.array(o.matrix_world,dtype=np.float32);parts[n]=arr@mat[:3,:3].T+mat[:3,3];o.to_mesh_clear()
 anchors={n:tuple(bpy.data.objects['Anchor_'+n].matrix_world.translation) for n in ANCHORS}
 return parts,anchors

def apply(kind,value,sampler,rig,actions):
 if kind=='layered':sampler.sample(value);return
 for b in rig.pose.bones:b.rotation_mode=sampler.modes[b.name]
 a=actions[kind];rig.animation_data.action=a;frame=a.frame_range.x+value;bpy.context.scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()

def inventory():
 rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');actions={n:next(a for a in bpy.data.actions if a.name.endswith('Pilot_'+n)) for n in CLIPS}
 return {'bones':{b.name:b.parent.name if b.parent else None for b in rig.data.bones},'actions':{n:{'range':list(a.frame_range),'channels':len(a.fcurves),'keys':sum(len(c.keyframe_points) for c in a.fcurves)} for n,a in actions.items()},'meshVertices':{n:len(bpy.data.objects['Vanguard_'+n].data.vertices) for n in GROUPS},'anchors':{n:{'parent':bpy.data.objects['Anchor_'+n].parent.name,'parentBone':bpy.data.objects['Anchor_'+n].parent_bone} for n in ANCHORS}}

try:
 bpy.ops.wm.open_mainfile(filepath=str(blend));rig=bpy.data.objects['Vanguard_Rig'];sampler=LayeredPose(rig);actions={n:next(a for a in bpy.data.actions if a.name.endswith('Pilot_'+n)) for n in CLIPS};report['sourceInventory']=inventory()
 tests=[]
 for n,a in actions.items():
  last=int(a.frame_range.y-a.frame_range.x)
  # Every 1/16 source frame includes all integer and all proposed bake midpoints.
  division=32 if mixed and n=='Basic' else 16
  grid=[i/division for i in range(last*division+1)];off=[i+v for i in range(last) for v in (.137,.731)]
  for frame in sorted(set(grid+off)):tests.append((n,frame,{'frame':frame,'domain':'integer' if frame.is_integer() else 'fractional'}))
 for filename in ['candidate-samples.json','candidate-stop-samples.json']:
  data=json.loads((inputs/filename).read_text())
  for s in data['samples']:tests.append(('layered',s,{'scenario':s['scenario'],'index':s['index'],'time':s['time'],'progress':s['progress']}))
 report['inputs']={n:sha(inputs/n) for n in ['candidate-samples.json','candidate-stop-samples.json']};report['sampleCounts']={n:sum(t[0]==n for t in tests) for n in (*CLIPS,'layered')};report['denseDomain']='Each clip every 1/16 source frame plus .137 and .731 offsets within every integer interval; endpoints included. Layered exact prior 288 policy samples.';save()
 refs=[]
 for i,(kind,value,meta) in enumerate(tests):
  apply(kind,value,sampler,rig,actions);refs.append(capture())
  if i%1000==0:print('SOURCE',i,len(tests),flush=True)
 print('REFERENCE COMPLETE',len(refs),flush=True)
 steps=[('baseline',None),('step0.5',.5),('step0.25',.25)]
 if '--step125' in sys.argv:steps=[('step0.125',.125)]
 if mixed:steps=[('step-mixed',1)]
 for label,step in steps:
  start=time.monotonic();fbx=original if step is None else out/(label+'.fbx')
  if step is not None:
   bpy.ops.wm.open_mainfile(filepath=str(blend));rig=bpy.data.objects['Vanguard_Rig'];rig.animation_data.action=bpy.data.actions['Pilot_Idle'];bpy.context.scene.frame_set(0)
   bpy.ops.object.select_all(action='DESELECT')
   for o in bpy.data.objects:
    if o==rig or o.name.startswith('Vanguard_') and o.type=='MESH' or o.name.startswith('Anchor_'):o.select_set(True)
   bpy.context.view_layer.objects.active=rig
   from io_scene_fbx import export_fbx_bin
   native=export_fbx_bin.fbx_animations_do;calls=[];schedule={'Idle':1,'Move':1,'Basic':.0625,'Hit':.5,'Skill':.5}
   def per_clip(scene_data,ref_id,*args,**kwargs):
    assert isinstance(ref_id,tuple) and len(ref_id)==2
    act=ref_id[1];name=act.name.removeprefix('Pilot_');assert name in schedule,name
    selected=schedule[name];calls.append({'action':act.name,'step':selected})
    return native(scene_data._replace(settings=scene_data.settings._replace(bake_anim_step=selected)),ref_id,*args,**kwargs)
   if mixed:export_fbx_bin.fbx_animations_do=per_clip
   try:
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,bake_anim_step=step,path_mode='STRIP')
   finally:export_fbx_bin.fbx_animations_do=native
   if mixed:
    assert len(calls)==5 and {c['action'] for c in calls}=={'Pilot_'+n for n in CLIPS}
    report['perClipExportCalls']=calls;report['additionalBasicDomain']='Every 1/32 source frame, including midpoints of .0625 bake intervals (400 additional samples); all original4523 samples retained.';save()
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(fbx));rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');sampler=LayeredPose(rig);actions={n:next(a for a in bpy.data.actions if a.name.endswith('Pilot_'+n)) for n in CLIPS};inv=inventory()
  item={'bytes':fbx.stat().st_size,'byteRatio':fbx.stat().st_size/original.stat().st_size,'sha256':sha(fbx),'inventory':inv,'maxima':{n:{'vertexM':0,'socketM':0,'integerVertexM':0,'integerSocketM':0,'samples':0} for n in (*CLIPS,'layered')},'completed':False};report['steps'][label]=item;save()
  assert len(rig.data.bones)==19 and inv['meshVertices']==report['sourceInventory']['meshVertices']
  for index,((kind,value,meta),(expect,anchors)) in enumerate(zip(tests,refs)):
   apply(kind,value,sampler,rig,actions);actual,actualAnchors=capture();stat=item['maxima'][kind];stat['samples']+=1;frameVertex=0
   for n in GROUPS:
    for a,b in ((actual[n],expect[n]),(expect[n],actual[n])):
     tree=KDTree(len(b))
     for j,p in enumerate(b):tree.insert(p,j)
     tree.balance();e=max(tree.find(p)[2] for p in a);frameVertex=max(frameVertex,e)
     if e>stat['vertexM']:stat['vertexM']=e;stat['vertexAt']=dict(meta,mesh=n)
   frameSocket=0
   for n in ANCHORS:
    e=math.dist(anchors[n],actualAnchors[n]);frameSocket=max(frameSocket,e)
    if e>stat['socketM']:stat['socketM']=e;stat['socketAt']=dict(meta,anchor=n)
   if meta.get('domain')=='integer':stat['integerVertexM']=max(stat['integerVertexM'],frameVertex);stat['integerSocketM']=max(stat['integerSocketM'],frameSocket)
   if index%1000==0:print(label,index,len(tests),flush=True);save()
  item['strictPass']=all(max(x['vertexM'],x['socketM'])<THRESHOLD for x in item['maxima'].values());item['completed']=True;item['elapsedSeconds']=time.monotonic()-start;save();print('RESULT',label,json.dumps({k:v for k,v in item.items() if k!='inventory'}),flush=True)
 report['sourceFilesUnchanged']=sha(blend)==report['sourceBlendSha256'] and sha(original)==report['baselineFBXSha256'];report['completed']=True;save()
except Exception:
 report['error']=traceback.format_exc();save();raise
