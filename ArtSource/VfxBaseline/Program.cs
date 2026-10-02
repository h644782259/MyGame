using System;using System.Collections.Generic;using System.Linq;using System.Reflection;using System.Text.Json;using UnityEngine;using Emberfall;
class Program {
 const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};static float[] C(Color c)=>new[]{c.r,c.g,c.b,c.a};
 static void Main(){
 var casts=new[]{new{time=.75f,slot=0,swingSide=-1},new{time=2.75f,slot=0,swingSide=1},new{time=5f,slot=1,swingSide=-1},new{time=7f,slot=1,swingSide=1}};var frames=new List<object>();var meshes=new Dictionary<string,object>();int maxPieces=0;
 for(int frame=0;frame<216;frame++){
  float time=frame/24f;foreach(var old in GameObject.All.ToArray())UnityEngine.Object.Destroy(old);GameObject.All.Clear();
  var hero=new GameObject("Hero reference").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=hero,HasStarted=true};Application.isMobilePlatform=false;EffectPreferences.EffectsScale=1;EffectPreferences.ReducedEffects=false;
  var objects=new List<object>();var rings=new List<object>();
  for(int cast=0;cast<casts.Length;cast++){
   float age=time-casts[cast].time;if(age<0||age>=.45f)continue;
   if(casts[cast].slot==1&&age<.34f){
    FilledSkillVfx.Crescent(hero,Vector3.zero,Vector3.forward,4.8f,new Color(1,.85f,.4f),casts[cast].swingSide,CombatVisualPriority.ActionBody);
    var root=GameObject.All.Last(o=>o.GetComponent<FilledSkillVfx>()!=null);var effect=root.GetComponent<FilledSkillVfx>();typeof(FilledSkillVfx).GetField("age",Private).SetValue(effect,age);
    var pieces=(Array)typeof(FilledSkillVfx).GetField("pieces",Private).GetValue(effect);int pieceIndex=0;
    foreach(var piece in pieces){if(piece==null)continue;typeof(FilledSkillVfx).GetMethod("Animate",Private).Invoke(effect,new[]{piece});var tr=(Transform)piece.GetType().GetField("Transform").GetValue(piece);int id=pieceIndex++;if(!tr.gameObject.activeInHierarchy)continue;var mesh=tr.gameObject.GetComponent<MeshFilter>().sharedMesh;var renderer=tr.gameObject.GetComponent<MeshRenderer>();string key=mesh.name;
     if(!meshes.ContainsKey(key))meshes[key]=new{vertices=mesh.vertices.Select(V).ToArray(),triangles=mesh.triangles,uv=mesh.uv.Select(v=>new[]{v.x,v.y}).ToArray()};
     objects.Add(new{id=$"cast{cast}/piece{id}",mesh=key,vertices=mesh.vertices.Select(v=>V(tr.TransformPoint(v))).ToArray(),color=C(renderer.Tint),opacity=renderer.Opacity,progress=renderer.Progress,style=renderer.Style});
    }
   }
   float life=casts[cast].slot==0?.45f:.4f;if(age>=life)continue;
   var ring=CombatFx.Ring(casts[cast].slot==0?Vector3.zero:new Vector3(0,0,2.5f),casts[cast].slot==0?3.4f:2.1f,new Color(1,.65f,.26f),life,casts[cast].slot==0?.2f:.16f);
   var fade=ring.GetComponent<FadingCombatEffect>();typeof(FadingCombatEffect).GetField("age",Private).SetValue(fade,age);Time.deltaTime=Time.unscaledDeltaTime=0;ring.Call("Update");var line=ring.GetComponent<LineRenderer>();rings.Add(new{id=$"cast{cast}/ring",points=line.Positions.Take(line.positionCount).Select(v=>V(ring.transform.TransformPoint(v))).ToArray(),width=line.widthMultiplier,color=C(line.startColor)});
  }
  maxPieces=Math.Max(maxPieces,objects.Count);frames.Add(new{time,objects,rings});
 }
 var result=new{schema="emberfall-vfx-worldframes-v1",pin="03422ab83d0ab6a8f84f2147a81d8aeb63a6cd5e",fps=24,duration=9,rank=1,platform="desktop full effects",casts,meshes,frames,notes=new[]{"Actual pinned FilledSkillVfx.Crescent+Animate and FilledVfxRecipes; actual CombatFx.Ring/FadingCombatEffect.Setup+Update executed with managed engine doubles.","2 closed crescent volumes and 4 crystal shards on groundshock; Whirlwind has ring only in this baseline. Melee dispatch adds no extra WeaponSlash.","Independent cast visual samples at real 1x effect ages; inter-cast cooldown gaps edited, not continuous gameplay.","No enemies/no hit feedback. Dynamic sword root-tip ribbon not exported: needs actual animated weapon sockets; do not represent as absent from baseline game.","Unity FilledSpell fragment shading, LineRenderer camera-facing tessellation, transparent sorting and engine render pipeline are not reproduced by this geometry export.","Shader alpha is color.a * opacity * dissolve grain, with clip below .025; both alpha uniforms exported separately. Hero at origin facing Unity+Z, Y up."}};
 string path=Environment.GetEnvironmentVariable("EMBERFALL_VFX_OUTPUT");if(string.IsNullOrEmpty(path))throw new Exception("EMBERFALL_VFX_OUTPUT required");System.IO.File.WriteAllText(path,JsonSerializer.Serialize(result));Console.WriteLine($"Exported {frames.Count} frames, {meshes.Count} shared meshes, maximum {maxPieces} filled pieces; {path}");
 }
}
