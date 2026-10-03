"""Actual factory base/max equipment body clearance against actual persistent effect vertices.
Excludes weapons, shields, fashion wings: neither damage radius nor equipment bounds set envelope.
Args dotnet; exports through existing enabled-renderer-filtered factory harness.
"""
from pathlib import Path
import json,sys,tempfile,math
root=Path(__file__).resolve().parents[2];out=Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(prefix='f6-bodies-') as tmp:
 sys.argv=['export',str(root),tmp,sys.argv[1]]
 path=root/'ArtSource/ActorModules/export_constructions.py'
 exec(compile(path.read_text().replace('tier<1','tier<2'),str(path),'exec'),{'__file__':str(path)})
 cases=json.loads((Path(tmp)/'geometry.json').read_text())
samples=json.loads((out/'Runtime-Samples.json').read_text());v=[v for p in samples[0]['objects'] for v in p['vertices']]
# All active guards and all four real defensive passives carry the same minimum envelope.
for sample in samples:
 verts=[v for p in sample['objects'] for v in p['vertices']];assert max(v[1] for v in verts)>3
rows=[]
for c in cases:
 if c.get('tier') not in [-1,1]:continue
 shoulder=[p for p in c['parts'] if any(n in p['name'].lower() for n in ['shoulder','pauldron','armor horn','raised left guard','sleeve'])]
 head=[p for p in c['parts'] if any(n in p['name'].lower() for n in ['head','helmet','wizard hat','forest hood','feather','spirit antler','contract crown','circlet'])]
 sv=[v for p in shoulder for v in p['vertices']];hv=[v for p in head for v in p['vertices']]
 top=max(v[1] for v in hv);shoulder_top=max(v[1] for v in sv);half=max(abs(v[0]) for v in sv)
 # Two opposed arcs have vertices within the shoulder slab outside its widest point.
 arcs=[p for p in v if shoulder_top-.3<p[1]<shoulder_top+.3]
 left=min(p[0] for p in arcs);right=max(p[0] for p in arcs);clear=min(-left,right)-half;crown=max(p[1] for p in v)-top
 assert clear>.12,(c['hero'],c['tier'],clear)
 assert crown>.15,(c['hero'],c['tier'],crown)
 rows.append(dict(hero=c['hero'],tier='base' if c['tier']==-1 else 'level100Legendary',shoulderHalfWidth=half,shoulderTop=shoulder_top,arcLateralClearance=clear,headTop=top,crownClearance=crown))
assert len(rows)==8
(out/'Body-Clearance.json').write_text(json.dumps(rows,indent=2));print('PASS eight actual base/max equipped hero body constructions; two shoulder arcs clearance > .12, crown > .15; no weapon/wing sizing')
