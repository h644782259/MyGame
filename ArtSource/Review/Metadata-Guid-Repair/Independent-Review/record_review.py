from pathlib import Path
import subprocess,json,hashlib,uuid,datetime
out=Path(__file__).resolve().parent
expected={'/workspace/emberfall_guid_fix':'fcdd97bfbcbeca6b9cf022c86a30766de6c93597','/workspace/emberfall_ios_guid_fix':'887cbbe84bac1f9b26d6aa7ee3d725b742453c93','/workspace/emberfall_android_guid_fix':'8654c803eb29b87dea7d69f09678ec21e1955f6d'}
old=['68a6e6a74a54417a8f1162a6ff67f5dc4','a61004eb35a84c3c821b82421b504a5cf']
metas=['Assets/Scripts/Combat/WeaponVisualLinks.cs.meta','Assets/Scripts/Core/FilledVfxPlacement.cs.meta']
paths=set(metas+['Docs/Metadata-Guid-Repair.md','Tests/MetaGuidAuditTests.py','Tools/cloud-validation.py','Tools/validate-meta-guids.py'])
report={'timeUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),'verdict':'GO','scope':'Every tracked Git blob at each exact candidate and its first parent, byte-search lowercased for both old GUIDs; all changed paths and candidate six-file bytes across platforms. Working copy hash verified against commit for all changed files. Excludes untracked/external scenes, Unity import, full-schema/package/fileID resolution.','commands':[],'platforms':[]}
def run(root,args):
 command=['git','-C',root]+args
 return subprocess.check_output(command)
for root,head in expected.items():
 assert run(root,['rev-parse','HEAD']).decode().strip()==head
 parent=run(root,['rev-parse',head+'^']).decode().strip()
 changed=run(root,['diff-tree','--no-commit-id','--name-only','-r',head]).decode().splitlines();assert set(changed)==paths
 entry={'root':root,'head':head,'parent':parent,'changedPaths':changed,'changedSha256':{},'oldGuidMatches':{},'commands':[['git','-C',root,'diff-tree','--no-commit-id','--name-only','-r',head]]}
 for revision in [parent,head]:
  listing=run(root,['ls-tree','-r','-z',revision]);matches={v:[] for v in old};count=0
  entry['commands'].append(['git','-C',root,'ls-tree','-r','-z',revision]);entry['commands'].append(['git','-C',root,'cat-file','--batch'])
  objects=[]
  for record in listing.split(b'\0'):
   if not record:continue
   meta,name=record.split(b'\t',1);mode,kind,oid=meta.split()
   if kind==b'blob':objects.append((oid,name.decode()))
  proc=subprocess.Popen(['git','-C',root,'cat-file','--batch'],stdin=subprocess.PIPE,stdout=subprocess.PIPE)
  raw,_=proc.communicate(b'\n'.join(oid for oid,name in objects)+b'\n');assert proc.returncode==0
  cursor=0
  for oid,name in objects:
   end=raw.index(b'\n',cursor);header=raw[cursor:end].split();length=int(header[2]);blob=raw[end+1:end+1+length];cursor=end+2+length;count+=1
   for value in old:
    if value.encode() in blob.lower():matches[value].append(name)
  entry['oldGuidMatches'][revision]={'blobCount':count,'matches':matches}
  for i,value in enumerate(old):assert matches[value]==([metas[i]] if revision==parent else ['Docs/Metadata-Guid-Repair.md'])
 entry['newGuids']={}
 for name in changed:
  blob=run(root,['show',head+':'+name]);assert blob==Path(root,name).read_bytes()
  entry['changedSha256'][name]=hashlib.sha256(blob).hexdigest()
  if name in metas:
   value=next(line.split(':',1)[1].strip() for line in blob.decode().splitlines() if line.startswith('guid:'))
   parsed=uuid.UUID(value);assert parsed.version==4;entry['newGuids'][name]={'value':value,'uuidVersion':parsed.version}
 report['platforms'].append(entry)
assert all(e['changedSha256']==report['platforms'][0]['changedSha256'] for e in report['platforms'])
report['threePlatformChangedBytesEqual']=True
report['executions']=[]
for label,args in [('scanner',['python3','Tools/validate-meta-guids.py']),('controls',['python3','Tests/MetaGuidAuditTests.py'])]:
 root=next(iter(expected));command=args;result=subprocess.run(command,cwd=root,capture_output=True,text=True)
 (out/(label+'.stdout.log')).write_text(result.stdout);(out/(label+'.stderr.log')).write_text(result.stderr)
 report['executions'].append({'command':command,'cwd':root,'exitCode':result.returncode,'stdout':label+'.stdout.log','stderr':label+'.stderr.log'})
 assert result.returncode==0,result.stderr
 if label=='scanner':report['scannerResult']=json.loads(result.stdout)
report['commands'].append(['python3',str(out/'record_review.py')]);report['scriptSha256']=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
(out/'review.json').write_text(json.dumps(report,indent=2)+'\n')
print('GO: exact3heads/alltrackedblobs/sixpathparity; scanner +18controls PASS; candidate files unchanged')
