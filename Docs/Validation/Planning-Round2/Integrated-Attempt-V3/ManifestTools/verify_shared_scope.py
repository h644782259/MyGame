import pathlib,subprocess,hashlib,json,re,sys
base=pathlib.Path('/workspace/scratch/planning-round2');win=base/'validation-v3';ios=base/'ios';android=base/'android-source';report={}
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
for scope in ['Assets/Scripts','Assets/Resources','Tests','ArtSource']:
 paths=[p for p in subprocess.check_output(['git','ls-files',scope],cwd=win,text=True).splitlines() if (win/p).is_file()]
 mismatches={k:[p for p in paths if not (root/p).is_file() or digest(win/p)!=digest(root/p)] for k,root in [('ios',ios),('android',android)]}
 report[scope]={'trackedFileCount':len(paths),'mismatches':mismatches}
metas=subprocess.check_output(['git','ls-files','*.meta'],cwd=win,text=True).splitlines();baseline='5d85e47b9fab2489d5b06963a0b896ec19112740';changed=[];added={}
old=set(subprocess.check_output(['git','ls-tree','-r','--name-only',baseline],cwd=win,text=True).splitlines())
for p in metas:
 content=(win/p).read_text();g=re.search(r'^guid: (.+)$',content,re.M);guid=g.group(1) if g else None
 if p not in old:added[p]=guid;continue
 prev=subprocess.check_output(['git','show',baseline+':'+p],cwd=win,text=True);g2=re.search(r'^guid: (.+)$',prev,re.M)
 if guid!=(g2.group(1) if g2 else None):changed.append(p)
report['guid']={'checkedMetas':len(metas),'existingChanged':changed,'newMetas':added}
jsonChanges=subprocess.check_output(['git','diff','--name-only',baseline,'HEAD','--','*.json'],cwd=win,text=True).splitlines()
report['historicalTrackedJsonChanged']=[p for p in jsonChanges if p in old]
report['newEvidenceJson']=[p for p in jsonChanges if p not in old]
report['preexistingPlatformDifferences']={}
for platform,root,oldRoot,oldCommit in [('ios',ios,None,'d3a4aab185eb368f5a4a7aba67b43678bfea508f'),('android',android,pathlib.Path('/workspace/scratch/android-final-packages-v2/project'),None)]:
 for scope in ['Assets/Scripts','Assets/Resources','Tests','ArtSource']:
  for p in report[scope]['mismatches'][platform]:
   beforeBytes=subprocess.check_output(['git','show',oldCommit+':'+p],cwd=root) if oldCommit else (oldRoot/p).read_bytes()
   report['preexistingPlatformDifferences'][platform+':'+p]={'currentSha256':digest(root/p),'beforeSha256':hashlib.sha256(beforeBytes).hexdigest(),'unchanged':(root/p).read_bytes()==beforeBytes}
assert not report['historicalTrackedJsonChanged']
assert all(v['unchanged'] for v in report['preexistingPlatformDifferences'].values())
(pathlib.Path(sys.argv[1])/'shared-scope-and-guids.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='guid'},indent=2));print('Existing GUID changes',changed,'New meta count',len(added))
