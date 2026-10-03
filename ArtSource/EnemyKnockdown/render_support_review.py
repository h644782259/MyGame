"""Two cameras, one unchanged production Guardian down mesh sample. No Unity rendering.
blender -b --factory-startup --python render_support_review.py -- geometry.json output_dir
"""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];source=Path(args[0]);out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
case=next(c for c in json.loads(source.read_text()) if c['name']=='Guardian down')
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=False
scene.render.resolution_x=1000;scene.render.resolution_y=650;scene.render.resolution_percentage=100;scene.world.color=(.12,.14,.16)
for part in case['parts']:
 mesh=bpy.data.meshes.new(part['name']);t=part['triangles'];mesh.from_pydata([(x,-z,y) for x,y,z in part['vertices']],[],[t[i:i+3] for i in range(0,len(t),3)]);mesh.update();obj=bpy.data.objects.new(part['name'],mesh);scene.collection.objects.link(obj)
 mat=bpy.data.materials.new(part['name']);mat.use_nodes=True;shader=mat.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(*part['color'],1);shader.inputs['Roughness'].default_value=.72;obj.data.materials.append(mat)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Actual y=0 reference plane';mat=bpy.data.materials.new('Reference floor');mat.use_nodes=True;mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.06,.075,.09,1);floor.data.materials.append(mat)
for at,power,size in [((3,-3,6),800,4),((-4,4,5),1000,4)]:
 bpy.ops.object.light_add(type='AREA',location=at);light=bpy.context.object;light.data.energy=power;light.data.size=size;light.rotation_euler=(Vector((0,1,.3))-light.location).to_track_quat('-Z','Y').to_euler()
for name,location,target in [('GuardianDown-Low',(-3.7,-4.6,.75),(0,1.2,.4)),('GuardianDown-Side',(5,1.2,.68),(0,1.2,.58))]:
 bpy.ops.object.camera_add(location=location);camera=bpy.context.object;camera.name=name;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.7;scene.camera=camera
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'GuardianDown-Support.blend'),compress=True)
