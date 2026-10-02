import argparse,subprocess,json,hashlib,datetime,tarfile,io
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--windows',required=True);p.add_argument('--ios',required=True);p.add_argument('--android',required=True);p.add_argument('--output',required=True);a=p.parse_args();out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
result={'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Tracked source bytes; not Unity import or platform build','platforms':{}}
for platform,path in [('Windows',a.windows),('iOS',a.ios),('Android',a.android)]:
 root=Path(path)
 def git(*args):return subprocess.check_output(['git',*args],cwd=root)
 head=git('rev-parse','HEAD').decode().strip();status=git('status','--porcelain').decode();assert not status,(platform,status)
 files={};normalizations=[]
 with tarfile.open(fileobj=io.BytesIO(git('archive','--format=tar',head))) as archive:
  archived={m.name:archive.extractfile(m).read() for m in archive.getmembers() if m.isfile()}
 for raw in git('ls-tree','-rz',head).split(b'\0'):
  if not raw:continue
  header,name=raw.split(b'\t',1);mode,kind,blob=header.decode().split();n=name.decode();assert kind=='blob' and mode in ('100644','100755'),n
  data=archived[n];work=(root/n).read_bytes();assert work==data,(n,'worktree differs from git archive')
  rawhash=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
  if rawhash!=blob:
   filtered=subprocess.check_output(['git','hash-object','--path',n,'--stdin'],cwd=root,input=data).decode().strip();assert filtered==blob,(n,'unexplained Git blob mismatch');normalizations.append({'path':n,'gitBlob':blob,'archiveRawBlob':rawhash,'attributes':git('check-attr','-a','--',n).decode().strip()})
  files[n]={'gitBlob':blob,'mode':mode,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
 doc={'platform':platform,'head':head,'tree':git('rev-parse','HEAD^{tree}').decode().strip(),'trackedFiles':len(files),'trackedBytes':sum(x['bytes'] for x in files.values()),'clean':True,'gitAttributeConversions':normalizations,'files':files};dest=out/(platform+'-source-inventory.json');dest.write_text(json.dumps(doc,indent=2)+'\n');result['platforms'][platform]={k:v for k,v in doc.items() if k!='files'};result['platforms'][platform]['inventorySha256']=hashlib.sha256(dest.read_bytes()).hexdigest()
(out/'platform-inventory-summary.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
