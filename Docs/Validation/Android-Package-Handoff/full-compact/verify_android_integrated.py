#!/usr/bin/env python3
import pathlib,zipfile,json,hashlib,subprocess
root=pathlib.Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
manifest=json.loads((root/'manifest.json').read_text()); files=json.loads((root/'file-manifest.json').read_text()); expected={x['path']:x for x in files}
reports=[]
for a in manifest['artifacts']:
 p=root/a['path'];assert p.stat().st_size==a['bytes'] and sha(p.read_bytes())==a['sha256']
 with zipfile.ZipFile(p) as z:
  assert z.testzip() is None;names=z.namelist();assert len(names)==len(set(names))
  want={p for p,r in expected.items() if 'Compact' not in a['path'] or r['compactRetained']};assert set(names)==want
  for name in names:
   assert not name.startswith('/') and '..' not in pathlib.PurePosixPath(name).parts and '\\' not in name
   assert sha(z.read(name))==expected[name]['sha256']
  reports.append({'archive':a['path'],'files':len(names),'crc':True,'safeUniquePaths':True,'allFileHashes':True})
print(json.dumps(reports,indent=2))
