"""Verify imported static geometry, UV, weights, bind bones, sockets, and material nodes unchanged."""
import bpy,json,hashlib
from pathlib import Path
out=Path(__file__).parent;baseline=Path('/workspace/scratch/fbx-portable-repro/actual-run/Stage/Baseline/Vanguard.fbx')
def digest(data):return hashlib.sha256(json.dumps(data,sort_keys=True,separators=(',',':')).encode()).hexdigest()
def snapshot(path):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path));result={}
 for o in bpy.data.objects:
  if o.type=='MESH':
   m=o.data;result[o.name]={'vertices':[list(v.co) for v in m.vertices],'polygons':[{'vertices':list(p.vertices),'materialIndex':p.material_index,'smooth':p.use_smooth,'normal':list(p.normal)} for p in m.polygons],'vertexNormals':[list(v.normal) for v in m.vertices],'cornerNormals':[list(v.vector) for v in m.corner_normals],'uv':[[list(v.uv) for v in layer.data] for layer in m.uv_layers],'weights':[{o.vertex_groups[g.group].name:g.weight for g in v.groups} for v in m.vertices],'matrixLocal':[list(r) for r in o.matrix_local],'materials':[m.name for m in o.data.materials]}
  elif o.type=='ARMATURE':result[o.name]={'bones':{b.name:{'parent':b.parent.name if b.parent else None,'matrix':[list(r) for r in b.matrix_local],'length':b.length} for b in o.data.bones}}
  elif o.name.startswith('Anchor_'):result[o.name]={'parent':o.parent.name if o.parent else None,'bone':o.parent_bone,'parentType':o.parent_type,'matrixLocal':[list(r) for r in o.matrix_local],'matrixParentInverse':[list(r) for r in o.matrix_parent_inverse],'rotationMode':o.rotation_mode,'location':list(o.location),'rotationEuler':list(o.rotation_euler),'rotationQuaternion':list(o.rotation_quaternion),'scale':list(o.scale),'displayType':o.empty_display_type,'displaySize':o.empty_display_size,'hideRender':o.hide_render,'hideViewport':o.hide_viewport}
 def value(v):
  if isinstance(v,(str,int,float,bool)) or v is None:return v
  try:return list(v)
  except TypeError:return str(v)
 def image_info(i):
  resolved=Path(bpy.path.abspath(i.filepath));raw=bytes(i.packed_file.data) if i.packed_file else resolved.read_bytes()
  return {'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'colorspace':i.colorspace_settings.name,'alphaMode':i.alpha_mode,'size':list(i.size),'source':i.source}
 result['materials']={}
 for m in bpy.data.materials:
  if not m.use_nodes:continue
  nodes=[]
  for n in m.node_tree.nodes:
   entry={'name':n.name,'type':n.type,'inputs':{v.identifier:value(v.default_value) for v in n.inputs if hasattr(v,'default_value')}}
   if n.type=='TEX_IMAGE' and n.image:entry.update({'image':image_info(n.image),'interpolation':n.interpolation,'extension':n.extension,'projection':n.projection})
   if n.type=='MATH':entry['operation']=n.operation
   nodes.append(entry)
  result['materials'][m.name]={'diffuseColor':list(m.diffuse_color),'roughness':m.roughness,'metallic':m.metallic,'nodes':nodes,'links':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]}

 return {k:digest(v) for k,v in result.items()}
base=snapshot(baseline);report={'baseline':base,'candidates':{},'scope':'Blender reimport exact static geometry/topology/UV/weights/vertex+corner+polygon normals/face material index; bind bones; socket transform defaults; full material input defaults and linked texture bytes/colorspace; not Unity'}
for p in sorted(list(out.glob('*mixed.fbx'))+[out/'helper-output.fbx']):
 current=snapshot(p);diff=[k for k in base.keys()|current.keys() if base.get(k)!=current.get(k)];report['candidates'][p.name]={'staticEqual':not diff,'differentEntries':diff,'sha256ByEntry':current}
(out/'hardened-static-invariants.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report['candidates']))
