"""Caption native PNGs without retouching/resizing them; Pillow only."""
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
out=Path(sys.argv[1]);font='/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
def sheet(files,titles,name,footer):
 images=[Image.open(out/f).convert('RGB') for f in files];w=sum(i.width for i in images);h=max(i.height for i in images)
 canvas=Image.new('RGB',(w,h+150),(22,27,35));draw=ImageDraw.Draw(canvas);f=ImageFont.truetype(font,24,index=2);small=ImageFont.truetype(font,20,index=2)
 draw.text((20,12),'Blender 源资产审阅 · 非 Unity 实机 · 几何未修改',font=f,fill='white');x=0
 for im,title in zip(images,titles):
  draw.text((x+20,52),title,font=small,fill=(199,211,228));canvas.paste(im,(x,85));x+=im.width
 draw.text((20,h+100),footer,font=small,fill=(199,211,228));canvas.save(out/name)
sheet(['Vanguard-front.png','Vanguard-side.png','Vanguard-back.png'],['当前资产 · 正面','当前资产 · 侧面','当前资产 · 背面'],'Vanguard-static-review.png','原始贴图与几何；单核 Cycles 128 采样，无降噪。此图不代表美术验收通过。')
sheet(['Combat-distance-old.png','Combat-distance-new.png'],['旧版 03422ab · 实际源码网格重建','当前 49e624b · 原始 Blender 资产'],'Combat-distance-comparison.png','默认战斗镜头参数；同一灰材质、灯光、尺寸与视野；无敌人、场景遮挡或 Unity 后处理。')
