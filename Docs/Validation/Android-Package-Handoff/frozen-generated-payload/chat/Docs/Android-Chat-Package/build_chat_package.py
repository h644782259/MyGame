#!/usr/bin/env python3
"""Derive runtime-complete Unity source ZIP, offline, preserving frozen source ZIP."""
import argparse,pathlib,zipfile,hashlib,json,re
p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--output',required=True);a=p.parse_args();out=pathlib.Path(a.output);out.mkdir(parents=True,exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest();source=pathlib.Path(a.source);sourcehash=sha(source.read_bytes());assert sourcehash=='c58a7542ecebfca4e03880d305cf77f527906bb92c143d9bb28a0089fcbb4e94'
with zipfile.ZipFile(source) as z:
 assert z.testzip() is None;original={i.filename:z.read(i) for i in z.infolist()};modes={i.filename:i.external_attr for i in z.infolist()};assert len(original)==len(z.infolist())
keepdocs={'Docs/Android-Development.md','Docs/Android-Lifecycle.md','Docs/Android-Toolchain-Report.json'}
excluded=[{'path':p,'bytes':len(b),'sha256':sha(b),'reason':'offline Blender/authoring/review source (full editable archive required)' if p.startswith('ArtSource/') else 'historical documentation/review logs or superseded full-package metadata'} for p,b in sorted(original.items()) if p.startswith('ArtSource/') or p.startswith('Docs/') and p not in keepdocs]
excludedpaths={r['path'] for r in excluded};data={p:b for p,b in original.items() if p not in excludedpaths};assert all(data[p]==b for p,b in original.items() if p.startswith(('Assets/','ProjectSettings/','Packages/','Tools/','Tests/')))
internal='Docs/Android-Chat-Package/'
readme='''# Emberfall Android runtime-complete Unity source project\n\nThis small chat-delivery archive contains the complete runtime Assets (including FBX, Resources and fonts), Android ProjectSettings and Packages, all existing build tools and test source. Open the project with the Unity version recorded in ProjectSettings/ProjectVersion.txt; Android build support and the SDK/NDK/JDK toolchain must be installed separately. No Unity build or Android device validation was performed for this archive. It is source, not an APK.\n\nIt is NOT the full editable art-source bundle. Offline ArtSource (including .blend files), review media and historical documentation/logs are omitted. Use the original full or compact Library source items for editable Blender sources; those local v2 packages are preserved unchanged. This task did not upload or update any Library item.\n\nAndroid baseline: 8654c803eb29b87dea7d69f09678ec21e1955f6d. Shared Windows main: 5d85e47b9fab2489d5b06963a0b896ec19112740. iOS cross-check: d3a4aab185eb368f5a4a7aba67b43678bfea508f. This is an assembled source project; no Android commit, branch or push was created. Original Library ZIP download was unsuccessful; the v2 source package uses exact authorized Git blobs.\n\n[Exact exclusions](Docs/Android-Chat-Package/excluded-files.json), [runtime preservation audit](Docs/Android-Chat-Package/runtime-preservation.json), [source manifest](Docs/Android-Chat-Package/source-manifest.json), and [offline builder](Docs/Android-Chat-Package/build_chat_package.py) are included in this ZIP. Offline art-generation and visual-evidence tools remain as source for completeness but require the omitted authoring bundle; runtime/project build inputs are retained.\n'''
data['README.md']=readme.encode();(out/'README.md').write_text(readme)
protected={p:sha(b) for p,b in data.items() if p.startswith(('Assets/','ProjectSettings/','Packages/'))}
guid=lambda b:re.search(rb'^guid: ([0-9a-f]{32})\s*$',b,re.M)
guids={p:guid(b).group(1).decode() for p,b in data.items() if p.startswith('Assets/') and p.endswith('.meta') and guid(b)}
assert len(guids)==len(set(guids.values()));assert all(p+'.meta' in data for p in data if p.startswith('Assets/') and not p.endswith('.meta'))
folders={str(q) for p in data if p.startswith('Assets/') for q in pathlib.PurePosixPath(p).parents if str(q) not in ('.','Assets')};assert all(q+'.meta' in data for q in folders)
sourceguid=json.loads(original['Docs/Android-Integrated-Package/guid-audit.json']);assert guids==sourceguid['finalGuids'];assert all(guids[p]==g for p,g in sourceguid['originalGuids'].items())
audit={'allRuntimeAssetsAndPlatformFilesByteIdenticalToV2':True,'protectedFiles':len(protected),'originalAndroidGuidsPreserved':len(sourceguid['originalGuids']),'totalGuidPaths':len(guids),'duplicateGuids':[],'missingAssetOrFolderMeta':[],'allToolsAndTestsRetained':True,'protectedSha256':protected}
manifest={'sourceArchive':source.name,'sourceArchiveBytes':source.stat().st_size,'sourceArchiveSha256':sourcehash,'androidBase':'8654c803eb29b87dea7d69f09678ec21e1955f6d','windowsMain':'5d85e47b9fab2489d5b06963a0b896ec19112740','iosMain':'d3a4aab185eb368f5a4a7aba67b43678bfea508f','scope':'runtime-complete Unity source project; offline ArtSource and historical Docs excluded; all Assets/Packages/ProjectSettings/Tools/Tests retained','excludedFiles':len(excluded),'excludedUncompressedBytes':sum(x['bytes'] for x in excluded),'notFullEditableSource':True,'apk':False,'unityBuildOrDeviceTest':False,'androidCommitOrPush':False,'uploaded':False,'builderSha256':sha(pathlib.Path(__file__).read_bytes()),'archiveDigest':'external manifest.json only, to avoid circular hashing'}
for name,obj in [('excluded-files.json',excluded),('runtime-preservation.json',audit),('source-manifest.json',manifest)]:
 b=(json.dumps(obj,indent=2)+'\n').encode();data[internal+name]=b;(out/name).write_bytes(b)
