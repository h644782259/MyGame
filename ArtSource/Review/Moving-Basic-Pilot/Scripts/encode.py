"""Encode native24fps frames; comparison pads captions outside800x600 panels."""
from pathlib import Path
import sys,subprocess
out=Path(sys.argv[1]).resolve();ffmpeg=sys.argv[2] if len(sys.argv)>2 else 'ffmpeg'
for folder,name in [('frames','Baseline-close-native'),('combat-frames','Baseline-combat-native'),('candidate-frames','Candidate-close-native'),('candidate-combat-frames','Candidate-combat-native')]:
 paths=sorted((out/folder).glob('*.png'));assert len(paths)==144,(folder,len(paths))
 subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate','24','-i',str(out/folder/'%04d.png'),'-c:v','libx264','-threads','1','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(out/(name+'.mp4'))],check=True)
subprocess.run([sys.executable,str(Path(__file__).with_name('compose.py')),str(out),ffmpeg],check=True)
