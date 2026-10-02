"""Read-only matched c5a734e/refined native Blender stills, no zoom or retouch.
blender -b --python-exit-code 1 --threads 1 --python SCRIPT -- BEFORE_BLEND AFTER_BLEND OUTPUT
"""
import bpy,json,sys,hashlib,time,math
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
before,after,out=map(Path,sys.argv[sys.argv.index('--')+1:]);out.mkdir(parents=True,exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
report={'sourceScope':'Native authored Blender assets, not Unity frames or quality acceptance','sourceScriptSha256':sha(Path(__file__)),'baselineCommit':'c5a734ebe43925eb46fee4c236bb863a1a0d5884','refinementCommit':'4d645c3174dd94117dbb6a7938b963a32fe98e9f','renders':[]}
packed_reference=None
for name,blend in [('Before',before),('After',after)]:
 source_hash=sha(blend)
 bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene
 rig=bpy.data.objects['Vanguard_Rig'];rig.animation_data.action=bpy.data.actions['Pilot_Idle'];scene.frame_set(0)
 scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=1
 scene.cycles.samples=128;scene.cycles.use_denoising=False;scene.cycles.use_adaptive_sampling=False
 scene.cycles.max_bounces=0;scene.cycles.diffuse_bounces=0;scene.cycles.glossy_bounces=0;scene.cycles.transmission_bounces=0
 scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.render.film_transparent=False
 scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.view_settings.exposure=0;scene.view_settings.gamma=1
 hero=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Vanguard_')]

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
 packed={i.name:hashlib.sha256(bytes(i.packed_file.data)).hexdigest() for i in bpy.data.images if i.packed_file}
 if packed_reference is None:packed_reference=packed
 else:assert packed==packed_reference,'shared atlas changed'
 assert all(len(o.data.materials)==1 and o.data.materials[0].name=='Pilot_Atlas_Standard' for o in hero)
 distance=19*1.2041595;pitch=math.radians(48.36646)
 at=(0,math.cos(pitch)*distance,.7+math.sin(pitch)*distance);target=(0,0,.7)
 camera.data.type='PERSP';camera.data.sensor_fit='VERTICAL';camera.data.sensor_height=32;camera.data.lens=32/(2*math.tan(math.radians(48)/2))
 camera.location=Vector(at);camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
 scene.render.resolution_x=800;scene.render.resolution_y=600;scene.render.filepath=str(out/(name+'.png'))
 bpy.context.view_layer.update();points=[];dg=bpy.context.evaluated_depsgraph_get()
 for o in hero:
  ev=o.evaluated_get(dg);mesh=ev.to_mesh();points.extend(world_to_camera_view(scene,camera,ev.matrix_world@v.co) for v in mesh.vertices);ev.to_mesh_clear()
 bounds=[min(p.x for p in points)*800,(1-max(p.y for p in points))*600,max(p.x for p in points)*800,(1-min(p.y for p in points))*600]
 assert all(p.z>0 and 0<=p.x<=1 and 0<=p.y<=1 for p in points),'model outside frame'
 t=time.monotonic();bpy.ops.render.render(write_still=True);seconds=time.monotonic()-t
 assert sha(blend)==source_hash,'source unexpectedly changed'
 report['renders'].append({'name':name,'sourceBlend':str(blend),'sourceSha256':source_hash,'imageSha256':sha(out/(name+'.png')),'packedImages':packed,'cameraBlender':at,'cameraUnity':[at[0],at[2],-at[1]],'targetBlender':target,'verticalFovDegrees':48,'defaultDistance':19,'distanceScale':1.2041595,'pitchDegrees':48.36646,'yawDegrees':0,'sensorHeightMm':32,'lensMm':camera.data.lens,'projection':'perspective','resolution':[800,600],'objectPixelBounds':bounds,'pose':'Pilot_Idle frame0','material':'unchanged shared original atlas','engine':bpy.app.version_string+' Cycles CPU','threads':1,'samples':128,'denoiser':False,'bounceLimits':0,'seconds':round(seconds,2)})
(out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n');print('COMBAT_DISTANCE_DONE',str(out))
