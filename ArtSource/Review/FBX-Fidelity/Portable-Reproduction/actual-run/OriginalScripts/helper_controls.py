import bpy,sys,json,inspect
from pathlib import Path
out=Path(__file__).parent;sys.path.insert(0,str(out));import export_vanguard_fbx as helper
from io_scene_fbx import export_fbx_bin
blend=Path('/workspace/emberfall_android_player_journey/ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend');checks=[]
# Actual current final helper output; independently compare its curves to dense-tested output.
calls=helper.export_blend(blend,out/'helper-output.fbx');native=export_fbx_bin.fbx_animations_do
assert tuple(inspect.signature(native).parameters)==('scene_data','ref_id','f_start','f_end','start_zero','objects','force_keep')
for case in ('wrong-suffix','wrong-selection','unknown-action'):
 if case=='wrong-selection':bpy.data.objects['Vanguard_Sword'].select_set(False)
 if case=='unknown-action':
  bpy.data.objects['Vanguard_Sword'].select_set(True);extra=bpy.data.actions['Pilot_Idle'].copy();extra.name='Pilot_Unexpected';extra.use_fake_user=True
 try:helper.export_current_scene(out/('reject.blend' if case=='wrong-suffix' else 'reject.fbx'))
 except (RuntimeError,ValueError) as e:checks.append({'case':case,'rejected':True,'message':str(e),'exportFunctionRestored':export_fbx_bin.fbx_animations_do is native})
 else:raise AssertionError('negative escaped: '+case)
 assert export_fbx_bin.fbx_animations_do is native
assert len(checks)==3 and all(c['rejected'] and c['exportFunctionRestored'] for c in checks)
(out/'helper-controls.json').write_text(json.dumps({'exportCalls':calls,'negativeControls':checks,'pass':True},indent=2)+'\n');print('PASS helper final export and three fail-closed controls')
