"""Assemble native-render comparisons; no synthesized pixels or geometry."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import sys
p=Path(sys.argv[1]);im=Image.new('RGB',(1080,990),(238,240,243));d=ImageDraw.Draw(im)
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18)
d.text((20,15),'SCENERY / source geometry reconstructed in Blender, not Unity',font=font,fill='#14202c')
for i,(f,label) in enumerate([('baseline-tree','Before: rock crown'),('tree-variant-0','After: branching tree / A'),('tree-variant-1','After: branching tree / B'),('tree-variant-2','After: branching tree / C'),('baseline-roof','Before: flat roof slab'),('workshop-roof','After: pitch / eave / chimney')]):
 x=i%3*360;y=70+i//3*455;im.paste(Image.open(p/(f+'.png')).convert('RGB'),(x,y));d.text((x+6,y+425),label,font=font,fill='#14202c')
im.save(p.parent/'scenery-comparison.png')
