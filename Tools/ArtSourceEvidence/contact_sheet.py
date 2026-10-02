from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import sys
folder=Path(sys.argv[1]);font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',19);small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
out=Image.new('RGB',(1440,1850),(238,240,243));d=ImageDraw.Draw(out);d.text((25,15),sys.argv[2],font=font,fill='#14202c');d.text((25,45),'Blender render of production meshes + managed TRS; not Unity / no animation collision validation',font=small,fill='#45566b')
for h,name in enumerate(['Vanguard','Arcanist','Ranger','Summoner']):
 for col,(tier,wing) in enumerate([(0,0),(1,0),(0,1),(1,1)]):
  x=col*360;y=85+h*435;out.paste(Image.open(folder/f'h{h}-t{tier}-w{wing}.png').convert('RGB'),(x,y));d.text((x+8,y+5),f'{name} | {"T1" if tier==0 else "T4"} | {"wings" if wing else "no wings"}',font=small,fill='#132030')
out.save(folder.parent/'silhouettes.png')
