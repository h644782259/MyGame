"""Read-only source-asset review: native Cycles CPU stills, no mesh/rig edits.
blender -b --threads 1 --python render_static_review.py -- ROOT BASELINE_JSON OUTPUT [--combat-only]
"""
import bpy,json,sys,hashlib,time,math
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];combat_only='--combat-only' in args
root,baseline,out=map(Path,args[:3]);out.mkdir(parents=True,exist_ok=True)
blend=root/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend'
hashes={'blend':hashlib.sha256(blend.read_bytes()).hexdigest(),'baseline':hashlib.sha256(baseline.read_bytes()).hexdigest()}
bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene;scene.frame_set(0)
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=1
scene.cycles.samples=128;scene.cycles.use_denoising=False;scene.cycles.use_adaptive_sampling=False
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
if combat_only and (out/'render-manifest.json').exists():
 reports=[r for r in json.loads((out/'render-manifest.json').read_text())['renders'] if r['file'].startswith('Vanguard-')]
def render(name,at,target,scale,width,height):
 camera.location=Vector(at);camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
 scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.filepath=str(out/(name+'.png'));start=time.monotonic();bpy.ops.render.render(write_still=True)
 reports.append({'file':name+'.png','camera':list(at),'target':list(target),'projection':camera.data.type,'orthographicScale':scale if camera.data.type=='ORTHO' else None,'verticalFovDegrees':48 if camera.data.type=='PERSP' else None,'resolution':[width,height],'samples':128,'seconds':round(time.monotonic()-start,2)})
for name,at in ([] if combat_only else [('front',(0,-8,3.0)),('side',(8,0,3.0)),('back',(0,8,3.0))]):render('Vanguard-'+name,at,(0,0,1.22),3.35,640,800)
clay=bpy.data.materials.new('Evidence shared neutral clay');clay.use_nodes=True;bs=clay.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.42,.45,.49,1);bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.8
for o in hero:o.data.materials.clear();o.data.materials.append(clay)
# GameSession.cs: FOV48, AdventureCamera distance19, DistanceScale1.2041595,
# DefaultPitch48.36646, yaw0 and look target y.7; Unity(x,y,z)->Blender(x,-z,y).
distance=19*1.2041595;pitch=math.radians(48.36646)
camera.data.type='PERSP';camera.data.sensor_fit='VERTICAL';camera.data.sensor_height=32
camera.data.lens=32/(2*math.tan(math.radians(48)/2))
fixture=((0,math.cos(pitch)*distance,.7+math.sin(pitch)*distance),(0,0,.7),0,800,600)
render('Combat-distance-new',*fixture)
for o in hero:o.hide_render=True
case=next(c for c in json.loads(baseline.read_text()) if c['hero']==0 and c['tier']==-1 and c['wing']==0)
for index,part in enumerate(case['parts']):
 mesh=bpy.data.meshes.new('Baseline_'+str(index));v=[(x,-z,y) for x,y,z in part['vertices']];t=part['triangles'];mesh.from_pydata(v,[],[t[i:i+3] for i in range(0,len(t),3)]);mesh.update()
 # Source recipes recalculate shared-vertex normals; averaging shared vertices
 # here avoids gratuitously degrading the old mesh to flat triangle shading.
 for face in mesh.polygons:face.use_smooth=True
 obj=bpy.data.objects.new(part['name'],mesh);scene.collection.objects.link(obj);mesh.materials.append(clay)
render('Combat-distance-old',*fixture)
assert hashlib.sha256(blend.read_bytes()).hexdigest()==hashes['blend']
manifest={'frozenHead':'49e624b3f0729ff44761a262a062dc67b849dd84','sourceHashes':hashes,'nativeEngine':'Blender 4.3.2 / Cycles CPU single-thread / 128 samples / denoiser off','geometryChanges':False,'renders':reports,'scope':'Source-art review, not Unity screenshot or quality acceptance. Three atlas-material views; combat pair identical neutral material/camera/light. Baseline meshes reconstructed from actual production export with managed TRS. No runtime/assets saved or changed.'}
(out/'render-manifest.json').write_text(json.dumps(manifest,indent=2));print('STATIC_REVIEW_DONE',str(out))
