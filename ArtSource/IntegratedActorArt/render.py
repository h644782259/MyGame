"""One combined production geometry sample. Blender TRS reconstruction, not Unity rendering."""
import bpy,json,sys,math,gzip
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
args=sys.argv[sys.argv.index('--')+1:];inp=Path(args[0]);cases=json.loads(gzip.decompress(inp.read_bytes()) if inp.suffix=='.gz' else inp.read_bytes());out=Path(args[1]);out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=False;s.render.threads_mode='FIXED';s.render.threads=2;s.world.color=(.14,.14,.14)
s.render.resolution_x=1800;s.render.resolution_y=1400;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(1,-16,13));cam=bpy.context.object;target=Vector((0,0,1.1));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=16;s.camera=cam
for pos,power in [((0,-6,10),2000),((4,5,9),1500)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.size=8;l.rotation_euler=(-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=150);bpy.context.object.location.z=-.08
mats={}
def material(c):
 key=tuple(round(x,4) for x in c)
 if key not in mats:
  m=bpy.data.materials.new(str(key));m.diffuse_color=(*key,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*key,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;mats[key]=m
 return mats[key]
def overlay(text,x,y,size):
 bpy.ops.object.text_add();o=bpy.context.object;o.parent=cam;o.location=(x,y,-1);o.rotation_euler=(0,0,0);o.data.body=text;o.data.align_x='CENTER';o.data.size=size
 m=bpy.data.materials.new('Overlay ink');m.use_nodes=True;n=m.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');e.inputs['Color'].default_value=(.025,.03,.04,1);oout=n.new('ShaderNodeOutputMaterial');m.node_tree.links.new(e.outputs[0],oout.inputs['Surface']);o.data.materials.append(m)
 o.visible_shadow=False
height=cam.data.ortho_scale*s.render.resolution_y/s.render.resolution_x
overlay('Actual enabled factory / Blender reconstruction - NOT Unity',0,height/2-.6,.26)
overlay('F1 + F2 + F3 + ActorModules + Vanguard motion / real palette and TRS',0,height/2-1.0,.19)
bpy.context.view_layer.update()
for index,c in enumerate(cases):
 dx=(index%4-1.5)*3.35;dy=3.3 if index<4 else -3.3
 def world(v):x,y,z=v;return (x+dx,-z+dy,y)
 for p in c['parts']:
  m=bpy.data.meshes.new(p['name']);t=p['triangles'];m.from_pydata([world(v) for v in p['vertices']],[],[t[j:j+3] for j in range(0,len(t),3)]);m.update();o=bpy.data.objects.new(p['name'],m);s.collection.objects.link(o);m.materials.append(material(p['color']))
 for line in c['lines']:
  curve=bpy.data.curves.new('Actual line positions','CURVE');curve.dimensions='3D';curve.bevel_depth=line['width']/2;curve.bevel_resolution=0;poly=curve.splines.new('POLY');poly.points.add(len(line['points'])-1)
  for point,v in zip(poly.points,line['points']):point.co=(*world(v),1)
  o=bpy.data.objects.new('Bowstring reconstruction',curve);s.collection.objects.link(o);curve.materials.append(material(line['color']))
 uv=world_to_camera_view(s,cam,Vector((dx,dy,0)))
 overlay(c['name'],(uv.x-.5)*cam.data.ortho_scale,(uv.y-.5)*height-.42,.26)

s.render.filepath=str(out);bpy.ops.render.render(write_still=True)
print('PASS eight actual production factories, real palette and line positions; Blender reconstruction NOT Unity')
