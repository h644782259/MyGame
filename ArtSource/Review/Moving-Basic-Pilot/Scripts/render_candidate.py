import bpy,json,math,hashlib,sys
from pathlib import Path
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:];root,out=map(lambda v:Path(v).resolve(),args[:2]);out.mkdir(parents=True,exist_ok=True);scriptdir=Path(__file__).parent;blend=root/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend'
combat='--combat' in sys.argv;stop='--stop' in sys.argv;framesdir=out/('candidate-stop-combat-frames' if combat else 'candidate-stop-frames') if stop else out/('candidate-combat-frames' if combat else 'candidate-frames');
data=json.loads((out/'motion.json').read_text());bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene
sys.path.insert(0,str(scriptdir));from layered_pose import LayeredPose
policy=json.loads((out/('candidate-stop-samples.json' if stop else 'candidate-samples.json')).read_text())['samples']
rig=bpy.data.objects['Vanguard_Rig'];sampler=LayeredPose(rig);hero=[x for x in bpy.data.objects if x.type=='MESH' and x.name.startswith('Vanguard_')]
for x in list(bpy.data.objects):
 if x.type in ['LIGHT','CAMERA'] or x.name=='Preview_Ground':bpy.data.objects.remove(x,do_unlink=True)
mat=bpy.data.materials.new('Matched neutral motion inspection');mat.use_nodes=True;bs=mat.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.42,.44,.47,1);bs.inputs['Roughness'].default_value=.68;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=0
for x in hero:x.data.materials.clear();x.data.materials.append(mat)
# Source-world geometry converts Unity (X,Y-up,Z-forward) to Blender(X,-Y-forward,Z-up).
procedural=[]
world=bpy.data.worlds.new('Neutral inspection');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.28,.32,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4;scene.world=world
for name,at,power in [('Key',(-3,-4,6),2.5),('Fill',(4,-1,3.5),.75),('Back',(-2,4,5),1.6)]:
 bpy.ops.object.light_add(type='SUN',location=at);x=bpy.context.object;x.name=name;x.data.energy=power;x.data.angle=0;x.rotation_euler=(Vector((0,0,1.1))-x.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=180,location=(0,0,-.045));floor=bpy.context.object
fm=bpy.data.materials.new('One-meter tracking ground');fm.use_nodes=True;nodes=fm.node_tree.nodes;tex=nodes.new('ShaderNodeTexChecker');coord=nodes.new('ShaderNodeTexCoord');tex.inputs['Color1'].default_value=(.145,.165,.185,1);tex.inputs['Color2'].default_value=(.205,.225,.245,1);tex.inputs['Scale'].default_value=180;fm.node_tree.links.new(coord.outputs['Generated'],tex.inputs['Vector']);fm.node_tree.links.new(tex.outputs[0],nodes['Principled BSDF'].inputs['Base Color']);nodes['Principled BSDF'].inputs['Roughness'].default_value=.95;nodes['Principled BSDF'].inputs['Specular IOR Level'].default_value=0;floor.data.materials.append(fm)
distance=19*1.2041595;pitch=math.radians(48.36646);cameraOffset=(0,math.cos(pitch)*distance,.7+math.sin(pitch)*distance) if combat else (5,-8,4.2);targetOffset=(0,0,.7) if combat else (0,0,1.1)
bpy.ops.object.camera_add(location=cameraOffset);camera=bpy.context.object;camera.rotation_euler=(Vector(targetOffset)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=4.7;scene.camera=camera
if combat:camera.data.type='PERSP';camera.data.sensor_fit='VERTICAL';camera.data.sensor_height=32;camera.data.lens=32/(2*math.tan(math.radians(48)/2))
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=int(sys.argv[sys.argv.index('--samples')+1]) if '--samples' in sys.argv else (4 if combat else 8);scene.cycles.use_denoising=False;scene.cycles.use_adaptive_sampling=False;scene.cycles.max_bounces=0;scene.cycles.diffuse_bounces=0;scene.cycles.glossy_bounces=0;scene.render.threads_mode='FIXED';scene.render.threads=1;scene.render.resolution_x=800 if combat else 480;scene.render.resolution_y=600 if combat else 360;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.view_settings.view_transform='AgX';scene.render.fps=24
# Camera follows an external owner transform at 6m/s. Rig Root remains unmodified.
framesdir.mkdir(exist_ok=True)
report={'sourceBlend':str(blend),'sourceBlendSha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'motionSha256':hashlib.sha256((out/'motion.json').read_bytes()).hexdigest(),'fps':24,'duration':6,'resolution':[scene.render.resolution_x,scene.render.resolution_y],'cameraOffset':cameraOffset,'targetOffset':targetOffset,'orthoScale':None if combat else 4.7,'verticalFov':48 if combat else None,'defaultDistance':19 if combat else None,'distanceScale':1.2041595 if combat else None,'pitch':48.36646 if combat else None,'lensMm':camera.data.lens if combat else None,'externalOwnerVelocityBlender':[0,-6,0],'rootMotion':False,'material':'matched gray .42,.44,.47 / roughness .68 / metal0 / specular0','lighting':'three direct suns 2.5/.75/1.6, zero angular size, max bounces0','cyclesSamples':scene.cycles.samples,'cpuThreads':1,'OIDN':False,'contacts':data['contacts'],'rig':{b.name:b.parent.name if b.parent else None for b in rig.data.bones},'clips':{a.name:list(a.frame_range) for a in bpy.data.actions},'runtimeCommit':'49a356eb2df9951fc07f9758f3eea4055b0b9c30','policySha256':hashlib.sha256((out/'policy-export/BlenderPilotPosePolicy.cs').read_bytes()).hexdigest(),'policySamplesSha256':hashlib.sha256((out/('candidate-stop-samples.json' if stop else 'candidate-samples.json')).read_bytes()).hexdigest(),'boundary':'Unchanged native Blender clips composed with actual production policy outputs and corresponding localTRS Lerp/Slerp. Not Unity/nativeFBX runtime playback. Same no-enemies/no-VFX scope as baseline.'}
if stop:report['externalOwnerVelocityBlender']='(0,-6,0) until1.4s; zero1.4-2.4s; then(0,-6,0)';report['stopRestartSeconds']=[1.4,2.4]
(out/('candidate-stop-combat-manifest.json' if combat else 'candidate-stop-render-manifest.json') if stop else out/('candidate-combat-manifest.json' if combat else 'candidate-render-manifest.json')).write_text(json.dumps(report,indent=2))
only={int(v) for v in sys.argv[sys.argv.index('--only')+1].split(',')} if '--only' in sys.argv else None
for f in data['frames']:
 if only is not None and f['index'] not in only:continue
 if (framesdir/('%04d.png'%f['index'])).exists():continue
 t=f['time'];distanceAlong=6*(min(t,1.4)+max(0,t-2.4)) if stop else 6*t;offset=Vector((0,-distanceAlong,0));camera.location=Vector(cameraOffset)+offset
 rig.location=offset
 sampler.sample(policy[f['index']])
 for x in hero:x.hide_render=False
 for x,p in zip(procedural,f['parts']):
  x.hide_render=f['pilot'];x.location=offset
  if not f['pilot']:
   x.data.vertices.foreach_set('co',[q for vx,vy,vz in p['vertices'] for q in (vx,-vz,vy)]);x.data.update()
 scene.render.filepath=str(framesdir/('%04d.png'%f['index']));bpy.ops.render.render(write_still=True)
