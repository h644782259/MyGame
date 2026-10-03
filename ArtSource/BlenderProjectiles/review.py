"""Render actual managed-factory transformed vertices; no independently posed stand-ins."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
S=Path(__file__).resolve().parent
samples={v['name']:v for v in json.loads((S/'factory-samples.json').read_text())}
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=False;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=1800;scene.render.resolution_y=820;scene.render.resolution_percentage=100
scene.world.color=(.14,.14,.14)
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.48;return m
neutral=mat('neutral',(0.55,.65,.7));colors=[(.7,.85,.95),(.3,.65,1),(.3,.8,.57),(1,.16,.24),(.58,.14,.04),(.5,.75,.25)]
def text(t,x,y,size=.18):
 c=bpy.data.curves.new('label','FONT');c.body=t;c.size=size;c.align_x='CENTER';o=bpy.data.objects.new(t,c);scene.collection.objects.link(o);o.location=(x,y,.01)
keys=['ArrowBody','CasterBolt','ContractBolt','HostileBolt','MeteorRock','TrapCore']
for col,key in enumerate(keys):
 x=col*2.5
 for row,version in enumerate(['old','authored']):
  y=-row*2.3;name=key+' '+version
  if name in samples:
   data=samples[name];v=[Vector(p) for p in data['vertices']];center=(Vector(tuple(min(p[i] for p in v) for i in range(3)))+Vector(tuple(max(p[i] for p in v) for i in range(3))))*.5
   gain=1.1 if col==0 else 3 if col<4 else 1.3
   points=[]
   for p in v:
    p=(p-center)*gain
    # Unity x/y/z -> Blender x/-z/y, slight consistent turn for depth.
    points.append((p.x,-p.z,p.y+.65))
   m=bpy.data.meshes.new(name);m.from_pydata(points,[],[data['triangles'][i:i+3] for i in range(0,len(data['triangles']),3)]);m.update();o=bpy.data.objects.new(name,m);scene.collection.objects.link(o);o.location=(x,y,0);o.data.materials.append(mat(name,data['color']))
  else:text('No mechanism body',x,y,.16)
  text(key+' / '+version,x,y-.87,.17)
  if col<4:text('+Z travel toward bottom',x,y-1.10,.11)
text('F5 | actual factory vertices + TRS | Blender preview, not Unity capture',6.25,1.6,.25)
text('Original palettes; projectile columns magnified equally per pair. No trajectory / damage change.',6.25,-3.85,.20)
bpy.ops.object.light_add(type='AREA',location=(3,-3,9));bpy.context.object.data.energy=1600;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=10
bpy.ops.object.camera_add(location=(6.25,-5,16));camera=bpy.context.object;camera.rotation_euler=(Vector((6.25,-1,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=16;scene.camera=camera
scene.render.filepath=str(S/'Factory-Review.png');bpy.ops.render.render(write_still=True)
