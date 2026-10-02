"""Caption-only Chinese top bar; original800x600 native video panels are not resized."""
from pathlib import Path
import sys,subprocess
from PIL import Image,ImageDraw,ImageFont
out=Path(sys.argv[1]).resolve();ffmpeg=sys.argv[2] if len(sys.argv)>2 else 'ffmpeg';font='/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
bar=Image.new('RGB',(1600,64),(23,33,43));draw=ImageDraw.Draw(bar);f=ImageFont.truetype(font,22,index=2);small=ImageFont.truetype(font,18,index=2)
draw.text((18,5),'修改前｜移动普攻切回程序模型',fill='white',font=f);draw.text((818,5),'候选｜同骨架行走＋上身普攻',fill='white',font=f);draw.text((18,35),'Blender 原生重建，非 Unity 实机；同镜头、原速，无补帧',fill='white',font=small);bar.save(out/'comparison-caption.png')
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-loop','1','-i',str(out/'comparison-caption.png'),'-i',str(out/'Baseline-combat-native.mp4'),'-i',str(out/'Candidate-combat-native.mp4'),'-filter_complex','[1:v][2:v]hstack=inputs=2,pad=1600:664:0:64:color=0x17212b[panels];[panels][0:v]overlay=0:0:shortest=1','-frames:v','144','-c:v','libx264','-threads','1','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'Comparison-combat.mp4')],check=True)
