"""Matched-camera native Blender VFX review. Baseline is pinned production geometry/time,
not Unity footage. Optimized is a visual proposal, not a combat implementation.
"""
import bpy,math,json,os,sys,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(os.environ.get('VFX_OUTPUT','/workspace/scratch/emberfall-vfx-comparison'));OUT.mkdir(parents=True,exist_ok=True)
MODE=os.environ.get('VFX_MODE','before');FPS=24
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/BlenderPilot/Emberfall-Pilot-Vanguard.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['Vanguard_Rig'];rig.animation_data.action=bpy.data.actions['Pilot_Idle'];sc.frame_set(0)
# Identical, stationary scale reference in both videos. No animation of game state.
for o in list(bpy.data.objects):
 if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
cam=sc.camera;cam.location=(9,-13,15);target=Vector((0,-1,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=18
sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=int(os.environ.get('VFX_SAMPLES','20'));sc.cycles.use_denoising=False;sc.cycles.max_bounces=2;sc.cycles.diffuse_bounces=0;sc.cycles.glossy_bounces=0;sc.cycles.transparent_max_bounces=8;sc.cycles.transmission_bounces=0
sc.render.resolution_x=1280;sc.render.resolution_y=720;sc.render.resolution_percentage=100;sc.render.threads_mode='FIXED';sc.render.threads=2;sc.render.fps=FPS
sc.view_settings.view_transform='AgX';sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
world=sc.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.18,.22,.28,1);world.node_tree.nodes['Background'].inputs[1].default_value=.35
sun=bpy.data.lights.new('Matched direct sunlight','SUN');sun.energy=2.4;sun.angle=.025;o=bpy.data.objects.new('Matched direct sunlight',sun);sc.collection.objects.link(o);o.rotation_euler=(.38,-.5,-.55)
# Modest ambient fill is deterministic and avoids a grainy low-sample indirect preview.
mat=bpy.data.materials['Pilot_Atlas_Standard'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF');out=nodes.get('Material Output');tex=next(n for n in nodes if n.type=='TEX_IMAGE' and 'Atlas' in n.image.name)
em=nodes.new('ShaderNodeEmission');em.inputs['Strength'].default_value=.18;links.new(tex.outputs['Color'],em.inputs['Color']);add=nodes.new('ShaderNodeAddShader');links.new(bs.outputs[0],add.inputs[0]);links.new(em.outputs[0],add.inputs[1]);links.new(add.outputs[0],out.inputs['Surface'])
def plain(name,color,rough=1):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;return m
floor=bpy.data.objects['Preview_Ground'];floor.data.materials.clear();floor.data.materials.append(plain('Matched neutral arena',(.16,.20,.23)))
# Low-contrast 1m ground references, outside runtime Assets.
gridmat=plain('Metre grid',(.24,.28,.30))
for axis in range(2):
 for i in range(-7,8):
  bpy.ops.mesh.primitive_cube_add(size=1,location=(i if axis==0 else 0,0 if axis==0 else i,-.022));g=bpy.context.object;g.name='Review grid';g.scale=(.012,14,.004) if axis==0 else (14,.012,.004);g.data.materials.append(gridmat)
# Colour and shape are the effect, not a full-screen glow or camera shake.
sc.use_nodes=True;nt=sc.node_tree;nt.nodes.clear();rl=nt.nodes.new('CompositorNodeRLayers');gl=nt.nodes.new('CompositorNodeGlare');gl.glare_type='FOG_GLOW';gl.quality='MEDIUM';gl.threshold=1.6;gl.size=6;co=nt.nodes.new('CompositorNodeComposite');nt.links.new(rl.outputs['Image'],gl.inputs['Image']);nt.links.new(gl.outputs['Image'],co.inputs['Image'])
def emission(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear();out=n.new('ShaderNodeOutputMaterial');e=n.new('ShaderNodeEmission');e.inputs[0].default_value=(*color,1);e.inputs[1].default_value=1;tr=n.new('ShaderNodeBsdfTransparent');mix=n.new('ShaderNodeMixShader');mix.inputs[0].default_value=1;l.new(tr.outputs[0],mix.inputs[1]);l.new(e.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs[0]);return m,mix.inputs[0],e.inputs[0]
def mesh(name,v,f,material):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);sc.collection.objects.link(o);m.materials.append(material);o.visible_shadow=False;return o
objects={};materials={}
def dynamic(name,verts,faces,color,alpha=1,uv=None):
 if name not in objects:
  mat,opacity,tint=emission(name+' colour',color);materials[name]=(opacity,tint);objects[name]=mesh(name,verts,faces,mat)
  if uv:
   layer=objects[name].data.uv_layers.new()
   for loop in objects[name].data.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
 o=objects[name];o.hide_render=False
 if len(o.data.vertices)==len(verts):
  o.data.vertices.foreach_set('co',[x for v in verts for x in v]);o.data.update()
 else:raise RuntimeError('unstable effect topology '+name)
 if name not in globals().get('shader_uniforms',{}):
  materials[name][0].default_value=max(0,min(1,alpha));materials[name][1].default_value=(*color,1)
 return o

def band(name,points,width,color,alpha=1,closed=True):
 pts=[Vector(p) for p in points];v=[];n=len(pts)
 for i,p in enumerate(pts):
  tangent=(pts[(i+1)%n]-pts[(i-1)%n]).normalized();toward=(cam.location-p).normalized();side=tangent.cross(toward).normalized()*width*.5;v.extend([p-side,p+side])
 faces=[(2*i,2*((i+1)%n),2*((i+1)%n)+1,2*i+1) for i in range(n if closed else n-1)];return dynamic(name,v,faces,color,alpha)
def circle(name,r,z,color,alpha,width=.06,at=(0,0)):
 return band(name,[(at[0]+math.sin(i*math.tau/64)*r,at[1]+math.cos(i*math.tau/64)*r,z) for i in range(64)],width,color,alpha)
def crescent(name,r,a0,span,height,width,color,alpha):
 v=[];n=32
 for i in range(n+1):
  t=i/n;a=a0+span*t;w=width*math.sin(math.pi*t)**.7+.008
  for rr,z in [(r,0),(r-w*.35,.09),(r-w,0),(r-w*.35,-.07)]:v.append((rr*math.sin(a),rr*math.cos(a),height+z*math.sin(math.pi*t)))
 f=[(i*4+k,(i+1)*4+k,(i+1)*4+(k+1)%4,i*4+(k+1)%4) for i in range(n) for k in range(4)];return dynamic(name,v,f,color,alpha)
def shard(name,p,size,angle,color,alpha):
 x,y,z=p;rx,ry,h=size;v=[(x-rx,y-ry,z),(x+rx,y-ry,z),(x+rx,y+ry,z),(x-rx,y+ry,z),(x+.15*rx,y,h+z)];f=[(0,3,2,1),(0,1,4),(1,2,4),(2,3,4),(3,0,4)];return dynamic(name,v,f,color,alpha)
# Optimized proposal keeps amber Vanguard identity, a clear core and bounded trailing geometry.
def optimized(slot,age,cast):
 if age<0 or age>=1.15:return
 t=age;fade=max(0,min(1,(1.15-t)/.38));amber=(1.5,.63,.15);hot=(2.4,1.7,.72)
 if slot==0:
  r=1.1+min(1,t/.48)*2.3
  for i in range(3):
   a=t*8+i*math.tau/3
   crescent('swirl'+str(i),r*(1-i*.05),a,math.radians(118),.62+i*.16,.48*(1-t*.45),amber,fade*.82)
   crescent('swirl core'+str(i),r*(1-i*.05)+.015,a+.04,math.radians(109),.63+i*.16,.07,hot,fade*.92)
  circle('whirl floor',3.4*(.5+min(1,t/.5)*.5),.06,amber,fade*.3,.035)
  for i in range(10):
   a=i*2.399+t*2;rad=1.3+t*2;shard('whirl motes'+str(i),(math.sin(a)*rad,math.cos(a)*rad,.08+math.sin(min(1,t)*math.pi)*.45),(.025,.04,.13),a,amber,fade*.7)
 else:
  # Impact at release remains visible immediately; traveling wake is secondary visual feedback.
  circle('shock contact',.25+min(1,t/.35)*1.85,.07,hot,fade*.7,.09,at=(0,-2.5))
  for i in range(7):
   a=i*math.tau/7;dist=.45+t*1.3;shard('contact chips'+str(i),(math.sin(a)*dist,-2.5+math.cos(a)*dist,.04+max(0,math.sin(t*math.pi/1.15))*.8),(.07,.10,.18),a,(.62,.39,.16),fade)
  for j in range(6):
   delay=j*.055;u=t-delay
   if u<0 or u>.6:continue
   y=-.5-j*.68;h=.75*math.sin(min(1,u/.35)*math.pi)+.08
   for side in [-1,1]:
    shard('shock crest'+str(j)+str(side),(side*(.12+j*.10),y,.015),(.10+j*.027,.24,h),0,amber,max(0,1-u/.6)*.8)
   # Raised forked fault line through the centre, visible through rather than hiding the ground.
   band('fault'+str(j),[(-.12,y,.045),(.08,y-.2,.07),(-.03,y-.5,.04)],.10,hot,max(0,1-u/.6)*.8,closed=False)
  circle('contact inner',.15+min(1,t/.3)*.85,.11,hot,max(0,1-t/.55),.08,at=(0,-2.5))


shader_uniforms={}
def production_shader(obj,name,rgba,opacity,progress,style):
 # Node-for-node arithmetic translation of FilledSpell.shader fragment formula.
 # Render pipeline / colour management still differ from Unity, clearly labelled.
 if name not in shader_uniforms:
  m=obj.data.materials[0];n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
  def mathnode(op,a,b=None):
   q=n.new('ShaderNodeMath');q.operation=op
   for i,v in enumerate([a,b]):
    if v is None:continue
    if isinstance(v,(int,float)):q.inputs[i].default_value=v
    else:l.new(v,q.inputs[i])
   return q.outputs[0]
  def val(x):q=n.new('ShaderNodeValue');q.outputs[0].default_value=x;return q.outputs[0]
  def mix(a,b,f):
   q=n.new('ShaderNodeMixRGB');q.blend_type='MIX';l.new(f,q.inputs[0])
   for i,v in enumerate([a,b]):
    if isinstance(v,tuple):q.inputs[i+1].default_value=v
    else:l.new(v,q.inputs[i+1])
   return q.outputs[0]
  uv=n.new('ShaderNodeTexCoord');sep=n.new('ShaderNodeSeparateXYZ');l.new(uv.outputs['UV'],sep.inputs[0]);u,v=sep.outputs[0],sep.outputs[1]
  edge=mathnode('MINIMUM',mathnode('MAXIMUM',v,0),1)
  grain=mathnode('ADD',.5,mathnode('MULTIPLY',.5,mathnode('SINE',mathnode('ADD',mathnode('ADD',mathnode('MULTIPLY',u,47),mathnode('MULTIPLY',v,29)),mathnode('MULTIPLY',mathnode('SINE',mathnode('MULTIPLY',v,17)),2)))))
  op,pr,st=val(1),val(0),val(1)
  dissolve=mathnode('MULTIPLY',mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('MULTIPLY',mathnode('SUBTRACT',pr,.5),2),0),1),st)
  al=mathnode('MULTIPLY',op,mathnode('MINIMUM',mathnode('MAXIMUM',mathnode('MULTIPLY',mathnode('SUBTRACT',mathnode('ADD',grain,.45),dissolve),3),0),1))
  al=mathnode('MULTIPLY',al,mathnode('GREATER_THAN',al,.025))
  rgb=n.new('ShaderNodeRGB');rgb.outputs[0].default_value=rgba
  mul=n.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(.55,.55,.55,1);l.new(rgb.outputs[0],mul.inputs[1])
  tint=mix(mul.outputs[0],mix(rgb.outputs[0],(1,1,1,1),mathnode('MULTIPLY',edge,.72)),edge)
  geo=n.new('ShaderNodeNewGeometry');dot=n.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';l.new(geo.outputs['Normal'],dot.inputs[0]);direction=Vector((.35,-.4,.8)).normalized();dot.inputs[1].default_value=direction
  light=mathnode('ADD',.6,mathnode('MULTIPLY',.4,mathnode('ABSOLUTE',dot.outputs['Value'])))
  em=n.new('ShaderNodeEmission');l.new(tint,em.inputs[0]);l.new(light,em.inputs[1]);tr=n.new('ShaderNodeBsdfTransparent');mx=n.new('ShaderNodeMixShader');l.new(al,mx.inputs[0]);l.new(tr.outputs[0],mx.inputs[1]);l.new(em.outputs[0],mx.inputs[2]);out=n.new('ShaderNodeOutputMaterial');l.new(mx.outputs[0],out.inputs[0]);shader_uniforms[name]=(op,pr,st,rgb.outputs[0])
  for poly in obj.data.polygons:poly.use_smooth=True
 op,pr,st,col=shader_uniforms[name];op.default_value=rgba[3]*opacity;pr.default_value=progress;st.default_value=style;col.default_value=rgba

