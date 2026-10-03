"""Factory hierarchy + actual resource deltas on rest pose. NOT full runtime motion/Unity."""
import bpy,json,sys,struct,math
from mathutils import Quaternion,Vector
from pathlib import Path
root=Path(__file__).resolve().parents[2]; source=Path(sys.argv[sys.argv.index('--')+1]);loaded=json.loads(source.read_text());full=isinstance(loaded[0],list);parts=loaded[0] if full else loaded;budget=json.loads((root/'ArtSource/VanguardActions/budget.json').read_text());data=(root/'Assets/Resources/VanguardActions/Swordguard.bytes').read_bytes();v=struct.unpack('<%df'%((len(data)-16)//4),data[16:])
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);return m
metal=mat('neutral teal armor',(.12,.38,.43));cloth=mat('warm cloth',(.42,.20,.11));floor=mat('floor',(.07,.085,.11))
def qdeg(e):
 x,y,z=[math.radians(x) for x in e];return Quaternion((0,1,0),y)@Quaternion((1,0,0),x)@Quaternion((0,0,1),z)
for clip,title in enumerate(budget['clips']):
 parts=loaded[clip] if full else loaded
 cx=(clip%5)*2.7;cz=-(clip//5)*3.1;objects=[]
 if clip==12:cx+=.8;cz-=.6
 for p in parts:
  mesh=None
  if p['vertices'] and p['triangles']:
   mesh=bpy.data.meshes.new(p['name']);t=p['triangles'];mesh.from_pydata(p['vertices'],[],[t[i:i+3] for i in range(0,len(t),3)]);mesh.materials.append(cloth if 'cloth' in p['name'].lower() or 'robe' in p['name'].lower() else metal)
  o=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(o);objects.append(o);o.parent=objects[p['parent']] if p['parent']>=0 else None;o.location=p['p'];o.rotation_mode='QUATERNION';q=p['q'];o.rotation_quaternion=Quaternion((q[3],q[0],q[1],q[2]));o.scale=p['s'];o.hide_render=not p['visible']
 # Blender still in Unity axes, convert whole group from Y-up to Z-up.
 group=bpy.data.objects.new(title,None);bpy.context.collection.objects.link(group);objects[0].parent=group;group.rotation_euler=(math.pi/2,0,0);group.location=(cx,cz,0)
 names=['Pelvis','Spine','Neck','Left Shoulder','Right Shoulder','Knee','Knee','Tailored Cloak','Sword Wrist'];knees=[o for o in objects if o.name.split('.')[0]=='Knee']
 frame=8 if clip in range(1,5) else 10 if clip in (7,8) else 0 if clip==11 else 32 if clip in (5,6,9,10,12) else 16
 for bone,name in enumerate(names):
  candidates=[o for o,p in zip(objects,parts) if p['name']==name]
  o=candidates[bone-5] if bone in (5,6) and len(candidates)>1 else candidates[0] if candidates else None
  if o and not full:
   at=((clip*33+frame)*9+bone)*3;o.rotation_quaternion=o.rotation_quaternion@qdeg(v[at:at+3])
 bpy.ops.object.text_add(location=(cx-1.0,cz-.25,.01));t=bpy.context.object;t.data.body=title;t.data.size=.23;t.data.materials.append(cloth)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(floor)
bpy.ops.object.light_add(type='AREA',location=(3,-3,12));bpy.context.object.data.energy=2100;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=10
bpy.ops.object.camera_add(location=(16,-22,20));cam=bpy.context.object;direction=Vector((5,-3,1))-cam.location;cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=18;bpy.context.scene.camera=cam
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.use_denoising=False;s.render.resolution_x=1800;s.render.resolution_y=1200;s.render.resolution_percentage=100;s.world.color=(.3,.3,.3);s.render.filepath=str(root/'ArtSource/VanguardActions'/('Factory-Runtime-Poses.png' if full else 'Factory-Additive-Poses.png'));bpy.ops.render.render(write_still=True)
print('PASS render real factory '+('actual AnimateHero final transforms; managed numerical TRS not Unity' if full else 'rest hierarchy and binary additive poses'))
