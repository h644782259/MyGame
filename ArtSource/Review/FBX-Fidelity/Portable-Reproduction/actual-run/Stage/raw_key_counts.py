import sys,json
from pathlib import Path
from io_scene_fbx import parse_fbx
out=Path(__file__).parent;paths=[Path('/workspace/scratch/fbx-portable-repro/actual-run/Stage/Baseline/Vanguard.fbx')]+sorted(p for p in out.glob('*.fbx') if p.name!='reject.fbx')
result={}
for path in paths:
 root,version=parse_fbx.parse(str(path));counts=[]
 def visit(e):
  if e.id==b'AnimationCurve':
   for child in e.elems:
    if child.id==b'KeyTime':counts.append(len(child.props[0]))
  for child in e.elems:visit(child)
 visit(root);result[path.name]={'fbxVersion':version,'rawAnimationCurveCount':len(counts),'rawKeyTimeCount':sum(counts),'curvesMinMaxKeys':[min(counts),max(counts)]}
(out/'raw-key-counts.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
