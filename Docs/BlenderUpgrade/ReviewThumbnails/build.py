from pathlib import Path
from PIL import Image
import hashlib,json
root=Path(__file__).resolve().parents[3];out=Path(__file__).parent
paths=['ArtSource/BlenderScenery/Scenery-New-Preview.png','ArtSource/BlenderSpellBases/Spell-Volumes-Preview.png','ArtSource/BlenderSpellBases/Primary-Beats-Preview.png','ArtSource/ActorModules/ActorModules-Blender.png']
report={'source_commit':'cda8a9045b3ea6e11b352cff40af8a39b5939e4c','scope':'Deterministic downsample/JPEG encoding of existing Blender renders; no generative retouch, no Unity claim','images':[]}
for name in paths:
 p=root/name;im=Image.open(p).convert('RGB');original=im.size;im.thumbnail((1600,1200),Image.Resampling.LANCZOS);target=out/(p.stem+'.jpg');im.save(target,quality=88,optimize=True)
 assert target.stat().st_size<1000000
 report['images'].append({'source':name,'source_sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'original_size':original,'review_path':str(target.relative_to(root)),'review_size':im.size,'bytes':target.stat().st_size,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
