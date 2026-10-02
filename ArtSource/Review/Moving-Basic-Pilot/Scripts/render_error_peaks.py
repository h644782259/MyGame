"""Native source/FBX pose diagnostics, not the procedural-fallback baseline movie."""
import bpy,sys,json,hashlib
from pathlib import Path
args=sys.argv[sys.argv.index('--')+1:];base,out=map(lambda x:Path(x).resolve(),args[:2]);scripts=Path(__file__).parent
forward=json.loads((out/'candidate-samples.json').read_text())['samples'];stop=json.loads((out/'candidate-stop-samples.json').read_text())['samples'];cases=[('layered-peak30',forward[30],True),('unlayered-basic-control57',forward[57],False),('incremental-stop49',stop[49],True)]
source=(scripts/'render_candidate.py').read_text();prefix=source[:source.index('report=')];folder=out/'error-peaks';folder.mkdir(exist_ok=True);reports=[]
for label,state,layered in cases:
 for imported in [False,True]:
  code=prefix
  if imported:code=code.replace('bpy.ops.wm.open_mainfile(filepath=str(blend))',"bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Resources/BlenderPilot/Vanguard.fbx'))")
  context={'__file__':str(scripts/'render_candidate.py')};exec(compile(code,str(scripts/'render_candidate.py'),'exec'),context)
  rig=context['rig'];sampler=context['sampler'];scene=context['scene']
  if layered:sampler.sample(state)
  else:sampler.clip('Basic',state['progress'])
  path=folder/(label+('-fbx.png' if imported else '-source.png'));scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
  reports.append({'file':path.name,'reimportedFBX':imported,'case':label,'input':state,'layered':layered,'camera':'same close camera at5,-8,4.2,target0,0,1.1,ortho4.7; external owner recentered to zero for diagnostic only','sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'meaning':'source/FBX authored pose diagnostic; old Basic control is NOT runtime procedural fallback baseline'})
(folder/'manifest.json').write_text(json.dumps(reports,indent=2))
