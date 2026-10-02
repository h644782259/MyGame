using System;
namespace Emberfall
{
    // Per-attempt combat only; durable unlocks and reward receipts belong to ProgressionService.
    public sealed class ChapterCombatRun
    {
        public ChapterNode Node {get;private set;}
        public ChapterDifficulty Difficulty {get;private set;}
        public int Seed {get;private set;}
        public int RoomIndex {get;private set;}
        public int Epoch {get;private set;}
        public int Seals {get;private set;}
        public float Progress {get;private set;}
        public bool DoorUnlocked {get;private set;}
        public bool Finished {get;private set;}
        public bool Failed {get;private set;}
        public bool RewardClaimed {get;private set;}
        public RoomObjective Objective {get{return ChapterDefinition.RoomKind(Node,RoomIndex);}}
        public int EnemyCount {get{return Objective==RoomObjective.Rest?0:Objective==RoomObjective.Boss?3:6;}}
        private int registered,kills;
        private readonly bool[] spawned=new bool[6],defeated=new bool[6];
        public ChapterCombatRun(ChapterNode node,ChapterDifficulty difficulty,int seed)
        {ChapterDefinition.Get(node);if((int)difficulty<0||(int)difficulty>2)throw new ArgumentOutOfRangeException(nameof(difficulty));Node=node;Difficulty=difficulty;Seed=seed;Epoch=-1;}
        public void BindRoom(int epoch)
        {if(Finished)return;Epoch=epoch;registered=kills=Seals=0;Progress=0;DoorUnlocked=Objective==RoomObjective.Rest;Array.Clear(spawned,0,6);Array.Clear(defeated,0,6);}
        public bool Register(int room,int epoch,int index)
        {if(Finished||Epoch<0||room!=RoomIndex||epoch!=Epoch||index<0||index>=EnemyCount||spawned[index])return false;spawned[index]=true;registered++;return true;}
        public bool Defeat(int room,int epoch,int index)
        {
            if(Finished||Epoch<0||room!=RoomIndex||epoch!=Epoch||index<0||index>=EnemyCount||!spawned[index]||defeated[index])return false;
            defeated[index]=true;kills++;
            if(registered==EnemyCount&&((Objective==RoomObjective.Hunt&&defeated[0])||(Objective==RoomObjective.Boss&&kills==EnemyCount)))DoorUnlocked=true;
            return true;
        }
        public void Advance(float delta,bool active,bool inside,bool contested)
        {
            if(Finished||DoorUnlocked||!active||!inside||contested||registered!=EnemyCount||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0||
                (Objective!=RoomObjective.Purify&&Objective!=RoomObjective.Escape))return;
            Progress+=Math.Min(delta,.25f);float required=Objective==RoomObjective.Purify?3:4;
            if(Progress+.00001f<required)return;Progress=0;Seals++;
            if(Objective==RoomObjective.Escape||Seals==2)DoorUnlocked=true;
        }
        public bool Exit(bool near,bool blocked)
        {if(Finished||!DoorUnlocked||!near||blocked)return false;if(RoomIndex==1){Finished=true;return true;}RoomIndex=1;Epoch=-1;DoorUnlocked=false;return true;}
        public bool FinishBoss(){if(Finished||RoomIndex!=1||Objective!=RoomObjective.Boss||!DoorUnlocked)return false;Finished=true;return true;}
        public void ClaimReward(){if(Finished&&!Failed)RewardClaimed=true;}
        public void Fail(){if(Finished)return;Finished=Failed=true;DoorUnlocked=false;}
    }
}
