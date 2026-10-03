"""Read-only targeted intermediate integration check. dotnet pinned-ref-directory."""
from pathlib import Path
import subprocess,sys,os,datetime,json,tempfile,importlib.util,concurrent.futures,hashlib
root=Path(__file__).resolve().parents[3];out=Path(__file__).resolve().parent
dotnet=sys.argv[1];refs=sorted(Path(sys.argv[2]).glob('*.dll'));assert refs
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()
report={'source_head':head,'scope':'intermediate PR33/34/36 + newest main/art; excludes pendingLoot followup 325544e and future A/B/C; not Unity editor/device','started_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'tests':{},'api':{},'refs':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in refs}}
tests=['CompanionIntentProductionTests','ChapterCombatProductionTests','BossSweepCapsuleProductionTests','ChapterHostProductionTests','ChapterEntryProductionTests','ChapterReturnTimeScaleTests','IntegratedJourneyTests']
def run_test(name):
 cmd=[sys.executable,str(root/'Tests'/(name+'.py')),dotnet]
 with (out/(name+'.log')).open('w') as log:
  log.write('SOURCE_HEAD '+head+'\nCOMMAND '+repr(cmd)+'\n');log.flush()
  with tempfile.TemporaryDirectory(prefix='targeted-cli-') as cli:
   r=subprocess.run(cmd,cwd=root,stdout=log,stderr=subprocess.STDOUT,env=dict(os.environ,DOTNET_CLI_HOME=cli,DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1'))
 return name,{'exit':r.returncode,'command':cmd}
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
 for name,result in pool.map(run_test,tests):report['tests'][name]=result;print(name,result['exit'],flush=True)
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
sources=sorted((root/'Assets/Scripts').rglob('*.cs'))
report['runtime_source_sha256']={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
with tempfile.TemporaryDirectory(prefix='published-art-api-') as temp:
 w=Path(temp);cfg=w/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 for name,defines in [('win','UNITY_STANDALONE;UNITY_STANDALONE_WIN'),('ios','UNITY_IOS'),('android','UNITY_ANDROID')]:
  project=cv.write_project(w/name,sources,references=refs,defines=defines);env=dict(os.environ,DOTNET_CLI_HOME=str(w/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');codes=[]
  with (out/(name+'-api.log')).open('w') as log:
   log.write('SOURCE_HEAD '+head+'\nPINNED Unity 2021.3 API ONLY; NOT Unity 6/device\nDEFINES '+defines+'\n');log.flush()
   for cmd in [[dotnet,'restore',str(project),'--configfile',str(cfg),'-v:q'],[dotnet,'build',str(project),'--no-restore','-v:minimal']]:codes.append(subprocess.run(cmd,cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT).returncode)
  report['api'][name]={'defines':defines,'restore_build_exit':codes};print(name,codes,flush=True)
report['completed_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat();(out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
assert all(t['exit']==0 for t in report['tests'].values()) and all(t['restore_build_exit']==[0,0] for t in report['api'].values()),'see raw logs/report'