data[internal+'build_chat_package.py']=pathlib.Path(__file__).read_bytes()
reproduce='Run offline with Python 3 and the unchanged complete v2 source ZIP:\n\npython3 build_chat_package.py --source /path/to/Emberfall-Android-Integrated-Source-base8654c803.zip --output /path/to/fresh-output\n\nNo network, Unity launch, Git modification or Android access is used. ZIP timestamps/modes/order are deterministic.\n'
data[internal+'REPRODUCE.txt']=reproduce.encode();(out/'REPRODUCE.txt').write_text(reproduce)
target=out/'Emberfall-Android-Runtime-Project-base8654c803.zip';assert not target.exists()
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for p,b in sorted(data.items()):
  assert not p.startswith('/') and '..' not in pathlib.PurePosixPath(p).parts and '\\' not in p
  i=zipfile.ZipInfo(p,(1980,1,1,0,0,0));i.compress_type=zipfile.ZIP_DEFLATED;i.external_attr=modes.get(p,0o100644<<16);z.writestr(i,b)
with zipfile.ZipFile(target) as z:
 assert z.testzip() is None;assert len(z.namelist())==len(set(z.namelist()));assert set(z.namelist())==set(data)
 for p,b in data.items():assert z.read(p)==b
assert target.stat().st_size<20*1024*1024,'Exceeds 20 MiB; do not remove any runtime resources'
manifest.update({'archive':target.name,'bytes':target.stat().st_size,'sha256':sha(target.read_bytes()),'entries':len(data),'crcSafeUniquePathsAndEveryEntryVerified':True,'under20MiB':True})
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');(out/'SHA256SUMS').write_text(''.join(sha(f.read_bytes())+'  '+f.name+'\n' for f in sorted(out.iterdir()) if f.is_file() and f.name!='SHA256SUMS'))
print(json.dumps(manifest,indent=2))
