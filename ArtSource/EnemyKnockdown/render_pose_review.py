"""Render actual production factory/TRS exported poses. Not a Unity screenshot.
blender -b --factory-startup --python render_pose_review.py -- geometry.json output.png
"""
import bpy,sys,json
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];cases=json.loads(Path(args[0]).read_text());output=Path(args[1]);output.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False
scene.render.resolution_x=1400;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.world.color=(.15,.17,.20)
materials={}
for case in cases:
 name=case['name'];kind,stage=name.split();row=0 if kind=='Goblin' else 1;column=['upright','fall','down','rise','standing'].index(stage);offset=Vector((column*3.8,row*4.2,0))
 collection=bpy.data.collections.new(name);scene.collection.children.link(collection)
 for part in case['parts']:
  mesh=bpy.data.meshes.new(part['name']);v=[tuple(Vector((x,-z,y))+offset) for x,y,z in part['vertices']];t=part['triangles'];mesh.from_pydata(v,[],[t[i:i+3] for i in range(0,len(t),3)]);mesh.update();obj=bpy.data.objects.new(part['name'],mesh);collection.objects.link(obj)
  color=tuple(part['color']);key=tuple(round(c,3) for c in color)
  if key not in materials:
   mat=bpy.data.materials.new('palette '+str(key));mat.use_nodes=True;bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Base Color'].default_value=(*color,1);bsdf.inputs['Roughness'].default_value=.72;materials[key]=mat
  obj.data.materials.append(materials[key])
 text=bpy.data.curves.new(name,'FONT');text.body=kind+' / '+stage;text.size=.28;text.align_x='CENTER';obj=bpy.data.objects.new(name+' label',text);scene.collection.objects.link(obj);obj.location=offset+Vector((0,-1.0,.015))
 bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=1.65,depth=.025,location=offset+Vector((0,.4,-.027)));bpy.context.object.name=name+' review platform'
bpy.ops.mesh.primitive_plane_add(size=100);floor=bpy.context.object;floor.location.z=-.055
floor.data.materials.append(bpy.data.materials.new('floor'));floor.data.materials[0].diffuse_color=(.055,.065,.08,1)
target=Vector((7.6,2.5,.6));bpy.ops.object.camera_add(location=target+Vector((3,-18,18)));camera=bpy.context.object;camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=21;scene.camera=camera
for at,power,size in [((5,-5,13),2200,9),((12,10,12),1700,8)]:
 bpy.ops.object.light_add(type='AREA',location=at);light=bpy.context.object;light.data.energy=power;light.data.size=size;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(output);bpy.ops.wm.save_as_mainfile(filepath=str(output.with_suffix('.blend')),compress=True);bpy.ops.render.render(write_still=True)
