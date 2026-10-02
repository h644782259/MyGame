"""Read-only packed Blender material inspection; no Unity shader/preview claim."""
import bpy,sys,json,hashlib,math
from pathlib import Path
from mathutils import Vector
out=Path(__file__).parent;repo=Path('/workspace/emberfall_moving_pilot');blend=repo/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';bpy.ops.wm.open_mainfile(filepath=str(blend));scene=bpy.context.scene
sys.path.insert(0,str(out));from layered_pose import LayeredPose
rig=bpy.data.objects['Vanguard_Rig'];sampler=LayeredPose(rig);hero=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Vanguard_')];source=bpy.data.materials['Pilot_Atlas_Standard']
report={'blendSha256':hashlib.sha256(blend.read_bytes()).hexdigest(),'material':'Unmodified packed source Pilot_Atlas_Standard Principled graph; not a Unity Standard shader conversion','images':{},'views':{},'settings':{}}
for i in bpy.data.images:
 if not i.packed_file:continue
 p=repo/'Assets/Resources/BlenderPilot'/(i.name+'.png');packed=bytes(i.packed_file.data);assert packed==p.read_bytes(),i.name
 report['images'][i.name]={'packedEqualsShippedPNG':True,'sha256':hashlib.sha256(packed).hexdigest(),'colorspace':i.colorspace_settings.name}
for o in list(bpy.data.objects):
 if o.type in ['CAMERA','LIGHT'] or o.name=='Preview_Ground':bpy.data.objects.remove(o,do_unlink=True)
neutral=bpy.data.materials.new('Inspection gray');neutral.use_nodes=True;bs=neutral.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.42,.44,.47,1);bs.inputs['Roughness'].default_value=.68;bs.inputs['Metallic'].default_value=0;bs.inputs['Specular IOR Level'].default_value=0
world=bpy.data.worlds.new('Inspection world');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.28,.32,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4;scene.world=world
lights=[]
for name,position,energy,size in [('Key',(-3,-4,6),700,4),('Fill',(4,-1,3.5),400,3),('Back',(-2,4,5),800,3)]:
 bpy.ops.object.light_add(type='AREA',location=position);o=bpy.context.object;o.name=name;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1.1))-o.location).to_track_quat('-Z','Y').to_euler();lights.append({'name':name,'position':position,'watts':energy,'diskDiameter':size})
bpy.ops.object.camera_add();cam=bpy.context.object;cam.data.type='ORTHO';scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=64;scene.cycles.seed=7081;scene.cycles.use_animated_seed=False;scene.cycles.use_denoising=False;scene.cycles.use_adaptive_sampling=False;scene.cycles.max_bounces=4;scene.cycles.diffuse_bounces=2;scene.cycles.glossy_bounces=2;scene.render.threads_mode='FIXED';scene.render.threads=1;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.view_settings.view_transform='AgX'
report['settings']={'renderer':bpy.app.version_string,'samples':64,'seed':7081,'threads':1,'denoising':False,'maxBounces':4,'diffuseBounces':2,'glossyBounces':2,'viewTransform':'AgX','exposure':scene.view_settings.exposure,'gamma':scene.view_settings.gamma,'lights':lights,'worldLinearRGB':[.25,.28,.32],'worldStrength':.4}
policy=json.loads((out/'contact-sample.json').read_text())
report['contactSample']=policy
for view in ['Contact24','Full','Weapon','Back']:
 if view=='Contact24':sampler.sample(policy)
 else:sampler.clip('Idle',0)
 rig.location=(0,0,0);bpy.context.view_layer.update()
 visible=hero if view in ['Contact24','Full'] else [o for o in hero if o.name==('Vanguard_Sword' if view=='Weapon' else 'Vanguard_Back')]
 for o in hero:o.hide_render=o not in visible
 points=[]
 for o in visible:
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());points.extend(ev.matrix_world@Vector(c) for c in ev.bound_box)
 low=Vector([min(v[i] for v in points) for i in range(3)]);high=Vector([max(v[i] for v in points) for i in range(3)]);target=(low+high)/2
 if view=='Contact24':target=Vector((0,0,1.1));cam.location=(5,-8,4.2);cam.data.ortho_scale=4.7;res=(800,600)
 else:
  direction=Vector((5,-8,3.1) if view!='Back' else (3,8,2));cam.location=target+direction;cam.data.ortho_scale=max(high-low)*1.32;res=(640,640)
 cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.resolution_x,scene.render.resolution_y=res
 report['views'][view]={'visibleMeshes':[o.name for o in visible],'camera':list(cam.location),'target':list(target),'orthographicScale':cam.data.ortho_scale,'resolution':res,'bounds':[list(low),list(high)],'pose':'actual locked layered contact frame24' if view=='Contact24' else 'source Idle normalized0'}
 for mode,mat in [('neutral',neutral),('material',source)]:
  for o in hero:o.data.materials.clear();o.data.materials.append(mat)
  scene.render.filepath=str(out/(view+'-'+mode+'.png'));bpy.ops.render.render(write_still=True)
(out/'render-manifest.json').write_text(json.dumps(report,indent=2))
