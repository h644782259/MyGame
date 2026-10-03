"""Retry only SDK startup failures under a writable temporary CLI cache; no source changes."""
from pathlib import Path
import json,subprocess,tempfile,os,datetime
out=Path(__file__).resolve().parent;root=out.parents[2];report=json.loads((out/'report.json').read_text())
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip()==report['source_head']
for name,result in report['tests'].items():
 if result['exit']==0:continue
 log=out/(name+'.log');initial=log.read_text();assert "Read-only file system : '/home/agent/.dotnet'" in initial
 log.rename(out/(name+'.initial-environment-failure.log'))
 with tempfile.TemporaryDirectory(prefix='targeted-retry-cli-') as cli,log.open('w') as f:
  f.write('SOURCE_HEAD '+report['source_head']+'\nCOMMAND '+repr(result['command'])+'\nDOTNET_CLI_HOME temporary writable cache\n');f.flush()
  r=subprocess.run(result['command'],cwd=root,stdout=f,stderr=subprocess.STDOUT,env=dict(os.environ,DOTNET_CLI_HOME=cli,DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1'))
 result['initial_environment_exit']=result['exit'];result['exit']=r.returncode;print(name,r.returncode,flush=True)
report['retry_completed_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat();(out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
assert all(t['exit']==0 for t in report['tests'].values())
