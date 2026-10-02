"""Verify imported static geometry, UV, weights, bind bones, sockets, and material nodes unchanged."""
import bpy,json,hashlib
from pathlib import Path
out=Path(__file__).parent;baseline=Path('/workspace/emberfall_android_player_journey/Assets/Resources/BlenderPilot/Vanguard.fbx')
def digest(data):return hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def snapshot(path):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path));result={}
 for o in bpy.data.objects:
  if o.type=='MESH':
   m=o.data;result[o.name]={'vertices':[list(v.co) for v in m.vertices],'polygons':[list(p.vertices) for p in m.polygons],'uv':[[list(v.uv) for v in layer.data] for layer in m.uv_layers],'weights':[{o.vertex_groups[g.group].name:g.weight for g in v.groups} for v in m.vertices],'matrixLocal':[list(r) for r in o.matrix_local],'materials':[m.name for m in o.data.materials]}
  elif o.type=='ARMATURE':result[o.name]={'bones':{b.name:{'parent':b.parent.name if b.parent else None,'matrix':[list(r) for r in b.matrix_local],'length':b.length} for b in o.data.bones}}
  elif o.name.startswith('Anchor_'):result[o.name]={'parent':o.parent.name if o.parent else None,'bone':o.parent_bone,'parentType':o.parent_type,'matrixLocal':[list(r) for r in o.matrix_local]}
 result['materials']={m.name:{'nodes':[(n.name,n.type,n.image.name if n.type=='TEX_IMAGE' and n.image else None) for n in m.node_tree.nodes],'links':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]} for m in bpy.data.materials if m.use_nodes}
 return {k:digest(v) for k,v in result.items()}
base=snapshot(baseline);report={'baseline':base,'candidates':{},'scope':'Blender reimport static bind/geometry/UV/weights/material wiring/socket parent-local matrices; not Unity'}
for p in sorted(out.glob('step*.fbx')):
 current=snapshot(p);diff=[k for k in base.keys()|current.keys() if base.get(k)!=current.get(k)];report['candidates'][p.name]={'staticEqual':not diff,'differentEntries':diff,'sha256ByEntry':current}
(out/'static-invariants.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report['candidates']))
