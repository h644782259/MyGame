"""Blender-native renders of exported production vertices with managed TRS, NOT Unity screenshots.
blender -b --python render_silhouettes.py -- geometry.json output_directory
"""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; cases=json.loads(Path(args[0]).read_text());out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=False
scene.render.resolution_x=360;scene.render.resolution_y=420;scene.render.resolution_percentage=100
scene.world.color=(.08,.08,.08)
mat=bpy.data.materials.new('neutral gray');mat.diffuse_color=(.46,.48,.51,1);mat.use_nodes=True;mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8;mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.28,.30,.34,1)
bpy.ops.object.camera_add(location=(3,7,4.5));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=5.1;scene.camera=cam
for at,power,size in [((3,4,7),650,5),((-4,-2,4),450,4)]:
 bpy.ops.object.light_add(type='AREA',location=at);light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.location.z=-.04
for case in cases:
 if case.get('tier',0)<0:continue
 objects=[]
 for part in case['parts']:
  mesh=bpy.data.meshes.new(part['name']);v=[(x,-z,y) for x,y,z in part['vertices']];t=part['triangles'];faces=[t[i:i+3] for i in range(0,len(t),3)];mesh.from_pydata(v,[],faces);mesh.update();obj=bpy.data.objects.new(part['name'],mesh);scene.collection.objects.link(obj);obj.data.materials.append(mat);objects.append(obj)
 if 'hero' not in case:
  points=[Vector((x,-z,y)) for part in case['parts'] for x,y,z in part['vertices']];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=Vector((0,0,4.4 if "roof" in case["name"] else 1.7))
  cam.location=center+Vector((5,9,5));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=11.5 if "roof" in case["name"] else 5.5
  scene.render.filepath=str(out/(case['name'].replace(' ','-')+'.png'))
 else:scene.render.filepath=str(out/f"h{case['hero']}-t{case['tier']}-w{case['wing']}.png")
 bpy.ops.render.render(write_still=True)
 for obj in objects:mesh=obj.data;bpy.data.objects.remove(obj,do_unlink=True);bpy.data.meshes.remove(mesh)
