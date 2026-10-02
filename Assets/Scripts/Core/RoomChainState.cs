using System;
namespace Emberfall
{
    public enum RoomFailureReason { None, Death, Timeout, Abandoned, GenerationOrPathFailure }
    public sealed class RoomChainPlan
    {
        public readonly int Index, EnemyCount, Layout, Seed;
        public readonly bool Interlude, Boss;
        public readonly RoomObjective Objective;
        public RoomChainPlan(int index, int seed = 0)
        {
            Index = index; Seed = seed; Objective = RoomTactics.Objective(seed,index);
            Layout = index < 3 ? 20 + RoomTactics.Terrain(seed,index)*2 + (RoomTactics.Mirror(seed)<0?1:0) : 10+index;
            Interlude = index == 3; Boss = index == 4; EnemyCount = Interlude ? 0 : Boss ? 3 : 6;
        }
    }
    public sealed class RoomChainState
    {
        public RoomChainPlan Room {get;private set;}
        public bool DoorUnlocked {get;private set;}
        public bool Finished {get;private set;}
        public bool Failed {get;private set;}
        public RoomFailureReason Failure {get;private set;}
        public bool RewardClaimed {get;private set;}
        public int Seals {get;private set;}
        public float Progress {get;private set;}
        private bool[] spawned, defeated;
        private int spawnedCount, kills;
        public RoomChainState(int seed=0) { SetRoom(new RoomChainPlan(0,seed)); }
        private void SetRoom(RoomChainPlan plan)
        {
            Room=plan; spawned=new bool[plan.EnemyCount]; defeated=new bool[plan.EnemyCount];
            spawnedCount=kills=Seals=0; Progress=0; DoorUnlocked=false;
        }
        public bool Register(RoomChainPlan plan,int index)
        { if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||spawned[index])return false;spawned[index]=true;spawnedCount++;return true; }
        public bool Defeat(RoomChainPlan plan,int index)
        {
            if(Finished||!ReferenceEquals(plan,Room)||index<0||index>=spawned.Length||!spawned[index]||defeated[index])return false;
            defeated[index]=true;kills++;
            if(Room.Boss && kills==Room.EnemyCount && spawnedCount==Room.EnemyCount)Finished=true;
            if(Room.Objective==RoomObjective.Hunt && defeated[0] && spawnedCount==Room.EnemyCount)DoorUnlocked=true;
            return true;
        }
        // No deadline or reinforcements: clearing enemies always leaves a safe way to finish.
        // Leaving a seal pauses progress; it never erases a mobile player's partial capture.
        public void Advance(float delta,bool active,bool inside,bool contested)
        {
            if(!active||Finished||DoorUnlocked||spawnedCount!=Room.EnemyCount||!inside||contested||
               float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0||
               (Room.Objective!=RoomObjective.Purify&&Room.Objective!=RoomObjective.Escape))return;
            Progress+=Math.Min(delta,.25f);
            float required=Room.Objective==RoomObjective.Purify?3f:4f;
            if(Progress<required)return;
            Progress=0;Seals++;
            if(Room.Objective==RoomObjective.Escape||Seals==2)DoorUnlocked=true;
        }
        public bool ChooseInterlude(){if(Finished||!Room.Interlude||DoorUnlocked)return false;DoorUnlocked=true;return true;}
        public bool Next(bool nearDoor,bool blocked)
        {if(Finished||!DoorUnlocked||!nearDoor||blocked||Room.Index>=4)return false;SetRoom(new RoomChainPlan(Room.Index+1,Room.Seed));return true;}
        public bool ClaimReward(bool durable){if(!Finished||Failed||RewardClaimed||!durable)return false;RewardClaimed=true;return true;}
        public void Fail(RoomFailureReason reason=RoomFailureReason.Abandoned){if(Finished)return;Failure=reason==RoomFailureReason.None?RoomFailureReason.Abandoned:reason;Failed=Finished=true;DoorUnlocked=false;}
        public void Dispose(){if(!Finished)Failure=RoomFailureReason.Abandoned;Failed=Finished=true;DoorUnlocked=false;Room=null;spawned=defeated=new bool[0];}
    }
}
