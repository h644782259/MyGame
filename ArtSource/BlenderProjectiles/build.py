"""Original Emberfall projectile geometry. Blender 4.3; Unity XYZ bytes, no importer axes."""
import bpy,math,struct,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];SRC=Path(__file__).resolve().parent;OUT=ROOT/'Assets/Resources/BlenderProjectiles'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
records={}
class Shape:
 def __init__(self):self.v=[];self.f=[]
 def prism(self,ring,axis,lo,hi):
  n=len(self.v)
  for a in [lo,hi]:
   for x,y in ring:
    self.v.append((x,a,y) if axis=='y' else (x,y,a))
  count=len(ring);self.f.extend([tuple(n+i for i in reversed(range(count))),tuple(n+count+i for i in range(count))])
  for i in range(count):j=(i+1)%count;self.f.append((n+i,n+j,n+count+j,n+count+i))
 def diamond(self,x,y,z,w,h,d):
  n=len(self.v);self.v.extend([(x,y-h,z),(x-w,y,z),(x,y,z+d),(x+w,y,z),(x,y,z-d),(x,y+h,z)])
  for i in range(4):self.f.extend([(n,n+1+(i+1)%4,n+1+i),(n+5,n+1+i,n+1+(i+1)%4)])
 def bar(self,a,b,w):
  a=Vector(a);b=Vector(b);d=(b-a).normalized();u=d.cross(Vector((0,0,1))).normalized()*w;v=d.cross(u);n=len(self.v)
  for p in [a,b]:
   for q in [u,v,-u,-v]:self.v.append(tuple(p+q))
  self.f.extend([(n+3,n+2,n+1,n),(n+4,n+5,n+6,n+7)])
  for i in range(4):j=(i+1)%4;self.f.append((n+i,n+j,n+4+j,n+4+i))
 def export(self,name):
  m=bpy.data.meshes.new(name);m.from_pydata([(x,-z,y) for x,y,z in self.v],[],self.f);m.update()
  # Correct normals for each closed disconnected piece without smoothing its authored facets.
  o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o;o.select_set(True)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
  m.calc_loop_triangles();vertices=[];ids=[]
  for tri in m.loop_triangles:
   for vi in tri.vertices:
    p=m.vertices[vi].co;n=tri.normal;vertices.append((p.x,p.z,-p.y,n.x,n.z,-n.y,(p.x+.5),p.z+.5));ids.append(len(ids))
  b=bytearray(struct.pack('<Iii',0x45464D31,len(vertices),len(ids)))
  for v in vertices:b.extend(struct.pack('<8f',*v))
  b.extend(struct.pack('<%di'%len(ids),*ids));(OUT/(name+'.bytes')).write_bytes(b)
  bounds=[[min(v[i] for v in vertices) for i in range(3)],[max(v[i] for v in vertices) for i in range(3)]]
  records[name]={'vertices':len(vertices),'triangles':len(ids)//3,'bytes':len(b),'bounds':bounds,'sha256':hashlib.sha256(b).hexdigest(),'materials':1,'textures':0}
  assert len(ids)//3 <= (192 if name=='MeteorRock' else 128)
# +Y is forward under the existing ordinary arrow root's +90deg X rotation.
s=Shape();s.prism([(-.09,-.09),(.09,-.09),(.09,.09),(-.09,.09)],'y',-.95,.5)
s.prism([(0,1),(-.46,.35),(0,.48),(.46,.35)],'z',-.09,.09)
for sign in [-1,1]:s.prism([(sign*.07,-.45),(sign*.4,-.69),(sign*.4,-1),(sign*.07,-.85)],'z',-.045,.045)
s.export('ArrowBody')
# Caster split lance, directed along +Z; local y remains vertical.
s=Shape();s.diamond(0,0,.07,.22,.29,.41)
for sign in [-1,1]:s.diamond(sign*.3,0,-.15,.12,.19,.27)
s.export('CasterBolt')
# Contract diamond window with outward winglets, visibly hollow even without color.
s=Shape()
for a,b in [((0,.45,0),(.31,0,0)),((.31,0,0),(0,-.45,0)),((0,-.45,0),(-.31,0,0)),((-.31,0,0),(0,.45,0))]:s.bar(a,b,.042)
for sign in [-1,1]:s.diamond(sign*.39,0,0,.1,.16,.14)
s.v=[(x,-z,y) for x,y,z in s.v] # window lies in ground-facing plane for the combat camera
s.export('ContractBolt')
# Opponent body: broad central barb and two short hooks. Existing hostile red unchanged.
s=Shape();s.diamond(0,0,.1,.24,.23,.39)
for sign in [-1,1]:
 s.prism([(sign*.13,-.08),(sign*.48,-.22),(sign*.35,.17)],'z',-.28,.10)
s.export('HostileBolt')
# Faceted meteor with recessed channels: consistent paired low-radius longitude seams.
s=Shape();segments=12
for j in range(5):
 y=[-.47,-.31,0,.29,.49][j];base=[.14,.4,.49,.39,.12][j]
 for i in range(segments):
  a=i*math.tau/segments;groove=.77 if i in [0,4,8] else 1;r=base*groove*(1+.035*math.sin(i*3+j))
  s.v.append((math.cos(a)*r,y,math.sin(a)*r))
for j in range(4):
 for i in range(segments):k=(i+1)%segments;s.f.append((j*segments+i,j*segments+k,(j+1)*segments+k,(j+1)*segments+i))
s.f.extend([tuple(reversed(range(segments))),tuple(range(48,60))]);s.export('MeteorRock')
# Compact spring plate and four teeth; decorative core, not an area boundary.
s=Shape();s.prism([(math.cos(i*math.tau/8)*.72,math.sin(i*math.tau/8)*.72) for i in range(8)],'y',0,.12)
s.diamond(0,.16,0,.29,.12,.29)
for x,z in [(1,0),(-1,0),(0,1),(0,-1)]:s.diamond(x*.52,.28,z*.52,.12,.22,.12)
s.export('TrapCore')
# Stable metas survive rebuilding; no assets recreated by name/GUID changes.
for p in [OUT,ROOT/'Assets/Scripts/Combat/AuthoredProjectileMeshes.cs',*OUT.glob('*.bytes')]:
 meta=Path(str(p)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+hashlib.md5(('emberfall-projectiles-v1/'+p.name).encode()).hexdigest()+'\n'+('folderAsset: yes\n' if p.is_dir() else ''))
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'Projectiles.blend'))
(SRC/'budget.json').write_text(json.dumps(records,indent=2)+'\n');print('PROJECTILES_BUILD_OK',json.dumps(records))
