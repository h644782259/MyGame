"""Readable 4-case weapon-only before/after camera; actual factory output."""
from pathlib import Path
source=Path(__file__).with_name('render.py').read_text();exec(source.split('for close in [False,True]:')[0])
cam.location=(11,-7,6);target=Vector((3,0,1.2));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=9
s.render.resolution_x=2000;s.render.resolution_y=1050;s.cycles.samples=16
selected=[(0,1,0),(0,4,1),(2,4,1),(1,4,1)]
for version in [0,1]:
 objects=[]
 for i,(hero,tier,fashion) in enumerate(selected):
  c=next(c for c in cases if (c['hero'],c['tier'],c['fashion'],c['version'])==(hero,tier,fashion,version));x=i*2
  for part in c['weapon']:
   vertices=[(vx+x,-vz,vy+1) for vx,vy,vz in part['vertices']];t=part['triangles'];m=bpy.data.meshes.new(part['name']);m.from_pydata(vertices,[],[t[k:k+3] for k in range(0,len(t),3)]);m.update();o=bpy.data.objects.new(part['name'],m);bpy.context.collection.objects.link(o);m.materials.append(material(part['color']));objects.append(o)
  for line in c.get('lines',[]):
   curve=bpy.data.curves.new('Actual LineRenderer path','CURVE');curve.dimensions='3D';curve.bevel_depth=line['width']/2;curve.bevel_resolution=0;spline=curve.splines.new('POLY');spline.points.add(len(line['points'])-1)
   for target,point in zip(spline.points,line['points']):vx,vy,vz=point;target.co=(vx+x,-vz,vy+1,1)
   o=bpy.data.objects.new('Actual bow string',curve);bpy.context.collection.objects.link(o);curve.materials.append(material(line['color']));objects.append(o)
  bpy.ops.object.text_add(location=(x-.5,-.65,.02));o=bpy.context.object;o.data.body=['Sword','Arcane','Bow','Contract'][hero]+' T'+str(tier);o.data.size=.19;o.data.materials.append(material((.85,.77,.56)));objects.append(o)
 s.render.filepath=str(out/('WeaponDetail-After.png' if version else 'WeaponDetail-Before.png'));bpy.ops.render.render(write_still=True)
 for o in objects:
  data=o.data;bpy.data.objects.remove(o,do_unlink=True)
  if isinstance(data,bpy.types.Mesh):bpy.data.meshes.remove(data)
print('PASS detail pair same camera: starter source sword, T4 sword/bow/staff and existing legendary ornaments')
