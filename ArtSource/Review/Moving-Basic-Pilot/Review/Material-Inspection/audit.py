import bpy,json,hashlib
from pathlib import Path
r=Path('/workspace/emberfall_moving_pilot');b=r/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend';bpy.ops.wm.open_mainfile(filepath=str(b))
x={'blendSha256':hashlib.sha256(b.read_bytes()).hexdigest(),'materials':{},'images':{},'meshes':{}}
for m in bpy.data.materials:
 x['materials'][m.name]={'nodes':[{'name':n.name,'type':n.type,'image':n.image.name if n.type=='TEX_IMAGE' and n.image else None} for n in m.node_tree.nodes] if m.use_nodes else [],'links':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links] if m.use_nodes else []}
for i in bpy.data.images:
 x['images'][i.name]={'path':i.filepath,'packed':bool(i.packed_file),'colorspace':i.colorspace_settings.name,'size':list(i.size),'alphaMode':i.alpha_mode}
for o in bpy.data.objects:
 if o.type=='MESH' and o.name.startswith('Vanguard_'):
  x['meshes'][o.name]={'materials':[m.name for m in o.data.materials],'uv':len(o.data.uv_layers),'swatches':sorted(set(int(min(.99999,max(0,v.uv.y))*4)*4+int(min(.99999,max(0,v.uv.x))*4) for v in o.data.uv_layers.active.data))}
Path('/workspace/scratch/pilot-material-review/audit.json').write_text(json.dumps(x,indent=2))