def baseline(frame,data):
 for p in frame['objects']:
  recipe=data['meshes'][p['mesh']];verts=[(v[0],-v[2],v[1]) for v in p['vertices']];tri=recipe['triangles'];faces=[tri[j:j+3] for j in range(0,len(tri),3)];color=p['color'];opacity=color[3]*p.get('opacity',1)
  # Exact recipe geometry/TRS/time/colour. Native Unity UV grain, queue/cull and depth alpha cannot be pixel-identical here.
  obj=dynamic('base '+str(p['id']),verts,faces,color[:3],opacity,recipe.get('uv'))
  production_shader(obj,'base '+str(p['id']),tuple(color),p.get('opacity',1),p.get('progress',0),p.get('style',1))
 for r in frame['rings']:
  band('base ring '+str(r['id']),[(v[0],-v[2],v[1]) for v in r['points']],r['width'],r['color'][:3],r['color'][3])

folder=OUT/MODE;folder.mkdir(exist_ok=True)
# Shared stage is rendered once; unlit alpha effects are separate native passes.
# This removes repeated sampling noise without changing camera/lighting or play speed.
stage_objects=[o for o in sc.objects if o.type=='MESH']
if os.environ.get('VFX_LAYERED')=='1':
 stage=OUT/'matched-stage.png'
 if not stage.exists():
  sc.render.filepath=str(stage);bpy.ops.render.render(write_still=True)
 hold=bpy.data.materials.new('Scale reference occlusion');hold.use_nodes=True;hn=hold.node_tree.nodes;hl=hold.node_tree.links;hn.clear();ho=hn.new('ShaderNodeOutputMaterial');hh=hn.new('ShaderNodeHoldout');hl.new(hh.outputs[0],ho.inputs[0])
 for o in stage_objects:
  if o.name.startswith('Review grid') or o==floor:o.hide_render=True
  else:
   o.data.materials.clear();o.data.materials.append(hold)
 sc.render.film_transparent=True;sc.render.image_settings.color_mode='RGBA';sc.cycles.samples=4

