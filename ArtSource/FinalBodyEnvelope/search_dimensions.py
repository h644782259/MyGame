"""Offline parameter screening only; final acceptance re-executes production Filled code."""
import json,sys
from pathlib import Path
from intersections import crossings
actors=json.load(open(sys.argv[1]));effect=json.load(open(sys.argv[2]))[0]['objects'][0]
# Group actual body parts once. Triangle math still runs on world vertices.
sets=[]
for c in actors:
 vertices=[];triangles=[]
 for p in c['parts']:
  if p['category'].startswith('external-'):continue
  offset=len(vertices);vertices.extend(p['vertices']);triangles.extend(t+offset for t in p['triangles'])
 sets.append((c,vertices,triangles))
for width,height in [(2.6,2.45),(2.6,2.5),(2.6,2.55)]:
 ev=[[x*width/2.6,.08+(y-.08)*height/2.2,z*width/2.6] for x,y,z in effect['vertices']];fail=[]
 for c,v,t in sets:
  hits=crossings(v,t,ev,effect['triangles'])
  if hits:fail.append((c['name'],c['tier'],c['phase'],c['skill'],len(hits)))
 print(width,height,'crossing cases',len(fail),fail[:6],flush=True)
