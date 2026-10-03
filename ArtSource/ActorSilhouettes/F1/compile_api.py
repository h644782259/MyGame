"""Pinned 2021.3 API compile, not Unity 6 engine/device validation. dotnet refs-dir"""
import importlib.util,pathlib,subprocess,os,tempfile,json,sys
root=pathlib.Path(__file__).resolve().parents[3];out=pathlib.Path(__file__).parent
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
sources=sorted((root/'Assets/Scripts').rglob('*.cs'));refs=list(pathlib.Path(sys.argv[2]).glob('*.dll'));assert refs
report={}
with tempfile.TemporaryDirectory(prefix='f1-compile-') as d:
 w=pathlib.Path(d);cfg=w/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(w/'cli'),DOTNET_NOLOGO='1')
 for name,defs in [('win','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios','UNITY_IOS'),('android','UNITY_ANDROID')]:
  project=m.write_project(w/name,sources,references=refs,defines=defs)
  with (out/(name+'-api.log')).open('w') as f:
   a=subprocess.run([sys.argv[1],'restore',str(project),'--configfile',str(cfg),'-v:q'],env=env,stdout=f,stderr=subprocess.STDOUT)
   b=subprocess.run([sys.argv[1],'build',str(project),'--no-restore','-v:minimal'],env=env,stdout=f,stderr=subprocess.STDOUT)
  report[name]=[a.returncode,b.returncode];assert a.returncode==b.returncode==0,(name,report[name])
print('PASS pinned Unity2021.3 API build on3 platform defines; NOT Unity6/device execution',report)
