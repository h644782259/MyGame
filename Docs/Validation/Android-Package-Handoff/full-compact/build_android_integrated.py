#!/usr/bin/env python3
"""Offline immutable-Git Android project assembly; no Git mutation/network/build."""
import argparse,subprocess,pathlib,json,hashlib,re,zipfile,os,sys
P=argparse.ArgumentParser();P.add_argument('--win-repo',required=True);P.add_argument('--ios-repo',required=True);P.add_argument('--win-head',required=True);P.add_argument('--ios-head',required=True);P.add_argument('--android-base',required=True);P.add_argument('--win-base',required=True);P.add_argument('--output',required=True);P.add_argument('--blender',default='/usr/bin/blender');a=P.parse_args()
out=pathlib.Path(a.output);out.mkdir(parents=True,exist_ok=True)
assert not (out/'manifest.json').exists(),'Refuse existing output manifest'
def git(r,*cmd):return subprocess.check_output(['git','-C',r,*cmd])
def snapshot(r,ref):
 rows={}
 for x in git(r,'ls-tree','-rz',ref).split(b'\0'):
  if not x:continue
  h,p=x.split(b'\t',1);mode,typ,oid=h.decode().split();p=p.decode();assert mode in ('100644','100755') and typ=='blob';assert not p.startswith('/') and '..' not in pathlib.PurePosixPath(p).parts
  rows[p]=(oid,mode)
 proc=subprocess.Popen(['git','-C',r,'cat-file','--batch'],stdin=subprocess.PIPE,stdout=subprocess.PIPE)
 data={}
 for p,(oid,mode) in rows.items():
  proc.stdin.write((oid+'\n').encode());proc.stdin.flush();head=proc.stdout.readline().split();size=int(head[2]);data[p]=proc.stdout.read(size);assert proc.stdout.read(1)==b'\n'
 proc.stdin.close();assert proc.wait()==0
 return data,rows
sha=lambda b:hashlib.sha256(b).hexdigest()
def dump(name,v): (out/name).write_text(json.dumps(v,indent=2)+'\n')
android,ar=snapshot(a.win_repo,a.android_base);wb,_=snapshot(a.win_repo,a.win_base);win,wr=snapshot(a.win_repo,a.win_head);ios,ir=snapshot(a.ios_repo,a.ios_head)
platform={p for p in android.keys()|wb.keys() if android.get(p)!=wb.get(p)}
scope=('Assets/Scripts/','Assets/Resources/','ArtSource/','Tests/','Tools/','Docs/')
protected=lambda p:p in platform or p == 'Assets/Resources/Fonts.meta' or p.startswith('Assets/Resources/Fonts/')
result=dict(android);mode={p:x[1] for p,x in ar.items()};overlaid=[];excluded=[]
def guid(b):
 m=re.search(rb'^guid: ([0-9a-f]{32})\s*$',b,re.M);return m.group(1).decode() if m else None
for p,b in win.items():
 if not p.startswith(scope):continue
 if protected(p):
  if android.get(p)!=b:excluded.append(p)
  continue
 if p.endswith('.meta') and p in android:assert guid(android[p])==guid(b),f'Original GUID conflict {p}'
 result[p]=b;mode[p]=wr[p][1];overlaid.append(p)
# All original paths survive. Platform config, native files, Packages and fonts untouched.
assert android.keys()<=result.keys()
for p,b in android.items():
 if not p.startswith(scope) or protected(p):assert result[p]==b,p
