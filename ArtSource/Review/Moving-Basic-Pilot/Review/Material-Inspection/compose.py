from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import hashlib,json
out=Path(__file__).parent
font='/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc';large=ImageFont.truetype(font,25,index=2);small=ImageFont.truetype(font,19,index=2)
for mode,title in [('neutral','中性几何'),('material','原有 UV / 贴图 / Blender 源材质')]:
 canvas=Image.new('RGB',(1920,730),(26,31,38));d=ImageDraw.Draw(canvas)
 for n,label in enumerate(['Full 全身','Weapon 独立武器','Back 独立背布']):
  p=out/(label.split()[0]+'-'+mode+'.png');im=Image.open(p).convert('RGB');assert im.size==(640,640);canvas.paste(im,(640*n,90));d.text((640*n+18,10),label,font=large,fill='white')
 d.text((18,49),title+'｜Blender 源资产检查；非 Unity 实机或导入后的收藏预览取景；配对图同镜头、同灯光',font=small,fill=(199,208,220))
 canvas.save(out/('Plate-'+mode+'.png'))
files=[p for p in out.iterdir() if p.is_file() and p.name!='hashes.json']
(out/'hashes.json').write_text(json.dumps({p.name:{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(files)},indent=2)+'\n')
