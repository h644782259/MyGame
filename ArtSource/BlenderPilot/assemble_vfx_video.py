"""Composite native Blender alpha passes over the identical stage and label provenance."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import os, subprocess, json, hashlib
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(os.environ.get('VFX_OUTPUT','/workspace/scratch/emberfall-vfx-comparison'))
MODE=os.environ.get('VFX_MODE','before');src=OUT/MODE;dst=OUT/(MODE+'-final');dst.mkdir(exist_ok=True)
fontpath=Path(os.environ.get('VFX_FONT','/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'))
font=ImageFont.truetype(str(fontpath),27,index=2);small=ImageFont.truetype(str(fontpath),20,index=2)
stage=Image.open(OUT/'matched-stage.png').convert('RGBA')
frames=sorted(src.glob('*.png'));assert len(frames)==216,len(frames)
for i,p in enumerate(frames):
 im=Image.alpha_composite(stage,Image.open(p).convert('RGBA')).convert('RGB');d=ImageDraw.Draw(im)
 d.rectangle((0,0,1280,78),fill=(15,22,29));d.rectangle((0,650,1280,720),fill=(15,22,29))
 title='现有代码重建（非Unity实录）' if MODE=='before' else 'Blender优化预演（尚未接入游戏）'
 d.text((28,12),title,font=font,fill=(245,239,217))
 d.text((28,49),'初习旋风斩 · 两次完整周期' if i<108 else '初习裂地冲击 · 两次完整周期',font=small,fill=(241,192,101))
 d.text((28,660),'同镜头 · 同比例 · 720p / 24fps · 正常时间尺度 · 角色仅作尺度参照',font=small,fill=(224,230,236))
 note='基线 03422ab；无敌人命中反馈；着色与透明排序不等同Unity；演示间隔不代表技能冷却' if MODE=='before' else '视觉提案：立体旋刃 / 地面传播 / 接触碎屑；不代表伤害范围、时机或玩法已变更'
 d.text((28,689),note,font=small,fill=(176,188,197));im.save(dst/p.name,compress_level=1)
video=OUT/('Emberfall-VFX-'+('Before' if MODE=='before' else 'After')+'.mp4')
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','24','-i',str(dst/'%04d.png'),'-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(video)],check=True)
print(json.dumps({'path':str(video),'bytes':video.stat().st_size,'sha256':hashlib.sha256(video.read_bytes()).hexdigest(),'frames':216,'fps':24,'duration':9},ensure_ascii=False))
