"""Render actual managed production-factory snapshot geometry; not Unity capture."""
import bpy,json,math,os,gzip
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
samples=json.loads((root/'factory-snapshots.json').read_text() if (root/'factory-snapshots.json').exists() else gzip.decompress((root/'factory-snapshots.json.gz').read_bytes()))
for sample in samples:
 if os.environ.get("F4_SAMPLE") and str(sample["id"])!=os.environ["F4_SAMPLE"]:continue
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 parts=sample['parts'];i=sample['id']
 # Entire small groups. Town snapshots focus on one actual building and central dais.
 if i==2:
  roof=next(p for p in parts if p['name']=='Quarry workshop');center=Vector(tuple(sum(v[k] for v in roof['vertices'])/len(roof['vertices']) for k in range(3)))
  parts=[p for p in parts if p['name'] in ['Quarry workshop','Pitched workshop roof module','Workshop projecting eave','Workshop ridge cap','Kiln chimney','Kiln chimney cap','Lit doorway','Timber storage beam','Weathered wall footing'] and (Vector(tuple(sum(v[k] for v in p['vertices'])/len(p['vertices']) for k in range(3)))-center).length<6]
 if i==3:parts=[p for p in parts if p['name'] in ['Observatory circular dais','Crystal upper','Crystal lower','Observatory brass meridian'] and abs(sum(v[0] for v in p['vertices'])/len(p['vertices']))<5 and abs(sum(v[2] for v in p['vertices'])/len(p['vertices'])-3)<5]
 mats={};points=[]
 for p in parts:
  verts=[(v[0],-v[2],v[1]) for v in p['vertices']];points.extend(Vector(v) for v in verts);tris=p['triangles'];
  if 'lineWidth' in p:
   curve=bpy.data.curves.new(p['name'],'CURVE');curve.dimensions='3D';curve.bevel_depth=max(.01,p['lineWidth']*.5);spline=curve.splines.new('POLY');spline.points.add(len(verts)-1)
   for pt,co in zip(spline.points,verts):pt.co=(*co,1)
   spline.use_cyclic_u=p['loop'];ob=bpy.data.objects.new(p['name'],curve);bpy.context.collection.objects.link(ob)
  else:
   mesh=bpy.data.meshes.new(p['name']);mesh.from_pydata(verts,[],[tris[t:t+3] for t in range(0,len(tris),3)]);mesh.update();ob=bpy.data.objects.new(p['name'],mesh);bpy.context.collection.objects.link(ob)
  key=tuple(p['color'])
  if key not in mats:
   m=bpy.data.materials.new(str(key));m.diffuse_color=(*key,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*key,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;mats[key]=m
  ob.data.materials.append(mats[key])
 lo=Vector(tuple(min(v[k] for v in points) for k in range(3)));hi=Vector(tuple(max(v[k] for v in points) for k in range(3)));center=(lo+hi)/2;extent=max((hi-lo).x,(hi-lo).y,(hi-lo).z)
 bpy.ops.mesh.primitive_plane_add(size=200,location=(center.x,center.y,lo.z-.02));floor=bpy.context.object;m=bpy.data.materials.new('neutral floor');m.diffuse_color=(.055,.07,.085,1);floor.data.materials.append(m)
 bpy.ops.object.camera_add(location=center+Vector((1.15,-1.7,1.05))*extent);cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=extent*1.75;bpy.context.scene.camera=cam
 for loc,power,size in [((4,-6,10),1700,8),((-5,1,6),1000,7)]:
  bpy.ops.object.light_add(type='AREA',location=center+Vector(loc));light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
 sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.world.color=(.35,.35,.35);sc.render.resolution_x=900;sc.render.resolution_y=720;sc.render.resolution_percentage=100;sc.view_settings.view_transform='Standard';sc.render.filepath=str(root/f'factory-{i}.png');bpy.ops.render.render(write_still=True)
