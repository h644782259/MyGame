"""Compose rendered views without modifying sampled shapes/colors."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).resolve().parent
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',20);small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
canvas=Image.new('RGB',(1200,1280),(10,15,22));d=ImageDraw.Draw(canvas)
d.text((22,10),'Actual Vanguard guard / passive | age 0.75 s | Blender reconstruction, NOT Unity',font=font,fill='white')
for row,kind in enumerate(['guard','passive']):
 for col,version in enumerate(['Before','After']):
  x=col*600;y=42+row*616;canvas.paste(Image.open(root/(version+'-'+kind+'.png')).convert('RGB'),(x,y));d.text((x+20,y+560),version+' : '+('generic Rune' if version=='Before' else 'ProtectionCage')+' / '+kind,font=font,fill='white')
d.text((20,1247),'Same camera, scale and enabled factory actor. Actual MPB tint/alpha + opacity; Blender surface approximation.',font=small,fill=(195,205,215))
canvas.save(root/'Defense-Before-After-MPB.png')
