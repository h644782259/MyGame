"""Export actual candidate policy weights using actual baseline locomotion inputs."""
from pathlib import Path
import os,sys,subprocess,json,shutil,hashlib
base,candidate,out=map(lambda v:Path(v).resolve(),sys.argv[1:4]);dotnet=sys.argv[4] if len(sys.argv)>4 else 'dotnet';p=out/'policy-export';p.mkdir(exist_ok=True,parents=True)
inspect=json.loads((out/'rig-inspection.json').read_text());extent=inspect['actions']['Pilot_Idle']['range'];length=(extent[1]-extent[0])*inspect['fpsBase']/inspect['sceneFPS']
for path in [candidate/'Assets/Scripts/Core/BlenderPilotPosePolicy.cs',base/'Assets/Scripts/Core/LocomotionPoseState.cs',Path(__file__).with_name('PolicyExport.cs')]:shutil.copy2(path,p/path.name)
(p/'Export.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
subprocess.run([dotnet,'run','--project',str(p/'Export.csproj'),'--',str(out),str(length)],env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli')),check=True)
h=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
report={'candidateCommit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=candidate,text=True).strip(),'policySha256':h(candidate/'Assets/Scripts/Core/BlenderPilotPosePolicy.cs'),'visualSha256':h(candidate/'Assets/Scripts/Combat/BlenderPilotVisual.cs'),'idleLength':length,'idleLengthSource':'actual blend frame range / scene fps including fps_base','motionSha256':h(out/'motion.json'),'samplesSha256':h(out/'candidate-samples.json'),'stopSamplesSha256':h(out/'candidate-stop-samples.json')};(out/'reproduced-policy-manifest.json').write_text(json.dumps(report,indent=2))
