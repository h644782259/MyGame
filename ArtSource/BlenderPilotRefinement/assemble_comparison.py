"""Paste native renders at their original pixel size; add captions outside images."""
from pathlib import Path
import sys
from PIL import Image,ImageDraw,ImageFont
out=Path(sys.argv[1]);font='/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc';f=ImageFont.truetype(font,22,index=2)
for angle,label in [('front','正面'),('side','侧面'),('back','背面'),('skill','既有 Skill 第25帧')]:
 images=[Image.open(out/group/('Vanguard-'+angle+'.png')).convert('RGB') for group in ['before','after']];w,h=images[0].size;assert images[1].size==(w,h)
 canvas=Image.new('RGB',(w*2,h+145),(22,27,35));d=ImageDraw.Draw(canvas)
 d.text((18,10),'Blender 源资产 · 非 Unity 实机 · '+label,font=f,fill='white')
 d.text((18,48),'修整前 · 已合并主线 c5a734e',font=f,fill=(200,210,224));d.text((w+18,48),'修整后 · 连续袖管与折叠背布',font=f,fill=(200,210,224))
 for x,im in enumerate(images):canvas.paste(im,(w*x,85))
 d.text((18,h+99),'同一镜头、灯光、材质与动作；未修改骨架或动作时序。',font=f,fill=(200,210,224));canvas.save(out/('Comparison-'+angle+'.png'))
