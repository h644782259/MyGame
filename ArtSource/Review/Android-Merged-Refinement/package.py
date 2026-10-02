from pathlib import Path, PurePosixPath
import subprocess, io, tarfile, zipfile, hashlib, json, re
root=Path('/workspace/emberfall_android_refinement_delivery');out=Path(__file__).parent
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
manifest={'sourceCommit':head,'runtimeCandidate':'57445429957bbc2c65a471e7ae5affeb9df841c3','windowsMain':'55d621f06068dd7ff2c3dd77ab6b0376303581d3','iosMain':'de5bbbf8ec8631c055ed653a3dc6fdf56d056246','fullSuite':{'checks':189,'passed':True,'testedHead':'e325c904ab5ee1a45aaf354cbaf3556677b407d9','reportSha256':'ad23b50a1ecee64a83179fd08fb12b7802f664457253f3a99bf8423bd6566e6e','sourceChangedDuringRun':[]},'sourceOnly':True,'unityEditorVerified':False,'apkProduced':False,'deviceVerified':False,'includesPR23':False,'deliveryChange':'Docs/Art-Replay-Refinement.md only; runtime/assets/platform settings unchanged','trackedFiles':len(entries),'filename':filename}
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
