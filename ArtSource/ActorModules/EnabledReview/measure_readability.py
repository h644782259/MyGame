import json,sys,numpy as np
from pathlib import Path
cases=json.loads(Path(sys.argv[1]).read_text())
report={}
def triangles(p):return np.array(p['vertices'])[np.array(p['triangles']).reshape(-1,3)]
def inside(points,p):
 tris=triangles(p);a,b,c=tris[:,0],tris[:,1],tris[:,2];d=(b[:,2]-c[:,2])*(a[:,0]-c[:,0])+(c[:,0]-b[:,0])*(a[:,2]-c[:,2]);okay=abs(d)>1e-9;d=np.where(okay,d,1);result=[]
 for q in np.unique(np.round(points,7),axis=0):
  u=((b[:,2]-c[:,2])*(q[0]-c[:,0])+(c[:,0]-b[:,0])*(q[2]-c[:,2]))/d;w=((c[:,2]-a[:,2])*(q[0]-c[:,0])+(a[:,0]-c[:,0])*(q[2]-c[:,2]))/d
  valid=okay&(u>=-1e-6)&(w>=-1e-6)&((1-u-w)>=-1e-6);ys=(u*a[:,1]+w*b[:,1]+(1-u-w)*c[:,1])[valid]
  if len(ys) and min(ys)-1e-6<=q[1]<=max(ys)+1e-6:result.append(q.tolist())
 return result
c=next(c for c in cases if c.get('name')=='Companion-2');body=next(p for p in c['parts'] if p['name']=='Breastplate')
report['treant_shoulder_unique_vertices_inside_torso']=[len(inside(p['vertices'],body)) for p in c['parts'] if p['name']=='Pauldrons']
def occluded(points,tris):
 direction=np.array([3,3.25,7]);direction/=np.linalg.norm(direction)
 a,b,c=tris[:,0],tris[:,1],tris[:,2];e1=b-a;e2=c-a;h=np.cross(np.broadcast_to(direction,e2.shape),e2);det=np.einsum('ij,ij->i',e1,h);good=abs(det)>1e-9;inv=np.where(good,1/np.where(good,det,1),0);result=[]
 for q in points:
  s=q-a;u=inv*np.einsum('ij,ij->i',s,h);cross=np.cross(s,e1);v=inv*np.dot(cross,direction);t=inv*np.einsum('ij,ij->i',e2,cross)
  result.append(bool(np.any(good&(u>=0)&(v>=0)&(u+v<=1)&(t>1e-4))))
 return np.array(result)
for tier in [-1,0]:
 c=next(c for c in cases if c.get('hero')==2 and c['tier']==tier)
 bow=[p for p in c['parts'] if 'bow' in p['name'].lower()];other=[p for p in c['parts'] if 'bow' not in p['name'].lower()];plate=[p for p in c['parts'] if p['name']=='Asymmetric leather chest wrap']
 points=np.unique(np.round(np.concatenate([np.array(p['vertices']) for p in bow]),7),axis=0)
 allmask=occluded(points,np.concatenate([triangles(p) for p in other]));platemask=occluded(points,np.concatenate([triangles(p) for p in plate])) if plate else np.zeros(len(points),dtype=bool)
 withoutplate=[p for p in other if p['name']!='Asymmetric leather chest wrap'];othermask=occluded(points,np.concatenate([triangles(p) for p in withoutplate]));
 report['ranger_'+str(tier)]={'bow_parts':len(bow),'unique_bow_vertex_samples':len(points),'occluded_by_other_parts':int(sum(allmask)),'occluded_by_chest_wrap':int(sum(platemask)),'occluded_only_by_chest_wrap':int(sum(platemask&~othermask)),'projection':'orthographic front right view direction Unity(3,3.25,7), sampled mesh vertices; no bow self-occlusion or rasterization','chest_wrap_bounds': [[float(min(v[k] for v in plate[0]['vertices'])),float(max(v[k] for v in plate[0]['vertices']))] for k in range(3)] if plate else None}
print(json.dumps(report,indent=2))
Path(__file__).with_name('readability-measurements.json').write_text(json.dumps(report,indent=2)+'\n')
