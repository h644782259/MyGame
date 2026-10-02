import hashlib,json,os,subprocess
from pathlib import Path
p=Path(__file__).resolve().parent
source=p/'BlenderPilotImport.cs';original=source.read_text();dotnet='/workspace/scratch/dotnet/dotnet'
env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
controls=[('current',None,None,None),('wrong-path','Assets/Resources/BlenderPilot/','Assets/Resources/','outside model path untouched'),('wrong-colorspace','!assetPath.Contains("MetallicSmoothness")','assetPath.Contains("MetallicSmoothness")','atlas sRGB and metallic linear'),('wrong-material','Root+"Pilot_Atlas_Standard.mat"','Root+"Wrong.mat"','shared material exact path and identity'),('lost-sockets','importer.optimizeGameObjects=false','importer.optimizeGameObjects=true','sockets hierarchy retained'),('prop-animation','assetPath.EndsWith("Vanguard.fbx",StringComparison.Ordinal)','true','only hero imports animation')]
results=[]
try:
 for name,old,new,expected in controls:
  if old:assert original.count(old)==1,(name,old)
  source.write_text(original if old is None else original.replace(old,new))
  build=subprocess.run([dotnet,'build',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config'),'-v:q'],cwd=p,env=env,capture_output=True,text=True)
  (p/(name+'.build.log')).write_text(build.stdout+build.stderr);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([dotnet,str(p/'bin/Debug/net8.0/Test.dll')],cwd=p,env=env,capture_output=True,text=True)
  (p/(name+'.stdout.log')).write_text(run.stdout);(p/(name+'.stderr.log')).write_text(run.stderr)
  if expected:assert run.returncode!=0 and 'System.Exception: '+expected in run.stderr,run.stdout+run.stderr
  else:assert run.returncode==0,run.stdout+run.stderr
  results.append({'case':name,'exitCode':run.returncode,'requiredFailure':expected,'pass':True})
  print(('PASS compiled negative: '+name+' -> '+expected) if expected else run.stdout.strip(),flush=True)
finally:source.write_text(original)
manifest={'sourceCommit':'dcd75ac','sourcePath':'Assets/Editor/BlenderPilotImport.cs','sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'boundary':'actual unchanged callback source; UnityEditor importers, AssetDatabase, renderer and menu APIs are managed doubles; no Unity import/render acceptance','results':results}
(p/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
