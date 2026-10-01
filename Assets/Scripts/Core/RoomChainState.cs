using System;
namespace Emberfall
{
 public sealed class RoomChainPlan
 {
  public readonly int Index,EnemyCount,Layout;public readonly bool Interlude,Boss;
  public RoomChainPlan(int index){Index=index;Layout=10+index;Interlude=index==3;Boss=index==4;EnemyCount=Interlude?0:Boss?3:5+index;}
 }
 public sealed class RoomChainState
 {
  public RoomChainPlan Room {get;private set;}=new RoomChainPlan(0);
  public bool DoorUnlocked {get;private set;}public bool Finished {get;private set;}public bool Failed {get;private set;}public bool RewardClaimed {get;private set;}
  private bool[] spawned=new bool[5],defeated=new bool[5];private int spawnedCount,kills;
  public bool Register(RoomChainPlan plan,int index)
  {if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||spawned[index])return false;spawned[index]=true;spawnedCount++;return true;}
  public bool Defeat(RoomChainPlan plan,int index)
  {if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||!spawned[index]||defeated[index])return false;defeated[index]=true;kills++;if(kills==Room.EnemyCount&&spawnedCount==Room.EnemyCount){if(Room.Boss)Finished=true;else DoorUnlocked=true;}return true;}
  public bool ChooseInterlude(){if(Finished||!Room.Interlude||DoorUnlocked)return false;DoorUnlocked=true;return true;}
  public bool Next(bool nearDoor,bool blocked)
  {if(Finished||!DoorUnlocked||!nearDoor||blocked||Room.Index>=4)return false;Room=new RoomChainPlan(Room.Index+1);spawned=new bool[Room.EnemyCount];defeated=new bool[Room.EnemyCount];spawnedCount=kills=0;DoorUnlocked=false;return true;}
  public bool ClaimReward(bool durable){if(!Finished||Failed||RewardClaimed||!durable)return false;RewardClaimed=true;return true;}
  public void Fail(){if(Finished)return;Failed=Finished=true;DoorUnlocked=false;}
  public void Dispose(){Failed=Finished=true;DoorUnlocked=false;Room=null;spawned=defeated=new bool[0];}
 }
}
