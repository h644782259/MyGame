"""Run after render_assemblies.py; geometry identity + nonempty same-camera image differences.
python3 verify_review.py before-enemies.json after-enemies.json animation.json
"""
import json,sys
from pathlib import Path
from PIL import Image,ImageChops,ImageStat
base=Path(__file__).resolve().parent
before=json.loads(Path(sys.argv[1]).read_text());after=json.loads(Path(sys.argv[2]).read_text());animation=json.loads(Path(sys.argv[3]).read_text())
before={c['name']:c for c in before if c.get('mask')==0};after={c['name']:c for c in after if c.get('mask')==0}
report={'boundary':'Blender same-camera pixel difference is a change detector, not Unity visual approval','models':{},'pixels':{}}
for name,a in before.items():
 b=after[name];ta=sum(len(p['triangles'])//3 for p in a['parts']);tb=sum(len(p['triangles'])//3 for p in b['parts']);report['models'][name]={'before_triangles':ta,'after_triangles':tb,'parts':len(b['parts'])}
 if name in ['Slime','Wisp']:assert a['parts']==b['parts'],name+' retained exact factory geometry and palette'
 else:assert tb<ta and a['parts']!=b['parts'],name+' upgraded at lower instance triangle cost'
for name in ['Slime','Wisp']:
 for phase in ['walk','windup','contact','recovery']:
  pair=[next(c for c in animation if c['name']==name+'-'+phase+'-'+v) for v in ['before','after']]
  assert pair[0]['parts']==pair[1]['parts'],name+' actual animated geometry/palette retained'
for phase in ['walk','windup','contact','recovery']:
 a=Image.open(base/'Assemblies'/('Action-'+phase+'-before.png')).convert('RGB');b=Image.open(base/'Assemblies'/('Action-'+phase+'-after.png')).convert('RGB');assert a.size==b.size
 diff=ImageChops.difference(a,b);value=sum(ImageStat.Stat(diff).mean)/3;assert value>.02,phase+' actual assembled change visible'
 report['pixels'][phase]={'size':list(a.size),'mean_absolute_channel_difference':value,'changed_bbox':list(diff.getbbox())}
(base/'review-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print('PASS exact Slime/Wisp factory + four animated phases retained; all three humanoid triangle budgets reduced; four same-camera rendered action pairs contain measurable changes')
