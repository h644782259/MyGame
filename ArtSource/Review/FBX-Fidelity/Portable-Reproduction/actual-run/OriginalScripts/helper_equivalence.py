import bpy,json,hashlib
from pathlib import Path
out=Path(__file__).parent
result={}
for name in ('hardened-mixed.fbx','helper-output.fbx'):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(out/name));curves={}
 for a in bpy.data.actions:
  key=a.name[a.name.index('Pilot_'):]
  data={'frameRange':list(a.frame_range),'channels':[{'path':f.data_path,'index':f.array_index,'extrapolation':f.extrapolation,'keys':[{'co':list(k.co),'interpolation':k.interpolation,'left':list(k.handle_left),'right':list(k.handle_right),'leftType':k.handle_left_type,'rightType':k.handle_right_type} for k in f.keyframe_points]} for f in sorted(a.fcurves,key=lambda f:(f.data_path,f.array_index))]}
  curves[key]=hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 result[name]={'sha256':hashlib.sha256((out/name).read_bytes()).hexdigest(),'curveHashes':curves}
result['allFiveImportedCurvesExactlyEqual']=result['hardened-mixed.fbx']['curveHashes']==result['helper-output.fbx']['curveHashes'] and len(result['helper-output.fbx']['curveHashes'])==5
(out/'helper-equivalence.json').write_text(json.dumps(result,indent=2)+'\n');assert result['allFiveImportedCurvesExactlyEqual'];print('PASS final helper output imported curves exactly equal dense-tested hardened-mixed')
