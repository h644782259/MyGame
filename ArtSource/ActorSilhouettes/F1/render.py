"""Production geometry/TRS in Blender: identical camera before/after, no Unity claim.
blender -b -t 2 --python render.py -- before.json after.json output
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];sets=[json.loads(Path(a).read_text()) for a in args[:2]];out=Path(args[2]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=False;s.render.threads_mode='FIXED';s.render.threads=2;s.world.color=(.14,.14,.14)
s.render.resolution_x=1500;s.render.resolution_y=750;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(3,-12,6));cam=bpy.context.object;target=Vector((0,0,1.4));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=12.5;s.camera=cam
for pos,power in [((0,-5,8),1600),((4,4,6),1100)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.size=8;l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=150);bpy.context.object.location.z=-.06
mats={}
def material(c):
 key=tuple(round(x,4) for x in c)
 if key not in mats:
  m=bpy.data.materials.new(str(key));m.diffuse_color=(*key,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*key,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;mats[key]=m
 return mats[key]
for ident in ['Arcanist','Ranger','Summoner','Spirit','Treant']:
 if len(args)>3 and ident not in args[3:]:continue
 for version,cases in zip(['Before','After'],sets):
  items=[];samples=[(-1,0),(0,1),(4,2)] if ident in ['Arcanist','Ranger','Summoner'] else [(3,0),(3,2),(3,3)]
  for i,(tier,pose) in enumerate(samples):
   c=next(c for c in cases if c['name']==ident and c['tier']==tier and c['pose']==pose)
   for p in c['parts']:
    m=bpy.data.meshes.new(p['name']);t=p['triangles'];m.from_pydata([(x+(i-1)*3.8,-z,y) for x,y,z in p['vertices']],[],[t[j:j+3] for j in range(0,len(t),3)]);m.update();o=bpy.data.objects.new(p['name'],m);s.collection.objects.link(o);m.materials.append(material(p['color']));items.append(o)
   bpy.ops.object.text_add(location=((i-1)*3.8-.85,-.85,-.025),rotation=(math.radians(68),0,0));o=bpy.context.object;o.data.body=('Base / idle' if i==0 else 'T1 / moving' if i==1 else 'T4 / action') if ident not in ['Spirit','Treant'] else ['Rank3 / idle','Attack prep','Attack / recall'][i];o.data.size=.21;o.data.materials.append(material((.04,.04,.04)));items.append(o)
  s.render.filepath=str(out/(ident+'-'+version+'.png'));bpy.ops.render.render(write_still=True)
  for o in items:bpy.data.objects.remove(o,do_unlink=True)
