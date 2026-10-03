"""Authored geometry preview, not Unity footage or a combat simulation."""
import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(root/'Vanguard-Skills.blend'))
sources={o.name:o for o in bpy.context.scene.objects}
for o in sources.values():o.hide_render=True
mat=bpy.data.materials.new('Preview amber');mat.diffuse_color=(1,.45,.065,1)
core=bpy.data.materials.new('Preview core');core.diffuse_color=(1,.88,.48,1)
def part(name,at,scale,yaw=0,hot=False):
 o=sources[name].copy();o.data=o.data.copy();bpy.context.collection.objects.link(o);o.hide_render=False;o.location=at;o.scale=scale;o.rotation_euler.z=yaw;o.data.materials.clear();o.data.materials.append(core if hot else mat)
t=.18;r=1.1+t/.48*2.3
for i in range(3):part('Crescent',(-3,0,.62+i*.16),(r,r,r),-(t*8+i*math.tau/3))
for i in range(2):part('CrescentCore',(-3,0,.63+i*.16),(r*.985,)*3,-(t*8+i*math.tau/3+.04),True)
for j in range(3):part('Shard',(3+(-1 if j%2 else 1)*(.12+j*.1),-.5-j*.68,.015),(.10+j*.027,.24,.08+.75*math.sin(min(1,(t-j*.055)/.35)*math.pi)))
part('Fault',(3,-2.5,.07),(2,2,1),hot=True);part('Fault',(3,-1.3,.045),(1,1,1),hot=True)
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;m=bpy.data.materials.new('Neutral ground');m.diffuse_color=(.065,.095,.12,1);floor.data.materials.append(m);floor.location.z=-.05
bpy.ops.object.light_add(type='AREA',location=(0,-3,9));bpy.context.object.data.energy=1700;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=8
bpy.ops.object.camera_add(location=(8,-12,12));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-1,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=13
sc=bpy.context.scene;sc.camera=camera;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.render.resolution_x=1100;sc.render.resolution_y=650;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(root/'Authored-Skills-Preview.png');bpy.ops.render.render(write_still=True)
