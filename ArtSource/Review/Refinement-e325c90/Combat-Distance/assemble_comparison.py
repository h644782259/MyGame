"""Caption native stills outside images; no resizing, crop or pixel retouch."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
out=Path(__file__).resolve().parent
before=Image.open(out/'Before.png').convert('RGB');after=Image.open(out/'After.png').convert('RGB');assert before.size==after.size==(800,600)
canvas=Image.new('RGB',(1600,750),(22,27,35));d=ImageDraw.Draw(canvas);f=ImageFont.truetype('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',22,index=2)
d.text((18,12),'默认战斗距离 · Blender 源资产对照 · 非 Unity 实机',font=f,fill='white')
d.text((18,51),'修整前 · c5a734e',font=f,fill=(200,210,225));d.text((818,51),'修整后 · 4d645c3',font=f,fill=(200,210,225))
canvas.paste(before,(0,85));canvas.paste(after,(800,85))
d.text((18,701),'原生 800×600，不裁切、不放大；同一镜头、灯光、原材质与 Idle 第0帧。',font=f,fill=(200,210,225))
p=out/'Comparison.png';canvas.save(p)
m=json.loads((out/'manifest.json').read_text());m['comparisonSha256']=hashlib.sha256(p.read_bytes()).hexdigest();m['captionScriptSha256']=hashlib.sha256(Path(__file__).read_bytes()).hexdigest();(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n')
