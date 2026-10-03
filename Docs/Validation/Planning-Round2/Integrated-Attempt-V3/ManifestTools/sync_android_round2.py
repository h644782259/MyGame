import pathlib,subprocess,hashlib,json,shutil,sys
base=pathlib.Path('/workspace/scratch/planning-round2');win=base/'validation-v3';ios=base/'ios';android=base/'android-source'
changes=subprocess.check_output(['git','diff','--name-only','5d85e47b9fab2489d5b06963a0b896ec19112740','HEAD'],cwd=win,text=True).splitlines()
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest() if p.is_file() else None
before={str(p.relative_to(android)):digest(p) for part in ['ProjectSettings','Packages','Assets/Editor'] for p in (android/part).rglob('*') if p.is_file()}
assert not any((p.startswith(('ProjectSettings/','Packages/','Assets/Editor/'))) for p in changes)
assert not (android/'.git').exists()
for rel in changes:
 src=win/rel; dst=android/rel
 if src.is_file():dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
 elif dst.exists():dst.unlink()
checks={rel:{'windows':digest(win/rel),'ios':digest(ios/rel),'android':digest(android/rel)} for rel in changes}
assert all(len(set(v.values()))==1 for v in checks.values())
after={str(p.relative_to(android)):digest(p) for part in ['ProjectSettings','Packages','Assets/Editor'] for p in (android/part).rglob('*') if p.is_file()}
assert before==after
report={'windowsSourceHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=win,text=True).strip(),'iosSourceHead':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ios,text=True).strip(),'androidOriginalBase':'8654c803eb29b87dea7d69f09678ec21e1955f6d','androidPath':str(android),'method':'Copy only this round changed shared files from verified Windows source; preserve all other local Android files. No Git repo, archive, remote, login or credential operation.','changedFileCount':len(changes),'changedFiles':checks,'preservedAndroidPlatformFiles':before,'platformFilesUnchanged':before==after}
(pathlib.Path(sys.argv[1])/'platform-sync.json').write_text(json.dumps(report,indent=2)+'\n');print('Synced identical changed files:',len(changes),'preserved platform files:',len(before))
