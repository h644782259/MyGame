"""blender -b --python render_comparison.py -- enabled-factory-geometry.json
Actual effect MPB RGBA/opacity; Blender surface approximation, not Unity shader.
"""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent
data=json.loads(Path(sys.argv[sys.argv.index('--')+1]).read_text());actor=data if isinstance(data,dict) else next(x for x in data if x.get('hero')==0 and x.get('tier')==-1)
(root/'Vanguard-Enabled-Factory.json').write_text(json.dumps(actor))
samples=json.loads((root/'Comparison-Samples.json').read_text())
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=False;sc.render.resolution_x=600;sc.render.resolution_y=580;sc.render.resolution_percentage=100
for sample in samples:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for part in actor['parts']+sample['objects']:
  verts=[(v[0],-v[2],v[1]) for v in part['vertices']];ids=part['triangles'];m=bpy.data.meshes.new('actual production mesh');m.from_pydata(verts,[],[ids[i:i+3] for i in range(0,len(ids),3)]);m.update();o=bpy.data.objects.new(part.get('name',part.get('label')),m);sc.collection.objects.link(o)
  mat=bpy.data.materials.new('Actual effect MPB' if 'color' in part else 'Neutral reference actor');mat.use_nodes=True;n=mat.node_tree;bs=n.nodes.get('Principled BSDF');c=part.get('color',[.12,.16,.21,1]);bs.inputs['Base Color'].default_value=(*c[:3],1);bs.inputs['Roughness'].default_value=.6
  if 'color' in part:
   bs.inputs['Emission Color'].default_value=(*c[:3],1);bs.inputs['Emission Strength'].default_value=1
   tr=n.nodes.new('ShaderNodeBsdfTransparent');mix=n.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=part['opacity']*c[3];n.links.new(tr.outputs[0],mix.inputs[1]);n.links.new(bs.outputs[0],mix.inputs[2]);n.links.new(mix.outputs[0],n.nodes.get('Material Output').inputs['Surface'])
  o.data.materials.append(mat)
 bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;o.location.z=-.04;mat=bpy.data.materials.new('Dark slate');mat.diffuse_color=(.018,.025,.038,1);o.data.materials.append(mat)
 for loc,energy in [((3,-4,7),1000),((-4,1,5),650)]:
  bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=energy;o.data.size=5;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
 bpy.ops.object.camera_add(location=(5,-9,6));o=bpy.context.object;o.rotation_euler=(Vector((0,0,.85))-o.location).to_track_quat('-Z','Y').to_euler();o.data.type='ORTHO';o.data.ortho_scale=8;sc.camera=o;sc.world.color=(.02,.02,.02)
 sc.render.image_settings.file_format='PNG';sc.render.filepath=str(root/(sample['kind']+'-'+('guard' if ' guard ' in sample['phase'] else 'passive')+'.png'));bpy.ops.render.render(write_still=True)