if os.environ.get('VFX_SMOKE')=='1':
 optimized(1,.18,0);sc.render.filepath=str(OUT/'clean-render-smoke.png');bpy.ops.render.render(write_still=True);print('VFX_SMOKE_READY');sys.exit(0)
data=json.loads(Path(os.environ['VFX_BASELINE']).read_text());FPS=data['fps'];sc.render.fps=FPS
idle_path=folder/'0000.png' if (folder/'0000.png').exists() else None
selected={int(x) for x in os.environ.get('VFX_FRAMES','').split(',') if x}
for i,frame in enumerate(data['frames']):
 if selected and i not in selected:continue
 for o in objects.values():o.hide_render=True
 if MODE=='before':baseline(frame,data)
 else:
  for index,cast in enumerate(data['casts']):optimized(cast['slot'],frame['time']-cast['time'],index)
 path=folder/('%04d.png'%i)
 active=any(not o.hide_render for o in objects.values())
 if not active and idle_path is not None:shutil.copyfile(idle_path,path);continue
 sc.render.filepath=str(path);bpy.ops.render.render(write_still=True)
 if not active:idle_path=path
 if i%12==0:print('VFX_FRAME',MODE,i,len(data['frames']),flush=True)
# Native scene retains the authored stage, material controls and all final effect objects.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('VFX-'+MODE+'.blend')))
print('VFX_DONE',MODE,len(data['frames']),flush=True)
