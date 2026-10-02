"""Compare frozen/current authored rig, action curves and all socket trajectories.
Blender source checks complement validate_fbx_motion.py; never Unity acceptance.
Run: blender -b --python-exit-code 1 --threads 1 --python SCRIPT -- BEFORE_BLEND AFTER_BLEND REPORT
"""
import bpy,json,sys,math,hashlib
from pathlib import Path
from collections import Counter
before,after,report=map(Path,sys.argv[sys.argv.index('--')+1:])
CLIPS=('Idle','Move','Basic','Hit','Skill');GROUPS=('Body','Clothes','Armor','Head','Back','Sword');SOCKETS=('Grip','Guard','BladeRoot','Tip','Pommel','Emission')
def matrix(m):return [[float(x) for x in row] for row in m]
def capture(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));rig=bpy.data.objects['Vanguard_Rig'];scene=bpy.context.scene
 bones={b.name:{'matrix':matrix(b.matrix_local),'parent':b.parent.name if b.parent else None,'length':b.length} for b in rig.data.bones}
 curves={};sockets={};parts={}
 for name in CLIPS:
  a=bpy.data.actions['Pilot_'+name];rig.animation_data.action=a
  curves[name]={'range':list(a.frame_range),'curves':[(c.data_path,c.array_index,[(list(k.co),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]}
  sockets[name]=[]
  for frame in range(int(a.frame_range.x),int(a.frame_range.y)+1):
   scene.frame_set(frame);sockets[name].append({s:matrix(bpy.data.objects['Anchor_'+s].matrix_world) for s in SOCKETS})
 for name in GROUPS:
  o=bpy.data.objects['Vanguard_'+name];o.data.calc_loop_triangles()
  assert len(o.data.materials)==1 and o.data.materials[0].name=='Pilot_Atlas_Standard'
  assert o.data.uv_layers and all(math.isfinite(x) for v in o.data.vertices for x in v.co)
  assert all(1<=len(v.groups)<=2 and abs(sum(g.weight for g in v.groups)-1)<1e-6 for v in o.data.vertices)
  parts[name]={'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'rest_geometry_sha256':hashlib.sha256(json.dumps({'positions':[list(v.co) for v in o.data.vertices],'faces':[list(f.vertices) for f in o.data.polygons]}).encode()).hexdigest()}
 # The same two packed atlas images must be present, with the same content.
 images={i.name:hashlib.sha256(bytes(i.packed_file.data)).hexdigest() for i in bpy.data.images if i.packed_file}
 return dict(bones=bones,curves=curves,sockets=sockets,parts=parts,images=images)
a=capture(before);b=capture(after)
for group in ('Body','Head','Sword'):assert a['parts'][group]==b['parts'][group],('unowned geometry changed',group)
assert a['bones']==b['bones'],'rest skeleton changed'
assert a['curves']==b['curves'],'action curves/timing changed'
assert a['images']==b['images'] and len(b['images'])==2,'packed shared atlas changed'
max_socket=max(abs(x-y) for name in CLIPS for old,new in zip(a['sockets'][name],b['sockets'][name]) for key in SOCKETS for ro,rn in zip(old[key],new[key]) for x,y in zip(ro,rn))
assert max_socket<1e-7,('socket/hand trajectory changed',max_socket)
# Closed mantle topology: every edge belongs to exactly two polygons.
o=bpy.data.objects['Vanguard_Back'];edges=Counter(tuple(sorted(e)) for f in o.data.polygons for e in f.edge_keys)
assert edges and all(v==2 for v in edges.values()),'open or nonmanifold cloth edge'
assert len(o.data.vertices)==108,'bounded 9x6 two-sided cloth grid changed'
# Closed sleeve components bridge shoulder and elbow instead of split rods.
o=bpy.data.objects['Vanguard_Clothes'];adj={v.index:set() for v in o.data.vertices}
for e in o.data.edges:adj[e.vertices[0]].add(e.vertices[1]);adj[e.vertices[1]].add(e.vertices[0])
unseen=set(adj);components=[]
while unseen:
 todo=[unseen.pop()];comp=set(todo)
 while todo:
  for j in adj[todo.pop()]:
   if j in unseen:unseen.remove(j);comp.add(j);todo.append(j)
 components.append(comp)
continuous=[]
for comp in components:
 names={o.vertex_groups[g.group].name for i in comp for g in o.data.vertices[i].groups if g.weight>0}
 if 'Spine' not in names:continue
 assert len(comp)==80 and any(n.startswith('UpperArm.') for n in names) and any(n.startswith('Forearm.') for n in names) and any(n.startswith('Hand.') for n in names)
 edges=Counter(tuple(sorted(e)) for f in o.data.polygons if f.vertices[0] in comp for e in f.edge_keys)
 assert all(v==2 for v in edges.values()),'open sleeve joint'
 continuous.append(sorted(names))
assert len(continuous)==2
result=dict(pass_=True,unity_validated=False,bones=len(b['bones']),clips=list(CLIPS),frames_sampled=sum(len(v) for v in b['sockets'].values()),max_socket_matrix_error=max_socket,action_curves_exactly_equal=True,packed_images_exactly_equal=True,continuous_closed_sleeves=continuous,closed_mantle_vertices=108,before=a['parts'],after=b['parts'],source_hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in (before,after)})
report.write_text(json.dumps(result,indent=2)+'\n');print('REFINEMENT_CONTRACT_PASS',json.dumps(result))
