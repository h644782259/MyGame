using System;using UnityEngine;using Emberfall;
namespace Emberfall {public sealed partial class CombatModel {public Vector3 PreviewSpine=>spine.localPosition;public Quaternion PreviewDecoration=>decoration.localRotation;} }
public static class CollectionPoseIsolationProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static bool Same(Quaternion a,Quaternion b)=>Quaternion.Difference(a,b)<.00001f;
 public static string Run()
 {
  foreach(var hero in new[]{HeroClass.Vanguard,HeroClass.Arcanist,HeroClass.Ranger,HeroClass.Summoner})
  {
   var model=new CombatModel(hero);model.PlayAction(1,false);var prior=model.Arm;int id=model.Identity;model.SamplePreview(7,CollectionPreviewAction.Attack,.3f);Check(Same(prior,model.Arm)&&model.Identity==id,"live model rejects preview sampling before isolation opt-in");
   model.ConfigurePreview();
   foreach(var action in new[]{CollectionPreviewAction.Idle,CollectionPreviewAction.Attack,CollectionPreviewAction.Cast})foreach(float progress in new[]{0f,.2f,.5f,.8f,1f})
   {
    Time.time=10;Time.frameCount=20;Time.deltaTime=.5f;model.SamplePreview(2.25f,action,progress);var arm=model.Arm;var weapon=model.Weapon;var spine=model.PreviewSpine;var decoration=model.PreviewDecoration;
    Time.time=900;Time.frameCount=1000;Time.deltaTime=0;model.SamplePreview(2.25f,action,progress);
    Check(Same(arm,model.Arm)&&Same(weapon,model.Weapon)&&Math.Abs(spine.y-model.PreviewSpine.y)<.000001f&&Same(decoration,model.PreviewDecoration),"preview pose is independent of world clock pause and frames");
    Check(model.Identity==id,"preview pose never dispatches an action identity");
   }
   model.SamplePreview(0,CollectionPreviewAction.Idle,1);float before=model.PreviewSpine.y;model.SamplePreview(.3f,CollectionPreviewAction.Idle,1);Check(Math.Abs(before-model.PreviewSpine.y)>.00001f,"local idle clock actually animates model joints");
  }
  return "PASS: "+n+" production isolated preview pose checks (managed transforms, no rendered engine frames)";
 }
}
