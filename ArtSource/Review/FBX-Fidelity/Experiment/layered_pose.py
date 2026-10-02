"""Shared Blender reproduction of production-emitted local-TRS blend weights.
Neither changes source actions nor computes gameplay/action timing.
"""
import bpy
from mathutils import Matrix
UPPER={'Spine','Head','Mantle','UpperArm.L','UpperArm.R','Forearm.L','Forearm.R','Hand.L','Hand.R'}
LOWER={'Pelvis','Thigh.L','Thigh.R','Shin.L','Shin.R','Foot.L','Foot.R','Tabard.L','Tabard.R'}
class LayeredPose:
 def __init__(self,rig):
  self.rig=rig;self.modes={b.name:b.rotation_mode for b in rig.pose.bones};self.clips={n:next(a for a in bpy.data.actions if a.name.endswith('Pilot_'+n)) for n in ('Idle','Move','Basic')}
  assert set(self.modes)==UPPER|LOWER|{'Root'}
 def clip(self,name,t):
  for b in self.rig.pose.bones:b.rotation_mode=self.modes[b.name]
  a=self.clips[name];self.rig.animation_data.action=a;frame=a.frame_range.x+(a.frame_range.y-a.frame_range.x)*t;bpy.context.scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()
  return {b.name:b.matrix_basis.decompose() for b in self.rig.pose.bones}
 @staticmethod
 def mix(a,b,w):return (a[0].lerp(b[0],w),a[1].slerp(b[1],w),a[2].lerp(b[2],w))
 def sample(self,s):
  idle=self.clip('Idle',s['idle']);move=self.clip('Move',s['move']);base={n:self.mix(idle[n],move[n],s['speedWeight']) for n in idle}
  if s['basic']:
   attack=self.clip('Basic',s['progress'])
   for n in UPPER:base[n]=self.mix(base[n],attack[n],s['upperWeight'])
  self.rig.animation_data.action=None
  for b in self.rig.pose.bones:b.matrix_basis=Matrix.LocRotScale(*base[b.name])
  bpy.context.view_layer.update()
