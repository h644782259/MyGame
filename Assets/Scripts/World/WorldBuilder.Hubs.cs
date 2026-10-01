using UnityEngine;
namespace Emberfall
{
 public static partial class WorldBuilder
 {
  private static void BuildTown(Transform parent,WorldResources r,int hub)
  {
   bool quarry=hub==1;Material floor=r.Material(quarry?new Color(.34f,.28f,.22f):new Color(.2f,.29f,.35f)),stone=r.Material(quarry?new Color(.47f,.34f,.24f):new Color(.43f,.49f,.56f)),roof=r.Material(quarry?new Color(.4f,.17f,.12f):new Color(.12f,.25f,.4f));
   Material glow=r.Material(quarry?new Color(.97f,.58f,.23f):new Color(.35f,.76f,1),true);
   Primitive(parent,"Town foundation",PrimitiveType.Cylinder,new Vector3(0,-.8f,0),new Vector3(47,.7f,47),floor);
   Primitive(parent,"Town plaza",PrimitiveType.Cube,new Vector3(0,-.015f,-3),new Vector3(22,.025f,30),stone);
   for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++)
   {
    Vector3 p=new Vector3(side*(quarry?13:14),0,-11+row*9);
    Primitive(parent,quarry?"Quarry workshop":"Observatory arcade",PrimitiveType.Cube,p+Vector3.up*1.9f,new Vector3(5.5f,3.8f,5.5f),stone);
    Primitive(parent,"Town roof",quarry?PrimitiveType.Cube:PrimitiveType.Sphere,p+Vector3.up*4,new Vector3(6.2f,quarry?.45f:2,6.2f),roof);
    WorldTraversal.AddBox(p,new Vector2(5.5f,5.5f));
    Primitive(parent,"Lit doorway",PrimitiveType.Cube,p+new Vector3(-side*2.78f,1.3f,0),new Vector3(.04f,2.4f,1.6f),glow);
   }
   if(quarry)
   {for(int side=-1;side<=1;side+=2){Pillar(parent,r,new Vector3(side*7,0,2),3.4f,false);Primitive(parent,"Quarry gantry",PrimitiveType.Cube,new Vector3(side*7,4.5f,2),new Vector3(.5f,3,.5f),roof);}
    Primitive(parent,"Forge gantry crossbeam",PrimitiveType.Cube,new Vector3(0,5.8f,2),new Vector3(15,.5f,.5f),roof);}
   else
   {Primitive(parent,"Observatory circular dais",PrimitiveType.Cylinder,new Vector3(0,.02f,3),new Vector3(9,.02f,9),floor);Crystal(parent,r,new Vector3(0,2,3),1.1f,glow);WorldTraversal.AddCircle(new Vector3(0,0,3),1.1f);}
   Portal(parent,r,new Vector3(0,0,11),glow);BuildHubNpcs(parent,r);
  }
  private static void BuildHubNpcs(Transform parent,WorldResources r)
  {
   for(int index=0;index<3;index++)
   {
    Vector3 p=GameSession.HubNpcPosition(index);Transform npc=Region(parent,index==0?"Camp Merchant":index==1?"Camp Blacksmith":"Star Exchange Steward");
    Material cloth=r.Material(index==0?new Color(.58f,.35f,.16f):index==1?new Color(.32f,.37f,.44f):new Color(.22f,.48f,.53f));
    Material skin=r.Material(new Color(.76f,.57f,.42f)),dark=r.Material(new Color(.12f,.16f,.2f));
    Primitive(npc,"NPC tunic",PrimitiveType.Capsule,p+Vector3.up*.85f,new Vector3(.7f,.58f,.5f),cloth);
    Primitive(npc,"NPC head",PrimitiveType.Sphere,p+Vector3.up*1.66f,Vector3.one*.47f,skin);
    for(int side=-1;side<=1;side+=2){Primitive(npc,"NPC boots",PrimitiveType.Capsule,p+new Vector3(side*.19f,.29f,0),new Vector3(.21f,.26f,.24f),dark);Primitive(npc,"NPC sleeves",PrimitiveType.Capsule,p+new Vector3(side*.45f,1.01f,0),new Vector3(.23f,.35f,.24f),cloth);}
    Primitive(npc,"Merchant counter",PrimitiveType.Cube,p+new Vector3(0,.45f,.65f),new Vector3(1.4f,.9f,.5f),dark);
    WorldTraversal.AddCircle(p,.43f);WorldTraversal.AddBox(p+new Vector3(0,0,.65f),new Vector2(1.4f,.5f));
   }
  }
 }
}
