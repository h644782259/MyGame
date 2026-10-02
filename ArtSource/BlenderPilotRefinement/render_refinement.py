"""Native matched front/side/back evidence. Blender 4.3.2, one CPU thread.
blender -b --python-exit-code 1 --threads 1 --python render_refinement.py -- ROOT_OR_BLEND OUTPUT [--skill]
--skill renders an additional diagnostic at the existing Skill frame 25.
"""
import bpy,json,sys,hashlib,time,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];skill='--skill' in args
root,out=map(Path,args[:2]);out.mkdir(parents=True,exist_ok=True)
blend=root if root.suffix=='.blend' else root/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend'
hashes={'blend':hashlib.sha256(blend.read_bytes()).hexdigest()}
bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene;scene.frame_set(0)
if skill:
 rig=bpy.data.objects['Vanguard_Rig'];rig.animation_data.action=bpy.data.actions['Pilot_Skill'];scene.frame_set(25)
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=1
scene.cycles.samples=64 if skill else 128;scene.cycles.use_denoising=False;scene.cycles.use_adaptive_sampling=False
scene.cycles.max_bounces=0;scene.cycles.diffuse_bounces=0;scene.cycles.glossy_bounces=0;scene.cycles.transmission_bounces=0
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.render.film_transparent=False
scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.view_settings.exposure=0;scene.view_settings.gamma=1
hero=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Vanguard_')]
original_materials={o.name:list(o.data.materials) for o in hero}
for o in list(bpy.data.objects):
 if o.type in ['LIGHT','CAMERA'] or o.name=='Preview_Ground':bpy.data.objects.remove(o,do_unlink=True)
world=bpy.data.worlds.new('Review neutral studio');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.32,.34,.38,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
# Broad-angle direct suns avoid noisy multi-bounce estimates. Geometry and
# normals stay exactly as authored; this does not repair visible facets or seams.
for name,at,power,angle in [('Key',(-3,-4,6),2.5,.12),('Fill',(4,-1,3.5),.75,.18),('Back',(-2,4,5),1.6,.15)]:
 bpy.ops.object.light_add(type='SUN',location=at);o=bpy.context.object;o.name='Evidence_'+name;o.data.energy=power;o.data.angle=angle;o.rotation_euler=(Vector((0,0,1.1))-o.location).to_track_quat('-Z','Y').to_euler()
# Floor is a photographic reference only; never exported into runtime.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.045));ground=bpy.context.object;ground.name='Evidence_Floor'
mat=bpy.data.materials.new('Evidence neutral floor');mat.use_nodes=True;bs=mat.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.19,.205,.23,1);bs.inputs['Roughness'].default_value=.9;ground.data.materials.append(mat)
bpy.ops.object.camera_add();camera=bpy.context.object;camera.name='Evidence_Camera';camera.data.type='ORTHO';scene.camera=camera
reports=[]
def render(name,at,target,scale,width,height):
 camera.location=Vector(at);camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
 scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.filepath=str(out/(name+'.png'));start=time.monotonic();bpy.ops.render.render(write_still=True)
 reports.append({'file':name+'.png','camera':list(at),'target':list(target),'projection':camera.data.type,'orthographicScale':scale if camera.data.type=='ORTHO' else None,'verticalFovDegrees':48 if camera.data.type=='PERSP' else None,'resolution':[width,height],'samples':scene.cycles.samples,'seconds':round(time.monotonic()-start,2)})
if skill:render('Vanguard-skill',(4,6,3.0),(0,0,1.22),3.35,480,600)
else:
 for name,at in [('front',(0,-8,3.0)),('side',(8,0,3.0)),('back',(0,8,3.0))]:render('Vanguard-'+name,at,(0,0,1.22),3.35,640,800)

assert hashlib.sha256(blend.read_bytes()).hexdigest()==hashes['blend']
(out/('skill-manifest.json' if skill else 'manifest.json')).write_text(json.dumps({'sourceHashes':hashes,'renders':reports,'geometryChangedDuringRender':False,'unityRender':False},indent=2))
