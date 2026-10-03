"""Offline authored mesh contact sheet. Not Unity capture."""
import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent;bpy.ops.wm.open_mainfile(filepath=str(root/'Reusable-Spell-Volumes.blend'))
for o in bpy.context.scene.objects:
 if o.name in ('IcePrimary','FirePrimary'):o.hide_render=True
colors=[(.95,.6,.12),(.1,.7,1),(1,.25,.03),(.8,.9,1),(.5,.6,1),(.8,.3,1),(.8,.3,1),(1,.6,.1),(.7,1,.3),(.3,.85,.2)]
for i,o in enumerate([o for o in bpy.context.scene.objects if o.name not in ("IcePrimary","FirePrimary")]):
 x=(i%5)*3.6-7.2;y=(i//5)*-4
 o.location=(x,y,.12);m=bpy.data.materials.new(o.name+' tint');m.diffuse_color=(*colors[i],1);o.data.materials.append(m)
 bpy.ops.object.text_add(location=(x-1.2,y-1.5,.02));text=bpy.context.object;text.data.body=o.name;text.data.size=.29
bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;o.location.z=-.03;m=bpy.data.materials.new('Neutral slate');m.diffuse_color=(.075,.1,.13,1);o.data.materials.append(m)
bpy.ops.object.light_add(type='AREA',location=(0,-3,13));o=bpy.context.object;o.data.energy=2800;o.data.size=15
bpy.ops.object.camera_add(location=(4,-13,17));o=bpy.context.object;o.rotation_euler=(Vector((0,-2.7,0))-o.location).to_track_quat('-Z','Y').to_euler();o.data.type='ORTHO';o.data.ortho_scale=20
sc=bpy.context.scene;sc.camera=o;sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=False;sc.render.resolution_x=1400;sc.render.resolution_y=820;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(root/'Spell-Volumes-Preview.png');bpy.ops.render.render(write_still=True)
