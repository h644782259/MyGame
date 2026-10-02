from pathlib import Path
import json,hashlib,math,sys
base=Path(sys.argv[1]);full=Path(sys.argv[2]);a=json.loads(base.read_text());b=json.loads(full.read_text());changed=[]
for i,(x,y) in enumerate(zip(a['frames'],b['frames'])):
 assert [o for o in y['objects'] if not o['id'].endswith('/swordRibbon')]==x['objects']
 assert x['rings']==y['rings']
 for r in [o for o in y['objects'] if o['id'].endswith('/swordRibbon')]:
  changed.append(i);assert len(r['vertices'])==8
  assert all(math.isfinite(v) for p in r['vertices'] for v in p) and 0<r['color'][3]<=1
  for j in range(4):assert abs(math.dist(r['vertices'][j],r['vertices'][j+4])-.024)<.000001
assert changed==list(range(120,126))+list(range(168,174))
for start in [120,168]:
 ribbon=lambda index:next(o for o in b['frames'][index]['objects'] if o['id'].endswith('/swordRibbon'))
 assert ribbon(start)['color'][3]==1
 assert all(ribbon(i+1)['color'][3]<ribbon(i)['color'][3] for i in range(start,start+5))
 assert len({tuple(ribbon(i)['sockets']['tip']) for i in range(start,start+6)})==6
 for i in range(start+1,start+6):
  r=ribbon(i);prior=ribbon(i-1)
  def center(v,a,b):return [(x+y)*.5 for x,y in zip(v[a],v[b])]
  assert math.dist(center(r['vertices'],2,6),prior['sockets']['tip'])<.000001
  assert math.dist(center(r['vertices'],3,7),prior['sockets']['root'])<.000001
assert len(b['meshes']['Weapon root-tip swept volume']['triangles'])==36
print(json.dumps({'sha256':hashlib.sha256(full.read_bytes()).hexdigest(),'changedFrames':changed,'unchangedOriginalEffects':True,'actualPriorEndpointsMatch':True,'status':'PASS'},indent=2))
