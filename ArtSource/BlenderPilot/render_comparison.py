"""Identical camera/light gray comparison; baseline is managed production geometry, not Unity pixels."""
import bpy,json,os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];P=Path('/workspace/scratch/blender-pilot-preview')
source=Path(os.environ.get('PILOT_BASELINE_GEOMETRY','/workspace/scratch/pilot-scenery-evidence/old/geometry.json'))
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend'))
sc=bpy.context.scene;sc.render.resolution_x=800;sc.render.resolution_y=800;sc.cycles.samples=24;sc.cycles.use_denoising=False
mat=bpy.data.materials.new('ComparisonNeutralClay');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.42,.45,.48,1);bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.8
hero=[o for o in bpy.data.objects if o.type=='MESH' and o.name.startswith('Vanguard_')]
for o in hero:o.data.materials.clear();o.data.materials.append(mat)
sc.frame_set(0);sc.render.filepath=str(P/'Pilot-Gray-New.png');bpy.ops.render.render(write_still=True)
for o in hero:o.hide_render=True
case=next(c for c in json.loads(source.read_text()) if c['hero']==0 and c['tier']==-1 and c['wing']==0)
for i,part in enumerate(case['parts']):
 v=[(x,-z,y) for x,y,z in part['vertices']];indices=part['triangles'];faces=[indices[j:j+3] for j in range(0,len(indices),3)]
 m=bpy.data.meshes.new('BaselineMesh'+str(i));m.from_pydata(v,[],faces);m.update();o=bpy.data.objects.new(part['name'],m);bpy.context.collection.objects.link(o);m.materials.append(mat)
sc.render.filepath=str(P/'Pilot-Gray-Old.png');bpy.ops.render.render(write_still=True)
print('COMPARISON rendered identical camera, lighting, neutral material; baseline managed source geometry; not Unity capture')
