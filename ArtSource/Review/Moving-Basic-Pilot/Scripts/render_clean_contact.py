"""Independent64-sample static supplement; never overwrites native movie frames."""
from pathlib import Path
import sys,subprocess,shutil,json,hashlib
from PIL import Image,ImageDraw,ImageFont
base,main,out=map(lambda v:Path(v).resolve(),sys.argv[1:4]);blender=sys.argv[4] if len(sys.argv)>4 else 'blender';scripts=Path(__file__).parent
assert main!=out,'Static supplement must use a different output directory'
out.mkdir(parents=True,exist_ok=True);(out/'policy-export').mkdir(exist_ok=True)
for name in ['motion.json','candidate-samples.json']:
 target=out/name
 if not target.exists():target.symlink_to(main/name)
shutil.copy2(main/'policy-export/BlenderPilotPosePolicy.cs',out/'policy-export/BlenderPilotPosePolicy.cs')
for script,log in [('render.py','baseline.log'),('render_candidate.py','candidate.log')]:
 with (out/log).open('w') as stream:subprocess.run([blender,'-b','--python-exit-code','1','--threads','1','--python',str(scripts/script),'--',str(base),str(out),'--combat','--only','24','--samples','64'],stdout=stream,stderr=subprocess.STDOUT,check=True)
files=[out/'combat-frames/0024.png',out/'candidate-combat-frames/0024.png'];images=[Image.open(f).convert('RGB') for f in files];assert all(im.size==(800,600) for im in images)
canvas=Image.new('RGB',(1600,664),(23,33,43));canvas.paste(images[0],(0,64));canvas.paste(images[1],(800,64));draw=ImageDraw.Draw(canvas);font=ImageFont.truetype('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',22,index=2)
draw.text((18,10),'修改前｜接触帧 24 静态补充',fill='white',font=font);draw.text((818,10),'候选｜接触帧 24 静态补充',fill='white',font=font);draw.text((18,36),'仅此静帧使用 64 样本；不改变原视频，不代表 Unity 实机效果',fill='white',font=ImageFont.truetype('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc',18,index=2));canvas.save(out/'Comparison-contact64.png')
report={'nativeFrame24Only':True,'sameCameraAsMovies':True,'samples':64,'noMovieFramesOverwritten':True,'nativeSha256':{str(f.relative_to(out)):hashlib.sha256(f.read_bytes()).hexdigest() for f in files},'comparisonSha256':hashlib.sha256((out/'Comparison-contact64.png').read_bytes()).hexdigest()};(out/'clean-contact-manifest.json').write_text(json.dumps(report,indent=2))
