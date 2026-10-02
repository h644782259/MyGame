import bpy,json,hashlib,math,traceback,re
from pathlib import Path
base=Path(__file__).parent;root=base/'extracted/Emberfall-Android';resource=root/'Assets/Resources/BlenderPilot';report={'blender':bpy.app.version_string,'completed':False,'UnityImportValidated':False,'fbx':{},'blend':{}}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def finite(seq,label):
 if not all(math.isfinite(float(x)) for x in seq):raise ValueError('nonfinite '+label)
def meshes():
 dg=bpy.context.evaluated_depsgraph_get();result={}
 for o in bpy.data.objects:
  for row in o.matrix_world:finite(row,o.name+' transform')
  if o.type!='MESH':continue
  ev=o.evaluated_get(dg);m=ev.to_mesh()
  for v in m.vertices:finite(ev.matrix_world@v.co,o.name+' evaluated vertex');finite(v.normal,o.name+' normal')
  result[o.name]={'vertices':len(m.vertices),'polygons':len(m.polygons),'uvLayers':len(m.uv_layers)};ev.to_mesh_clear()
 return result
def textures():
 result={}
 for m in bpy.data.materials:
  if not m.use_nodes:continue
  links=[]
  for n in m.node_tree.nodes:
   if n.type!='TEX_IMAGE' or not n.image:continue
   i=n.image;p=Path(bpy.path.abspath(i.filepath));name=p.name;expected=resource/name
   raw=bytes(i.packed_file.data) if i.packed_file else p.read_bytes();digest=hashlib.sha256(raw).hexdigest()
   assert expected.is_file() and digest==sha(expected),(m.name,i.name,p)
   links.append({'image':i.name,'resourceFile':name,'sha256':digest,'colorspace':i.colorspace_settings.name,'size':list(i.size),'destinations':[(l.to_node.name,l.to_socket.name) for l in m.node_tree.links if l.from_node==n]})
  result[m.name]={'textureNodes':links,'allLinks':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]}
 return result
try:
 assert bpy.app.version==(4,3,2)
 files=sorted(resource.glob('*.fbx'));assert len(files)==5
 for p in files:
  bpy.ops.wm.read_factory_settings(use_empty=True);assert bpy.ops.import_scene.fbx(filepath=str(p))=={'FINISHED'}
  row={'sha256':sha(p),'meshes':meshes(),'materialLinks':textures()};assert row['meshes']
  if p.name=='Vanguard.fbx':
   groups={'Vanguard_'+n for n in ['Body','Clothes','Armor','Head','Back','Sword']};assert set(row['meshes'])==groups
   rigs=[o for o in bpy.data.objects if o.type=='ARMATURE'];assert len(rigs)==1;rig=rigs[0];assert len(rig.data.bones)==19
   row['bones']={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
   code=(root/'Assets/Scripts/Combat/BlenderPilotVisual.cs').read_text()
   names=re.findall(r'"([^"]+)"',re.search(r'BoneNames = \{([^}]+)\}',code)[1]);parents=[int(v) for v in re.search(r'BoneParents = \{([^}]+)\}',code)[1].split(',')]
   expected={n:names[parents[i]] if parents[i]>=0 else None for i,n in enumerate(names)};assert row['bones']==expected
   row['boneNamesAndParentsMatchPackagedRuntime']=True
   required={'Anchor_'+n for n in ['Pommel','Grip','Guard','BladeRoot','Tip']};allanchors=required|{'Anchor_Emission'}
   actual={o.name for o in bpy.data.objects if o.name.startswith('Anchor_')};assert actual==allanchors
   row['anchors']={}
   for name in sorted(actual):
    o=bpy.data.objects[name];assert o.parent==rig and o.parent_type=='BONE' and o.parent_bone=='Hand.R';row['anchors'][name]={'parent':o.parent.name,'bone':o.parent_bone,'runtimeRequired':name in required}
   actions={}
   for a in bpy.data.actions:
    name=a.name[a.name.index('Pilot_'):];assert name not in actions;actions[name]=a
   assert set(actions)=={'Pilot_'+n for n in ['Idle','Move','Basic','Hit','Skill']}
   row['clips']={}
   for name,a in actions.items():
    for curve in a.fcurves:
     for key in curve.keyframe_points:finite(key.co,name+' key');finite(key.handle_left,name+' left handle');finite(key.handle_right,name+' right handle')
    lo,hi=a.frame_range;assert hi>lo;rig.animation_data.action=a
    for t in [float(lo),float((lo+hi)/2),float(hi)]:bpy.context.scene.frame_set(int(t),subframe=t%1);meshes()
    row['clips'][name]={'range':[lo,hi],'channels':len(a.fcurves),'keyframes':sum(len(f.keyframe_points) for f in a.fcurves),'finiteDeformationSamples':'start/mid/end'}
  report['fbx'][p.name]=row;print('READABLE FBX',p.name,flush=True)
 blends=sorted((root/'ArtSource/BlenderPilot').glob('*.blend'));assert len(blends)==2
 for p in blends:
  bpy.ops.wm.open_mainfile(filepath=str(p));row={'sha256':sha(p),'meshes':meshes(),'materialLinks':textures(),'packedImages':{}}
  for i in bpy.data.images:
   if not i.packed_file:continue
   raw=bytes(i.packed_file.data);expected=resource/(i.name+'.png');assert expected.is_file() and raw==expected.read_bytes()
   row['packedImages'][i.name]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'colorspace':i.colorspace_settings.name,'matchesPackagedPNG':True}
  assert set(row['packedImages'])=={'EmberfallPilot_Atlas','EmberfallPilot_MetallicSmoothness'};report['blend'][p.name]=row;print('READABLE BLEND',p.name,flush=True)
 report['completed']=True;report['pass']=True
except Exception:report['error']=traceback.format_exc();report['pass']=False;raise
finally:(base/'blender-readability.json').write_text(json.dumps(report,indent=2)+'\n')