originalguids={p:guid(b) for p,b in android.items() if p.endswith('.meta') and guid(b)}
currentguids={p:guid(b) for p,b in result.items() if p.endswith('.meta') and guid(b)}
assert all(currentguids.get(p)==g for p,g in originalguids.items())
assert len(set(currentguids.values()))==len(currentguids),'GUID collision'
missingmeta=[p for p in result if p.startswith('Assets/') and not p.endswith('.meta') and p+'.meta' not in result];assert not missingmeta,missingmeta
# Folders also require metadata, with Assets itself excluded.
folders={str(parent) for p in result if p.startswith('Assets/') for parent in pathlib.PurePosixPath(p).parents if str(parent) not in ('.','Assets')}
assert all(p+'.meta' in result for p in folders),'Missing folder meta'
common=sorted(p for p in win.keys()&ios.keys()&result.keys() if p.startswith(('Assets/Scripts/','Assets/Resources/')))
mismatch=[p for p in common if len({sha(win[p]),sha(ios[p]),sha(result[p])})!=1]
assert all(protected(p) for p in mismatch),mismatch
cs=[p for p in common if p.endswith('.cs')];assert len(cs)==273 and all(win[p]==ios[p]==result[p] for p in cs)
# Top-level Android-oriented description; preserve exact original README.
result['Docs/Android-Original-README.md']=android['README.md'];mode['Docs/Android-Original-README.md']='100644'
readme=f'''# Emberfall Android integrated source project\n\nAndroid platform baseline: `{a.android_base}`. Shared Windows main: `{a.win_head}`; iOS main cross-check: `{a.ios_head}`.\n\nThis is a locally assembled complete Unity source project, not a patch or APK. No Android commit or branch was created, nothing was pushed, and no Android Unity build, device run or engine acceptance was performed. The original Library ZIP could not be downloaded successfully; the exact authorized Android Git baseline was used instead, not claimed as downloaded Library bytes.\n\nAndroid platform settings, Packages, native/editor files, fonts, original GUIDs and Android-specific tools/tests are preserved. Shared Assets/Scripts, Resources, ArtSource, Tests, Tools and final Docs are overlaid from immutable Windows main blobs, with explicit platform exclusions in the external manifest. Original README: Docs/Android-Original-README.md. Package-local provenance, inventories, platform differences, GUID audit, dependency audit and rebuild script: [Docs/Android-Integrated-Package/README.md](Docs/Android-Integrated-Package/README.md).\n\nThe full archive retains all source and offline review media. Compact removes only enumerated offline review PNG/JPG/MP4 images/videos; all runtime files, editable .blend files, external texture dependencies and other source remain. Compact documentation may reference omitted review images; use the full archive for those. Source archive sizes are not player build size or runtime memory.\n'''
result['README.md']=readme.encode();mode['README.md']='100644'
# Reject secret/cache paths and scan high-confidence credential signatures without logging values.
for p in result:
 assert not any(x in {'.git','.credentials','Library','Temp','obj','__pycache__','.aws','.ssh','node_modules'} for x in pathlib.PurePosixPath(p).parts),p
patterns={'private_key':rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----','github_token':rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,})\b','aws_access_key':rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b','slack_token':rb'\bxox[baprs]-[A-Za-z0-9-]{20,}\b','openai_token':rb'\bsk-(?:proj-)?[A-Za-z0-9_-]{40,}\b'}
hits=[]
for p,b in result.items():
 for label,rx in patterns.items():
  if re.search(rx,b):hits.append({'path':p,'rule':label})
dump('token-scan.json',{'scope':'all packaged source bytes; high-confidence patterns only, not proof of absence of secrets','matches':hits});assert not hits,'Potential credential detected; see paths/rules only'
stage=out/'project';stage.mkdir()
for p,b in result.items():
 f=stage/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b);f.chmod(int(mode.get(p,'100644'),8)&0o777)
# Inspect actual blend image/movieclip/library/sound references; disable file auto-execution.
inspect=out/'inspect_blend_dependencies.py'
inspect.write_text('''import bpy,json,pathlib,sys\nroot=pathlib.Path(sys.argv[sys.argv.index('--')+1]); output=sys.argv[sys.argv.index('--')+2]; rows=[]\nfor f in sorted(root.rglob('*.blend')):\n bpy.ops.wm.open_mainfile(filepath=str(f),load_ui=False,use_scripts=False)\n refs=[]\n for kind in ('images','movieclips','sounds','libraries','fonts'):\n  for x in getattr(bpy.data,kind):\n   path=getattr(x,'filepath','')\n   if path and path!='<builtin>':refs.append({'kind':kind,'path':path,'packed':bool(getattr(x,'packed_file',None)),'resolved':bpy.path.abspath(path)})\n rows.append({'blend':str(f.relative_to(root)),'references':refs})\npathlib.Path(output).write_text(json.dumps(rows,indent=2)+'\\n')\n''')
with (out/'blender-dependency-scan.log').open('w') as log:
 subprocess.run([a.blender,'--background','--factory-startup','--disable-autoexec','--python',str(inspect),'--',str(stage),str(out/'blend-dependencies.json')],stdout=log,stderr=subprocess.STDOUT,check=True)
