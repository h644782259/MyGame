"""Render actual managed production samples with Blender materials, NOT Unity shader footage."""
import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parent;samples=json.loads((root/'Runtime-Samples.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for index,sample in enumerate(samples):
 x=(index%2)*7-3.5;y=-(index//2)*7
 for index2,part in enumerate(sample['objects']):
  verts=[(v[0]+x,-v[2]+y,v[1]) for v in part['vertices']];ids=part['triangles'];faces=[ids[i:i+3] for i in range(0,len(ids),3)]
  m=bpy.data.meshes.new(part['label']);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(part['label'],m);bpy.context.collection.objects.link(o)
  mat=bpy.data.materials.new('Production opacity %.3f'%part['opacity']);mat.use_nodes=True;nt=mat.node_tree;bs=nt.nodes.get('Principled BSDF');tint=(.6,.75,.8);bs.inputs['Base Color'].default_value=(*tint,1);bs.inputs['Roughness'].default_value=.5;bs.inputs['Emission Color'].default_value=(*tint,1);bs.inputs['Emission Strength'].default_value=.25
  # Material transparency approximates only the sampled opacity, not FilledSpell UV grain.
  out=nt.nodes.get('Material Output');tr=nt.nodes.new('ShaderNodeBsdfTransparent');mix=nt.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=part['opacity'];nt.links.new(tr.outputs[0],mix.inputs[1]);nt.links.new(bs.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],out.inputs['Surface']);o.data.materials.append(mat)
 bpy.ops.object.text_add(location=(x-2.7,y-2.7,.05));o=bpy.context.object;o.data.body=sample['kind']+' / '+sample['phase'];o.data.size=.38
bpy.ops.mesh.primitive_plane_add(size=200);o=bpy.context.object;o.location.z=-.15;mat=bpy.data.materials.new('Slate');mat.diffuse_color=(.07,.09,.12,1);o.data.materials.append(mat)
bpy.ops.object.light_add(type='AREA',location=(0,-3,22));o=bpy.context.object;o.data.energy=8000;o.data.size=22
bpy.ops.object.camera_add(location=(0,-22,25));o=bpy.context.object;o.rotation_euler=(Vector((0,-3.5,0))-o.location).to_track_quat('-Z','Y').to_euler();o.data.type='ORTHO';o.data.ortho_scale=17
sc=bpy.context.scene;sc.camera=o;sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=False;sc.render.resolution_x=1100;sc.render.resolution_y=950;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(root/'Defense-Actual-Construction.png');bpy.ops.render.render(write_still=True)
