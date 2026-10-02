from pathlib import Path
import json,hashlib,math,sys
p=Path(sys.argv[1]);d=json.loads(p.read_text());checks=0
assert d['pin']=='03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e'
assert len(d['frames'])==216 and d['fps']==24 and d['duration']==9
assert [c['swingSide'] for c in d['casts']]==[-1,1,-1,1]
for frame,f in enumerate(d['frames']):
 assert abs(f['time']-frame/24)<.000001
 for o in f['objects']:
  mesh=d['meshes'][o['mesh']]
  assert len(o['vertices'])==len(mesh['vertices'])==len(mesh['uv'])
  assert all(0<=i<len(o['vertices']) for i in mesh['triangles'])
  assert all(math.isfinite(a) for v in o['vertices'] for a in v)
  assert 0<=o['color'][3]<=1 and 0<=o['opacity']<=1
  checks+=1
 for r in f['rings']:
  assert len(r['points'])==64 and r['width'] in [.12,.16]
  assert all(math.isfinite(a) for v in r['points'] for a in v)
  checks+=1
for cast in d['casts']:
 f=d['frames'][round(cast['time']*24)]
 assert len(f['rings'])==1 and len(f['objects'])==(0 if cast['slot']==0 else 1)
 assert abs(f['rings'][0]['color'][3]-(.66 if cast['slot']==0 else 1))<.000001
 f=d['frames'][round((cast['time']+.125)*24)]
 assert len(f['objects'])==(0 if cast['slot']==0 else 6)
 f=d['frames'][round((cast['time']+.5)*24)]
 assert not f['rings'] and not f['objects']
assert len(d['meshes']['Filled curved crescent volume']['triangles'])==876
assert len(d['meshes']['Faceted ice spear']['triangles'])==36
print(json.dumps({'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size,'frames':216,'checkedVisibleInstances':checks,'status':'PASS'},indent=2))
