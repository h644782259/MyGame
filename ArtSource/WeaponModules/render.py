"""Same-camera actual factory/TRS/color boards. Blender, not Unity screenshots."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2];args=sys.argv[sys.argv.index('--')+1:];cases=json.loads(Path(args[0]).read_text());out=root/'ArtSource/WeaponModules'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=False;s.world.color=(.2,.2,.2);s.render.resolution_percentage=100
materials={}
def material(rgb):
 key=tuple(round(x,4) for x in rgb)
 if key not in materials:
  m=bpy.data.materials.new(str(key));m.diffuse_color=(*rgb,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.55;materials[key]=m
 return materials[key]
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(material((.065,.085,.11)));bpy.context.object.location.z=-.07
for loc,energy,size in [((2,-3,14),2800,9),((-7,4,8),1700,8)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=energy;o.data.size=size
bpy.ops.object.camera_add(location=(11,-21,23));cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';target=Vector((4.5,-4.5,.8));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=18
for close in [False,True]:
 s.render.resolution_x=2200;s.render.resolution_y=1900
 for version in [0,1]:
  objects=[]
  for c in [c for c in cases if c['version']==version]:
   col=(0 if c['tier']==1 else 2)+c['fashion'];x=col*3;y=-c['hero']*3
   for part in c['weapon' if close else 'parts']:
    vertices=[(vx+x,-vz+y,vy+(1.1 if close else 0)) for vx,vy,vz in part['vertices']];tri=part['triangles'];m=bpy.data.meshes.new(part['name']);m.from_pydata(vertices,[],[tri[i:i+3] for i in range(0,len(tri),3)]);m.update();o=bpy.data.objects.new(part['name'],m);bpy.context.collection.objects.link(o);m.materials.append(material(part['color']));objects.append(o)
   bpy.ops.object.text_add(location=(x-.7,y-.8,.02));t=bpy.context.object;t.data.body=['Sword','Arcane','Bow','Contract'][c['hero']]+' T'+str(c['tier'])+(' +Legend' if c['fashion'] else '');t.data.size=.17;t.data.materials.append(material((.85,.77,.56)));objects.append(t)
  s.render.filepath=str(out/(('WeaponClose' if close else 'Factory16')+('-After.png' if version else '-Before.png')))
  bpy.ops.render.render(write_still=True)
  for o in objects:
   data=o.data;bpy.data.objects.remove(o,do_unlink=True)
   if isinstance(data,bpy.types.Mesh):bpy.data.meshes.remove(data)
print('PASS four same-camera boards: visible-renderer factory 16 combinations and weapon-local closeups; production palette, managed TRS, Blender render')
