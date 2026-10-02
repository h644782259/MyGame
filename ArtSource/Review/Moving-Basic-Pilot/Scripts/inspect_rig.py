import bpy,json,hashlib,sys
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];r,out=map(lambda v:Path(v).resolve(),args[:2]);out.mkdir(parents=True,exist_ok=True);f=r/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';bpy.ops.wm.open_mainfile(filepath=str(f));rig=bpy.data.objects['Vanguard_Rig']
report={'sceneFPS':bpy.context.scene.render.fps,'fpsBase':bpy.context.scene.render.fps_base,'blendHash':hashlib.sha256(f.read_bytes()).hexdigest(),'bones':{b.name:b.parent.name if b.parent else None for b in rig.data.bones},'anchors':{o.name:{'parent':o.parent.name if o.parent else None,'parentType':o.parent_type,'parentBone':o.parent_bone} for o in bpy.data.objects if o.name.startswith('Anchor_') or o.name=='Vanguard_Sword'},'actions':{}}
for a in bpy.data.actions:
 channels={}
 for c in a.fcurves:
  values=[p.co.y for p in c.keyframe_points]
  channels[c.data_path+'['+str(c.array_index)+']']={'min':min(values),'max':max(values),'keys':len(values)}
 report['actions'][a.name]={'range':list(a.frame_range),'allChannels':len(channels),'animatedChannels':{k:v for k,v in channels.items() if v['max']-v['min']>1e-7},'rootChannels':{k:v for k,v in channels.items() if '"Root"' in k}}
(out/'rig-inspection.json').write_text(json.dumps(report,indent=2))
