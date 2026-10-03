"""Render actual Enemy factory + actual adapter loaded .bytes. Blender, not Unity.
blender -b --factory-startup --python render_enemies.py -- enemies.json output
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];cases=json.loads(Path(args[0]).read_text());out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=False;s.world.color=(.14,.14,.14)
s.render.resolution_x=1800;s.render.resolution_y=620;s.render.resolution_percentage=100
bpy.ops.object.camera_add(location=(0,-16,8));cam=bpy.context.object;target=Vector((0,0,1.5));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=19; s.camera=cam
for loc,power in [((0,-6,10),2300),((5,4,8),1700)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=12;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.06
materials={}
def mat(rgb):
 key=tuple(round(x,3) for x in rgb)
 if key not in materials:
  m=bpy.data.materials.new('runtime palette '+str(key));m.diffuse_color=(*key,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*key,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;materials[key]=m
 return materials[key]
labels={0:'Identity',1:'Supplier',2:'Hunt target',4:'Supported',7:'Combined roles'}
def render(name,selected,back=False):
 items=[]
 for i,c in enumerate(selected):
  shift=(i-2)*3.6
  for p in c['parts']:
   m=bpy.data.meshes.new(p['name']);v=[((x if not back else -x)+shift,(-z if not back else z),y) for x,y,z in p['vertices']];t=p['triangles'];faces=[t[j:j+3] for j in range(0,len(t),3)];m.from_pydata(v,[],faces);m.update();o=bpy.data.objects.new(p['name'],m);s.collection.objects.link(o);o.data.materials.append(mat(p['color']));items.append(o)
  bpy.ops.object.text_add(location=(shift-.9,-.85,-.015),rotation=(math.radians(70),0,0));o=bpy.context.object;o.data.body=c['name'] if back else labels[c['mask']];o.data.size=.24;o.data.materials.append(mat((.05,.05,.05)));items.append(o)
 s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 for o in items:bpy.data.objects.remove(o,do_unlink=True)
for name in dict.fromkeys(c['name'] for c in cases):render(name,[next(c for c in cases if c['name']==name and c['mask']==mask) for mask in [0,1,2,4,7]])
render('Combined-back',[c for c in cases if c['mask']==7],True)
