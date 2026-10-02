using System;using System.Linq;using UnityEngine;using Emberfall;
namespace Emberfall
{
 public enum EnemyKind{Slime,Other}public sealed class EnemyController:MonoBehaviour{public bool IsDead,IsBoss;public EnemyKind Kind;}
 public static class MobileControls{public static bool Active=true;}public static class WorldTraversal{public static int Revision;}
 public static class ProceduralVisuals{public static GameObject Create(string name,PrimitiveType type,Material material){var o=new GameObject(name);o.AddComponent<MeshRenderer>().sharedMaterial=material;return o;}}
 public sealed class FadingCombatEffect:MonoBehaviour{public void Setup(LineRenderer l,Color c,float s,float d,bool b){}}
}
public static class ElementalPriorityProductionTests
{
 static int checks;static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static GameObject[] Fields()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<ElementalFieldVisual>()!=null).ToArray();
 static GameObject[] Particles()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy&&o.GetComponent<ParticleSystem>()!=null).ToArray();
 static void Reset(){foreach(var o in GameObject.All.ToArray())UnityEngine.Object.Destroy(o);Check(CombatVisualLease.Active==0,"cleanup returns every global lease");GameObject.All.Clear();Time.time=0;Application.isMobilePlatform=true;EffectPreferences.ReducedEffects=false;}
 static void Fill(int count,CombatVisualPriority p){for(int i=0;i<count;i++)Check(CombatVisualLease.Attach(new GameObject("filler"),p)!=null,"filler lease accepted");}
 public static string Run()
 {
  Reset();Fill(20,CombatVisualPriority.Decoration);var root=new GameObject("area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);
  Check(Fields().Length==1,"full decoration pool must retain actual Area primary field");Check(Particles().Length==0&&CombatVisualLease.Active==20,"failed optional particle cannot displace field or exceed budget");
  root.SetActive(false);Check(CombatVisualLease.Active==19,"parent disable immediately releases field lease");root.SetActive(true);Check(CombatVisualLease.Active==20,"parent reenable reacquires field lease once");UnityEngine.Object.Destroy(root);Check(CombatVisualLease.Active==19,"destroy after disable is idempotent");
  Reset();Fill(20,CombatVisualPriority.Decoration);var enemy=new GameObject("enemy").AddComponent<EnemyController>();ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,2);
  Check(Fields().Length==1&&Particles().Length==0,"full decoration pool retains actual OnEnemy body shape without particles");Time.time=3;enemy.gameObject.Call("Update");Check(Fields().Length==0&&CombatVisualLease.Active==19,"body shape expires even when particles never allocated");
  Reset();enemy=new GameObject("enemy").AddComponent<EnemyController>();ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);var shape=Fields().Single();var particle=Particles().Single();var material=particle.GetComponent<ParticleSystemRenderer>().sharedMaterial;
  Check(shape.transform.parent==enemy.transform&&particle.transform.parent==enemy.transform,"aura main shape must be sibling of optional particles");
  UnityEngine.Object.Destroy(particle);Check(!shape.Destroyed&&shape.activeInHierarchy&&CombatVisualLease.Active==1&&material.Destroyed,"optional particle destruction retains shape and releases owned material");
  enemy.gameObject.SetActive(false);Check(CombatVisualLease.Active==0,"enemy disable releases child shape lease immediately");UnityEngine.Object.Destroy(enemy.gameObject);Check(shape.Destroyed&&CombatVisualLease.Active==0,"enemy destroy retires all visual children once");
  Reset();enemy=new GameObject("eviction enemy").AddComponent<EnemyController>();ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Poison,2);shape=Fields().Single();particle=Particles().Single();Fill(18,CombatVisualPriority.Primary);
  Check(CombatVisualLease.Attach(new GameObject("finisher"),CombatVisualPriority.Finale)!=null,"finisher preempts a full pool");
  Check(particle.Destroyed&&!shape.Destroyed&&shape.activeInHierarchy&&CombatVisualLease.Active==20,"budget eviction of optional child cannot destroy main sibling");
  Check(CombatVisualLease.Attach(new GameObject("second finisher"),CombatVisualPriority.Finale)!=null&&shape.Destroyed&&CombatVisualLease.Active==20,"later higher priority eviction releases body shape exactly once");
  Reset();Fill(20,CombatVisualPriority.Finale);root=new GameObject("blocked area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);enemy=new GameObject("blocked enemy").AddComponent<EnemyController>();ElementalCombatVfx.OnEnemy(enemy,ElementalCombatVfx.Element.Fire,2);
  Check(Fields().Length==0&&Particles().Length==0&&CombatVisualLease.Active==20,"higher priority saturation fails closed without leaked child leases");Reset();
  // Actual per-kind particle cap also must not block a new primary shape.
  EffectPreferences.ReducedEffects=true;for(int i=0;i<10;i++)Check(ElementalCombatVfx.Create(null,"particle",ElementalCombatVfx.Element.Poison,10,1)!=null,"reduced particle quota fill");root=new GameObject("area");ElementalCombatVfx.Area(root.transform,3,ElementalCombatVfx.Element.Poison);Check(Fields().Length==1&&Particles().Length==10,"local particle cap never gates field silhouette");Reset();
  return "PASS: "+checks+" actual elemental entry/lease/lifecycle checks (managed Unity substitute, not rendered)";
 }
}
