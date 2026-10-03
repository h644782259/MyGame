import sys,importlib.util,pathlib,subprocess,os,tempfile,json,hashlib
root=pathlib.Path(sys.argv[1]);spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
sources=sorted((root/'Assets/Scripts').rglob('*.cs'));refs=list(m.unity_references(False).glob('*.dll'));sdk='/workspace/shared/emberfall-tools/dotnet/dotnet';output=pathlib.Path(sys.argv[2]);output.mkdir(parents=True,exist_ok=True);report={'scope':'Pinned Unity 2021.3.33 API compilation only; not Unity 6 or engine execution','sources':{str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},'checks':[]}
with tempfile.TemporaryDirectory(prefix='art-compile-') as d:
 w=pathlib.Path(d);cfg=w/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(w/'cli'),DOTNET_NOLOGO='1')
 for name,defs in [('win','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios','UNITY_IOS'),('android','UNITY_ANDROID')]:
  project=m.write_project(w/name,sources,references=refs,defines=defs)
  with (output/(name+'-compile.log')).open('w') as f:
   restore=subprocess.run([sdk,'restore',str(project),'--configfile',str(cfg),'-v:q'],env=env,stdout=f,stderr=subprocess.STDOUT)
   result=subprocess.run([sdk,'build',str(project),'--no-restore','-v:minimal'],env=env,stdout=f,stderr=subprocess.STDOUT)
  report['checks'].append({'platform':name,'restore':restore.returncode,'build':result.returncode});print(name,result.returncode,flush=True)
report['sourceStable']=all(hashlib.sha256((root/p).read_bytes()).hexdigest()==h for p,h in report['sources'].items());(output/'compile-report.json').write_text(json.dumps(report,indent=2)+'\n')
