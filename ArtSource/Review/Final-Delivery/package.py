from pathlib import Path, PurePosixPath
import subprocess, io, tarfile, zipfile, hashlib, json, re
import argparse,csv
cli=argparse.ArgumentParser();cli.add_argument('--root',required=True);cli.add_argument('--metadata',required=True);args=cli.parse_args()
root=Path(args.root).resolve();out=Path(__file__).parent
metadata=json.loads(Path(args.metadata).read_text())
def git(*a):return subprocess.check_output(['git',*a],cwd=root)
assert not git('status','--porcelain').strip()
head=git('rev-parse','HEAD').decode().strip();filename='Emberfall-Android-Source-'+head[:7]+'.zip';archive=out/filename
entries={}
with tarfile.open(fileobj=io.BytesIO(git('archive','--format=tar',head))) as t:
 for m in t.getmembers():
  if m.isdir():continue
  assert m.isfile(),m.name
  p=PurePosixPath(m.name);assert not p.is_absolute() and '..' not in p.parts
  assert not set(x.lower() for x in p.parts)&{'.git','library','temp','obj','logs','sdk','ndk','__pycache__','usersettings'}
  assert not re.search(r'(?i)(\.(p12|pfx|pem|key|keystore|jks|mobileprovision)$|(^|/)(id_rsa|id_ed25519|credentials|auth\.json|\.env)(/|$))',m.name)
  data=t.extractfile(m).read();assert not re.search(rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}',data)
  entries[m.name]=data
assert set(entries)==set(git('ls-tree','-rz','--name-only',head).decode().rstrip('\0').split('\0'))
for n,d in entries.items():
 assert (root/n).read_bytes()==d
 if n.startswith('Assets/') and not n.endswith('.meta'):assert n+'.meta' in entries,n
assert metadata['sourceCommit']==head
assert metadata['fullSuite']['passed'] and metadata['fullSuite']['sourceChangedDuringRun']==[]
asset_dirs={str(parent) for name in entries if name.startswith('Assets/') for parent in PurePosixPath(name).parents if str(parent) not in ('.','Assets')}
assert all(name+'.meta' in entries for name in asset_dirs), 'Missing Assets directory metadata'
manifest=dict(metadata,sourceOnly=True,unityEditorVerified=False,apkProduced=False,deviceVerified=False,trackedFiles=len(entries),filename=filename)
source_inventory={n:{'bytes':len(d),'sha256':hashlib.sha256(d).hexdigest()} for n,d in sorted(entries.items())}
(out/'source-inventory.json').write_text(json.dumps(source_inventory,indent=2)+'\n')
with (out/'asset-inventory.csv').open('w',newline='') as f:
 writer=csv.writer(f,lineterminator='\n');writer.writerow(['sourceCommit','path','bytes','sha256','category'])
 for name,row in source_inventory.items():
  if name.startswith(('ArtSource/','Assets/Art/','Assets/Resources/')) and not name.endswith('.meta'):
   writer.writerow([head,name,row['bytes'],row['sha256'],'authoring/review' if name.startswith('ArtSource/') else 'runtime asset'])
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for n,d in sorted(entries.items()):z.writestr('Emberfall-Android/'+n,d)
 z.writestr('Emberfall-Android/SOURCE-DELIVERY.json',json.dumps(manifest,indent=2)+'\n')
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 assert len(z.namelist())==len(entries)+1
 for n,d in entries.items():assert z.read('Emberfall-Android/'+n)==d
manifest.update(bytes=archive.stat().st_size,sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),entries=len(entries)+1)
(out/('manifest-'+head[:7]+'.json')).write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(manifest,indent=2))
