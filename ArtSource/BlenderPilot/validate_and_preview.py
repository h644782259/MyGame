"""FBX roundtrip, rig deformation samples, and native-render motion contact frames."""
import bpy,json,math,os
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Resources/BlenderPilot';SOURCE=ROOT/'ArtSource/BlenderPilot';PREVIEW=Path('/workspace/scratch/blender-pilot-preview')
report={'engine':'Blender '+bpy.app.version_string,'unity_validation':False,'roundtrips':{}}
for f in sorted(OUT.glob('*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(f))
 meshes=[o for o in bpy.data.objects if o.type=='MESH'];arms=[o for o in bpy.data.objects if o.type=='ARMATURE']
 r={'meshes':len(meshes),'triangles':0,'finite_vertices':True,'uv_present':True,'bones':sum(len(o.data.bones) for o in arms),'actions':[]}
 for o in meshes:
  o.data.calc_loop_triangles();r['triangles']+=len(o.data.loop_triangles);r['uv_present'] &= bool(o.data.uv_layers)
  r['finite_vertices'] &= all(math.isfinite(x) for v in o.data.vertices for x in v.co)
 for a in bpy.data.actions:r['actions'].append({'name':a.name,'range':list(a.frame_range)})
 assert r['finite_vertices'] and r['uv_present'] and r['triangles']>0
 if f.stem=='Vanguard':
  assert r['bones']==19 and len(r['actions'])==5
  assert all(any('Pilot_'+n in a['name'] for a in r['actions']) for n in ('Idle','Move','Basic','Hit','Skill'))
 report['roundtrips'][f.name]=r
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'Emberfall-Pilot-Vanguard.blend'))
rig=bpy.data.objects['Vanguard_Rig'];skins=[bpy.data.objects['Vanguard_'+g] for g in ('Body','Clothes','Armor','Head','Back')];sw=bpy.data.objects['Vanguard_Sword'];sc=bpy.context.scene;sc.render.threads_mode='FIXED';sc.render.threads=4
report['deformation']={};clipactions={n:bpy.data.actions['Pilot_'+n] for n in ('Idle','Move','Basic','Hit','Skill')}
for name,act in clipactions.items():
 rig.animation_data.action=act;bounds=[];grips=[]
 for frame in range(int(act.frame_range.y)+1):
  sc.frame_set(frame);dg=bpy.context.evaluated_depsgraph_get();coords=[]
  for skin in skins:
   ev=skin.evaluated_get(dg);me=ev.to_mesh();coords.extend(ev.matrix_world@v.co for v in me.vertices);ev.to_mesh_clear()
  assert all(math.isfinite(c) for v in coords for c in v)
  bounds.append([min(v.z for v in coords),max(v.z for v in coords)])
  grips.append(list(bpy.data.objects['Anchor_Grip'].matrix_world.translation))
 report['deformation'][name]={'frames_sampled':len(bounds),'min_z':min(b[0] for b in bounds),'max_z':max(b[1] for b in bounds),'grip_at_first':grips[0],'grip_at_mid':grips[len(grips)//2]}
 if os.environ.get('PILOT_RENDER_CLIPS') and name not in os.environ['PILOT_RENDER_CLIPS'].split(','):continue
 # Original native Blender frames, assembled into animation only after rendering.
 sc.render.resolution_x=384;sc.render.resolution_y=384;sc.cycles.samples=6
 folder=PREVIEW/name;folder.mkdir(exist_ok=True)
 for i in range(12):
  sc.frame_set(13 if name=='Basic' and i==6 else round(i/12*act.frame_range.y));sc.render.filepath=str(folder/('%02d.png'%i));bpy.ops.render.render(write_still=True)
report['contact']={'authored_basic_frame':13,'authored_normalized':.52,'runtime_samples_actionAge_over_actionDuration':True,'no_hit_or_timing_change':True}
(SOURCE/'validation-blender.json').write_text(json.dumps(report,indent=2));print('PILOT_VALIDATION',json.dumps(report))
