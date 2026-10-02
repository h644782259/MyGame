from pathlib import Path
from PIL import Image
from fontTools.ttLib import TTFont
from fontTools.pens.recordingPen import RecordingPen
import hashlib,json,re
base=Path(__file__).parent;root=base/'extracted/Emberfall-Android';r={'png':{},'fonts':{},'materialTextureGUIDs':{}}
for p in sorted(root.rglob('*.png')):
 with Image.open(p) as im:im.verify()
 with Image.open(p) as im:im.load();r['png'][str(p.relative_to(root))]={'size':list(im.size),'mode':im.mode,'fullDecode':True,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
texts={'NotoSansSC-Regular.otf':'星烬纪元营地沉星遗迹星核观星学徒装备图鉴职业试炼晶核支线 · 2敌全灭：1材料+补给可放弃 · 不阻北门','EmberfallWorldLabels.otf':'营地沉星遗迹星核观星学徒装备图鉴职业试炼'}
for name,text in texts.items():
 p=root/'Assets/Resources/Fonts'/name
 with TTFont(p,lazy=False,checkChecksums=2,recalcBBoxes=False,recalcTimestamp=False) as font:
  tables=list(font.keys())
  for table in tables:font[table]
  cmap=font.getBestCmap();required=sorted(set(c for c in text if not c.isspace()));missing=[c for c in required if ord(c) not in cmap];assert not missing,(name,missing)
  glyphs=font.getGlyphSet();commands={}
  for c in required:
   pen=RecordingPen();glyphs[cmap[ord(c)]].draw(pen);commands[c]=len(pen.value)
  r['fonts'][name]={'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'tablesDecompiled':tables,'cmapEntries':len(cmap),'requiredCharacters':''.join(required),'missing':missing,'glyphOutlineDecoded':commands}
resource=root/'Assets/Resources/BlenderPilot';mat=(resource/'Pilot_Atlas_Standard.mat').read_text()
for prop,name in [('_MainTex','EmberfallPilot_Atlas.png'),('_MetallicGlossMap','EmberfallPilot_MetallicSmoothness.png')]:
 guid=re.search(r'^guid:\s*(\S+)',(resource/(name+'.meta')).read_text(),re.M)[1]
 assigned=re.search(re.escape(prop)+r':\s*\n\s*m_Texture:.*?guid:\s*(\w+)',mat)[1];assert assigned==guid
 r['materialTextureGUIDs'][prop]={'file':name,'guid':guid,'matchesPackagedPNGMeta':True}
r['fontScope']='Main UI font: actual basic title and listed labels; subset: its 18 supported camp-label characters only. WorldBuilder.Label floor=false calls WorldLabelPresentation.Initialize->GameFont.Apply->Shared main font, so expanded tactical text is checked against main font, not subset. No Unity glyph rasterization or all-runtime-string coverage claim.'
r['pass']=True;(base/'png-font-readability.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print('PASS',len(r['png']),'PNG decoded;2fonts tables/cmap/selected glyph outlines;2material texture GUIDs')
