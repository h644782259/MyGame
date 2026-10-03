"""Full production samples, shared cameras and light, Blender only.
Usage: blender -b -t 2 --python render_assemblies.py -- assemblies.json animation.json output
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];assemblies=json.loads(Path(args[0]).read_text());animation=json.loads(Path(args[1]).read_text());out=Path(args[2]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=20;s.cycles.use_denoising=False;s.world.color=(.14,.14,.14);s.render.resolution_x=1200;s.render.resolution_y=700;s.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';s.camera=cam
for loc,power in [((0,-6,10),2300),((5,4,8),1700)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=10;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.06
materials={}
def mat(rgb):
 key=tuple(round(x,4) for x in rgb)
 if key not in materials:
  m=bpy.data.materials.new(str(key));m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*key,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;materials[key]=m
 return materials[key]
def render(name,cases,spacing,scale,target=(0,0,1.1)):
 cam.location=(0,-16,8);cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;items=[]
 for i,c in enumerate(cases):
  ox,oy,oz=c.get('origin',[0,0,0]);shift=(i-(len(cases)-1)/2)*spacing
  for p in c['parts']:
   mesh=bpy.data.meshes.new(p['name']);t=p['triangles'];mesh.from_pydata([(x-ox+shift,-z+oz,y-oy) for x,y,z in p['vertices']],[],[t[j:j+3] for j in range(0,len(t),3)]);mesh.update();obj=bpy.data.objects.new(p['name'],mesh);s.collection.objects.link(obj);obj.data.materials.append(mat(p['color']));items.append(obj)
  bpy.ops.object.text_add(location=(shift-spacing*.36,-.9,-.02),rotation=(math.radians(70),0,0));obj=bpy.context.object;obj.data.body=c['name'];obj.data.size=.16;obj.data.materials.append(mat((.04,.04,.04)));items.append(obj)
 s.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 for obj in items:bpy.data.objects.remove(obj,do_unlink=True)
render('Anchor-pair',[c for c in assemblies if c['name'].startswith('Anchor-')],2.2,5,(0,0,.55))
render('Astrolabe-retained',[c for c in assemblies if c['name']=='LargeAstrolabe'],5,8,(0,0,1.3))
for phase in ['walk','windup','contact','recovery']:
 for variant in ['before','after']:
  cases=[next(c for c in animation if c['name']==kind+'-'+phase+'-'+variant) for kind in ['Slime','Goblin','Wisp','Guardian','GuardianBoss']]
  # Same identity labels prevent annotation text influencing before/after pixel comparison.
  cases=[dict(c,name=c['name'].split('-')[0]) for c in cases]
  render('Action-'+phase+'-'+variant,cases,3.6,19,(0,0,1.5))