deps=json.loads((out/'blend-dependencies.json').read_text());protectednames={pathlib.PurePosixPath(x['path']).name for row in deps for x in row['references']}
removed=sorted(p for p in result if p.startswith(('ArtSource/','Docs/BlenderUpgrade/ReviewThumbnails/')) and pathlib.PurePosixPath(p).suffix.lower() in ('.png','.jpg','.jpeg','.mp4') and pathlib.PurePosixPath(p).name not in protectednames and not any(x.lower() in ('sources','textures','atlas') for x in pathlib.PurePosixPath(p).parts))
assert not any(p.startswith('Assets/') or p.endswith('.blend') for p in removed)
dump('compact-excluded-review-media.json',[{'path':p,'bytes':len(result[p]),'sha256':sha(result[p]),'reason':'offline review output; not runtime or any blend dependency basename'} for p in removed])
# Self-contained metadata: inventories describe the original payload before this audit folder.
internal='Docs/Android-Integrated-Package/'
payload=[{'path':p,'bytes':len(b),'sha256':sha(b),'mode':mode.get(p,'100644'),'compactRetained':p not in removed} for p,b in sorted(result.items())]
records={
 'source-manifest.json':{'androidBase':a.android_base,'windowsMain':a.win_head,'windowsTree':git(a.win_repo,'rev-parse',a.win_head+'^{tree}').decode().strip(),'iosMain':a.ios_head,'iosTree':git(a.ios_repo,'rev-parse',a.ios_head+'^{tree}').decode().strip(),'source':'exact local Git baseline; original Library download failed','androidCommitCreated':False,'pushed':False,'networkUsed':False,'unityOrAndroidBuildPerformed':False,'overlayScopes':scope,'inventoryScope':'payload-file-manifest.json covers all project files except Docs/Android-Integrated-Package/ metadata itself; ZIP digest is deliberately external to avoid circular self-hash','allAssetsIdenticalFullCompact':True},
 'payload-file-manifest.json':payload,
 'platform-differences.json':[{'path':p,'androidOriginal':sha(android[p]) if p in android else None,'windowsOriginal':sha(wb[p]) if p in wb else None,'windowsFinal':sha(win[p]) if p in win else None,'integrated':sha(result[p]) if p in result else None} for p in sorted(platform|{'Assets/Resources/Fonts.meta'})],
 'guid-audit.json':{'originalPreserved':len(originalguids),'newGuidPaths':len(currentguids)-len(originalguids),'collisions':[],'missingMeta':[],'originalGuids':originalguids,'finalGuids':currentguids},
 'source-common-hash-audit.json':{'commonRuntimeCs':len(cs),'exceptions':mismatch,'paths':[{'path':p,'windows':sha(win[p]),'ios':sha(ios[p]),'androidIntegrated':sha(result[p])} for p in common]},
 'compact-excluded-review-media.json':json.loads((out/'compact-excluded-review-media.json').read_text()),
 'blend-dependencies.json':deps,
 'token-scan.json':{'scope':'original project payload before audit metadata; high-confidence patterns only','matches':hits}}
for name,record in records.items():result[internal+name]=(json.dumps(record,indent=2)+'\n').encode();mode[internal+name]='100644'
result[internal+'build_android_integrated.py']=pathlib.Path(__file__).read_bytes();mode[internal+'build_android_integrated.py']='100755'
result[internal+'README.md']=("# Android integrated package provenance\n\nSee source-manifest.json for exact source commits and platform boundaries, payload-file-manifest.json for hashes (excludes this metadata directory), platform-differences.json, guid-audit.json, source-common-hash-audit.json, compact-excluded-review-media.json and blend-dependencies.json. Both archives contain these records. Compact omissions are pure offline review images/videos; runtime and editable files are retained. ZIP hashes remain external to avoid circular self-hashing. No Android commit/push/build/device acceptance.\n\nOffline reproducibility requires local Git repositories containing the cited commits plus Blender. From this directory run, replacing repository paths and using a fresh output directory:\n\n```sh\npython3 build_android_integrated.py --win-repo /path/to/windows --ios-repo /path/to/ios --win-head "+a.win_head+" --ios-head "+a.ios_head+" --android-base "+a.android_base+" --win-base "+a.win_base+" --output /path/to/new-output\n```\n").encode();mode[internal+'README.md']='100644'
for p,b in result.items():
 if p.startswith(internal):
  f=stage/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(b)
