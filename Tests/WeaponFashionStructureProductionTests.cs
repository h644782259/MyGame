using System;using System.Linq;using UnityEngine;using Emberfall;
public static class WeaponFashionStructureProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 public static void Main()
 {
  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))for(int tier=1;tier<=4;tier++)
  {
   var host=new GameObject("weapon fashion");var m=CombatModel.Hero(host.transform,hero);m.ApplyEquipment(new ItemData{id="weapon",slot=ItemSlot.Weapon,level=(tier-1)*25+1,rarity=Rarity.Legendary,upgradeLevel=10},null,null);int common=0;
   foreach(var rarity in new[]{Rarity.Common,Rarity.Rare})
   {
    m.ApplyFashion(null,new FashionData{id=rarity.ToString(),slot=FashionSlot.Weapon,rarity=rarity});UnityEngine.Object.Flush();var root=m.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fashion Weapon");var parts=root.GetComponentsInChildren<MeshFilter>(false);
    if(rarity==Rarity.Common)common=parts.Length;else Check(parts.Length>common&&parts.Count(p=>p.transform.name.StartsWith("Rare "))==2,"rare weapon fashion has distinct actual structural pieces beyond common palette");
    Check(parts.All(p=>p.sharedMesh.triangles.Length>0&&p.sharedMesh.vertices.Length>0),"fashion construction uses real nonempty mesh recipes");
    Check(root.parent.name==(hero==HeroClass.Vanguard?"Sword Wrist":hero==HeroClass.Ranger?"Bow":"Staff Wrist"),"rarity structure keeps the actual weapon anchor");
    var vertices=parts.SelectMany(p=>p.sharedMesh.vertices.Select(v=>root.InverseTransformPoint(p.transform.TransformPoint(v)))).ToArray();
    if(hero==HeroClass.Arcanist||hero==HeroClass.Summoner)Check(vertices.All(v=>v.y>.7f),"caster fashion stays above grip and safe lower shaft");
    else if(hero==HeroClass.Vanguard)Check(vertices.All(v=>!(Math.Abs(v.x)<.09f&&v.y<.08f)),"sword fashion preserves grip opening");
    else Check(vertices.All(v=>Math.Abs(v.y)>.25f),"bow fashion preserves grip string hand and arrow-rest opening");
   }
   UnityEngine.Object.Destroy(host);UnityEngine.Object.Flush();Check(!UnityEngine.Object.All.OfType<Material>().Any(v=>!v.destroyed),"fashion swap disposes owned palette");
  }
  Console.WriteLine("PASS: "+n+" actual common/rare weapon fashion construction checks; static managed geometry only");
 }
}
