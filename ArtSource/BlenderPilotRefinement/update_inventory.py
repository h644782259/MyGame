"""Refresh only Vanguard's manifest entry and assert all other asset hashes stable."""
import json,hashlib,sys
from pathlib import Path
root=Path(__file__).resolve().parents[2];source=root/'ArtSource/BlenderPilot'
before=json.loads(Path(sys.argv[1]).read_text());manifest=json.loads((source/'export-manifest.json').read_text())
rows=[]
for item in before['runtime_files']:
 p=root/item['path'];row={'path':item['path'],'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
 if p.name!='Vanguard.fbx':assert row==item,('unexpected nonhero runtime asset change',p)
 rows.append(row)
manifest['runtime_files']=rows;manifest['total_bytes']=sum(r['bytes'] for r in rows)
(source/'export-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
old=next(r for r in before['runtime_files'] if r['path'].endswith('/Vanguard.fbx'));new=next(r for r in rows if r['path'].endswith('/Vanguard.fbx'))
print(json.dumps({'before':old,'after':new,'fbx_bytes_delta':new['bytes']-old['bytes'],'other_runtime_assets_unchanged':True},indent=2))
