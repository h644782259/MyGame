from pathlib import Path
import ast,importlib.util,subprocess,os
root=Path('/workspace/scratch/planning-round2/win');out=Path(__file__).parent
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
parsed=ast.parse((root/'Tools/cloud-validation.py').read_text())
for node in ast.walk(parsed):
 if isinstance(node,ast.Tuple) and node.elts and isinstance(node.elts[0],ast.Constant) and node.elts[0].value=='chapter-room-geometry':
  sources=eval(compile(ast.Expression(node.elts[1]),'<runner sources>','eval'),{'ROOT':root});program=ast.literal_eval(node.elts[2]);break
else:raise Exception('runner not found')
project=cv.write_project(out/'project',sources,program)
config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
sdk='/workspace/shared/emberfall-tools/dotnet/dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_CLI_TELEMETRY_OPTOUT='1')
for args in [['restore',str(project),'--configfile',str(config),'--verbosity','quiet'],['run','--project',str(project),'--no-restore','--configuration','Release']]:
 result=subprocess.run([sdk,*args],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
 print(result.stdout,end='',flush=True)
 if result.returncode:raise SystemExit(result.returncode)