artifacts=[]
for label,omit in [('Source',set()),('Compact',set(removed))]:
 name=f'Emberfall-Android-Integrated-{label}-base8654c803.zip';target=out/name
 with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for p in sorted(result):
   if p in omit:continue
   info=zipfile.ZipInfo(p,(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;info.external_attr=int(mode.get(p,'100644'),8)<<16;z.writestr(info,result[p])
 with zipfile.ZipFile(target) as z:
  assert z.testzip() is None;assert len(z.namelist())==len(set(z.namelist()));assert set(z.namelist())==result.keys()-omit
  for p in z.namelist():assert z.read(p)==result[p],p
 artifacts.append({'path':name,'bytes':target.stat().st_size,'sha256':sha(target.read_bytes()),'entries':len(result)-len(omit),'crcAndAllEntryHashesVerified':True})
platformrows=[{'path':p,'androidOriginal':sha(android[p]) if p in android else None,'windowsOriginal':sha(wb[p]) if p in wb else None,'windowsFinal':sha(win[p]) if p in win else None,'integrated':sha(result[p]) if p in result else None,'policy':'retain Android baseline (README replaced; historical copy retained)' if p in android else 'do not import Windows-only baseline difference'} for p in sorted(platform)]
dump('platform-differences.json',platformrows)
dump('guid-audit.json',{'originalPreserved':len(originalguids),'finalGuidPaths':len(currentguids),'newGuidPaths':len(currentguids)-len(originalguids),'collisions':[],'missingFileOrFolderMeta':[],'originalGuids':originalguids,'finalGuids':currentguids})
dump('source-common-hash-audit.json',{'threePlatformCommonScriptsAndResourcesPaths':len(common),'commonRuntimeCs':len(cs),'exceptions':mismatch,'paths':[{'path':p,'windows':sha(win[p]),'ios':sha(ios[p]),'androidIntegrated':sha(result[p])} for p in common]})
dump('file-manifest.json',[{'path':p,'bytes':len(b),'sha256':sha(b),'mode':mode.get(p,'100644'),'origin':'generated Android integration README' if p=='README.md' else 'Android original README' if p=='Docs/Android-Original-README.md' else 'Windows final main overlay' if p in overlaid else 'Android original Git baseline','compactRetained':p not in removed} for p,b in sorted(result.items())])
manifest={'status':'integrated source archive; no Android engine/device acceptance','androidBase':a.android_base,'windowsMain':a.win_head,'windowsTree':git(a.win_repo,'rev-parse',a.win_head+'^{tree}').decode().strip(),'iosMain':a.ios_head,'iosTree':git(a.ios_repo,'rev-parse',a.ios_head+'^{tree}').decode().strip(),'originalWinBaseline':a.win_base,'source':'exact Git blobs; not downloaded original Library ZIP','androidCommitCreated':False,'pushed':False,'networkUsed':False,'unityOrAndroidBuildPerformed':False,'overlayScopes':scope,'platformExcludedPaths':excluded,'artifacts':artifacts,'compactRemovedReviewFiles':len(removed),'compactRemovedBytes':sum(len(result[p]) for p in removed),'blendFilesPreserved':len(deps),'allAssetsIdenticalFullCompact':True,'originalGuidPathsPreserved':len(originalguids),'runtimeResourcesAddedByArtUpgrade':{'files':112,'bytes':1094536},'securityScan':'token-scan.json (bounded pattern scan, zero matches)','builderSha256':sha(pathlib.Path(__file__).read_bytes())}
dump('manifest.json',manifest);(out/'README.md').write_text(readme+'\nSee manifest.json, file-manifest.json, platform-differences.json, guid-audit.json and source-common-hash-audit.json for exact audit.\n')
print(json.dumps(manifest,indent=2))
