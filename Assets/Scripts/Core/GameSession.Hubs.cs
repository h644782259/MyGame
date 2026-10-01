using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  public int CurrentHub {get{return Progression==null?0:Progression.Profile.currentHub;}}
  public bool CanOpenTravelMap {get{return CanTravelNow();}}
  public HubNpcKind NearbyHubNpc
  {
   get
   {
    if(!HasStarted||InDungeon||IsDead||Player==null)return HubNpcKind.None;
    HubNpcKind kind=HubNpcKind.None;float nearest=2.65f;
    for(int index=0;index<3;index++){float distance=Vector3.Distance(Player.transform.position,HubNpcPosition(index));if(distance<nearest){nearest=distance;kind=(HubNpcKind)(index+1);}}
    return kind;
   }
  }
  public static Vector3 HubNpcPosition(int index){return index==0?new Vector3(-4.5f,0,-10):index==1?new Vector3(4.5f,0,-10):new Vector3(0,0,-5.5f);}
  private bool CanTravelNow()
  {
   bool nearby=false;if(Player!=null)foreach(var enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&Vector3.Distance(Player.transform.position,enemy.transform.position)<7){nearby=true;break;}
   return HubTravelRules.CanTravel(HasStarted,InDungeon,IsDead,nearby,changingZone);
  }
  public bool TravelToHub(int hub)
  {
   if(!CanTravelNow()){Notify("先离开挑战并远离敌人，再旅行。");return false;}
   if(hub==CurrentHub)return true;
   if(!PreserveWorldLoot()||!Progression.TravelToHub(hub)){Notify(Progression.LastError);return false;}
   loadingSaveSnapshot=true;
   try {if(!ChangeZone(false))return false;}
   finally {loadingSaveSnapshot=false;}
   Notify("已抵达"+HubTravelRules.Name(CurrentHub)+" · 商人、铁匠与兑换员在营地");return true;
  }
 }
}
