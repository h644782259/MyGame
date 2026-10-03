"""One combined production geometry sample. Blender TRS reconstruction, not Unity rendering."""
import bpy,json,sys,math,gzip
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
args=sys.argv[sys.argv.index('--')+1:];inp=Path(args[0]);allcases=json.loads(gzip.decompress(inp.read_bytes()) if inp.suffix=='.gz' else inp.read_bytes());out=Path(args[2]);out.parent.mkdir(parents=True,exist_ok=True)
ep=Path(args[1]);effectcases=json.loads(gzip.decompress(ep.read_bytes()) if ep.suffix=='.gz' else ep.read_bytes());effects=effectcases[3]['objects']
bp=Path(args[3]);oldcases=json.loads(gzip.decompress(bp.read_bytes()) if bp.suffix=='.gz' else bp.read_bytes());oldfx=oldcases[3]['objects']
cases=[]
for tier in [0,4]:
 for hero in range(4):
  skill=tier==4 and hero!=0;phase=(.28 if hero==2 else .52) if tier==4 else 1
  cases.append(next(c for c in allcases if c['hero']==hero and c['tier']==tier and c['skill']==skill and abs(c['phase']-phase)<.0001))
reference=next(c for c in allcases if c['hero']==1 and c['tier']==4 and c['skill'])
cases.extend([dict(reference,comparison='Old height 2.2 / hat crossing'),dict(reference,comparison='New height 2.6 / steady envelope')])
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=False;s.render.threads_mode='FIXED';s.render.threads=2;s.world.color=(.14,.14,.14)
s.render.resolution_x=1800;s.render.resolution_y=1800;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(8,-16,13));cam=bpy.context.object;target=Vector((0,0,1.1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=16;s.camera=cam
for pos,power in [((0,-6,10),2000),((4,5,9),1500)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.size=8;l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=150);bpy.context.object.location.z=-.08
mats={}
def material(c):
 key=tuple(round(x,4) for x in c)+( (1,) if len(c)==3 else () )
 if key not in mats:
  m=bpy.data.materials.new(str(key));m.diffuse_color=key;m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=key;m.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value=key[3];m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;mats[key]=m
 return mats[key]
def overlay(text,x,y,size):
 bpy.ops.object.text_add();o=bpy.context.object;o.parent=cam;o.location=(x,y,-1);o.rotation_euler=(0,0,0);o.data.body=text;o.data.align_x='CENTER';o.data.size=size
 m=bpy.data.materials.new('Overlay ink');m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');e.inputs['Color'].default_value=(.025,.03,.04,1);oout=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(e.outputs[0],oout.inputs['Surface']);o.data.materials.append(m)
 o.visible_shadow=False
height=cam.data.ortho_scale*s.render.resolution_y/s.render.resolution_x
overlay('Actual combined F1 / F3 / F6 geometry - Blender reconstruction, NOT Unity',0,height/2-.6,.26)
overlay('Base idle above | T4 + highest fashion: sword contact, caster release, arrow draw, spirit cast below',0,height/2-1.0,.19)
bpy.context.view_layer.update()
for index,c in enumerate(cases):
 dx=(index%4-1.5)*3.35;dy=5.2 if index<4 else -.4
 if index>=8:dx=(index-8-.5)*4.1;dy=-6.4
 shownfx=oldfx if index==8 else effects
 ox=dx*.894427191-dy*.4472135955;oy=dx*.4472135955+dy*.894427191
 def world(v):x,y,z=v;return (x+ox,-z+oy,y)
 for p in c['parts']+[dict(name='Actual protection',vertices=e['vertices'],triangles=e['triangles'],color=[1,.8,.3,e['opacity']]) for e in shownfx]:
  m=bpy.data.meshes.new(p['name']);t=p['triangles'];m.from_pydata([world(v) for v in p['vertices']],[],[t[j:j+3] for j in range(0,len(t),3)]);m.update();o=bpy.data.objects.new(p['name'],m);s.collection.objects.link(o);m.materials.append(material(p['color']))
 for line in c['lines']:
  curve=bpy.data.curves.new('Actual line positions','CURVE');curve.dimensions='3D';curve.bevel_depth=line['width']/2;curve.bevel_resolution=0;poly=curve.splines.new('POLY');poly.points.add(len(line['points'])-1)
  for point,v in zip(poly.points,line['points']):point.co=(*world(v),1)
  o=bpy.data.objects.new('Bowstring reconstruction',curve);s.collection.objects.link(o);curve.materials.append(material(line['color']))
 uv=world_to_camera_view(s,cam,Vector((ox,oy,0)))
 overlay(c.get('comparison') or c['name']+(' / base idle' if c['tier']==0 else ' / T4 '+('arrow draw .28' if c['hero']==2 else 'skill .52' if c['skill'] else 'contact .52')),(uv.x-.5)*cam.data.ortho_scale,(uv.y-.5)*height-.42,.20)

assert all(abs(m.node_tree.nodes['Principled BSDF'].inputs['Alpha'].default_value-k[3])<1e-6 for k,m in mats.items())
print('Actual BSDF Alpha values:',sorted(set(k[3] for k in mats)))
s.render.filepath=str(out);bpy.ops.render.render(write_still=True)
print('PASS eight combined body/protection and real weapon pose samples, actual palette and line positions; Blender reconstruction NOT Unity')
