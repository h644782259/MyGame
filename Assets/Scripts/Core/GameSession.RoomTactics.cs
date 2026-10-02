using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private int previousRoomSeed=-1, beforePreviousRoomSeed=-1;
        private readonly DeferredRoomChoice pendingRoomChoice=new DeferredRoomChoice();
        public bool RoomCaptureInside {get;private set;}
        public bool RoomCaptureContested {get;private set;}
        private GameObject roomObjectiveMarker;
        private EnemyController roomSupplier;
        private float nextSupplyVisual;
        public bool RoomSupplyActive {get{return RoomChainRun!=null&&!RoomChainRun.Finished&&roomSupplier!=null&&!roomSupplier.IsDead;}}
        public bool IsRoomSupplier(EnemyController enemy){return RoomSupplyActive&&enemy==roomSupplier;}
        public int RoomRunSeed {get{return RoomChainRun==null?0:RoomChainRun.Room.Seed;}}
        private Vector3 RoomObjectivePoint
        {
            get
            {
                if(RoomChainRun.Room.Objective==RoomObjective.Escape)return new Vector3(0,0,11);
                int side=RoomTactics.Mirror(RoomRunSeed);
                return RoomChainRun.Seals==0?new Vector3(-side*8,0,-6):new Vector3(side*8,0,9);
            }
        }
        private string TacticalObjectiveStatus
        {
            get
            {
                var run=RoomChainRun;
                if(run.DoorUnlocked)return "北门已开 · 可撤离或留下获取击杀收益";
                if(run.Room.Interlude)return "选择星泉祝福";
                if(run.Room.Boss)return "击败王座守卫";
                if(run.Room.Objective==RoomObjective.Hunt)return "击败金环供能魔灵 · 切断附近敌人护援";
                return (run.Room.Objective==RoomObjective.Purify?"净化 "+run.Seals+"/2 · ":"突围 · ")+"站入光环 "+run.Progress.ToString("0.0")+"/"+(run.Room.Objective==RoomObjective.Purify?"3":"4")+"秒 · 敌人靠近暂停";
            }
        }
        private void BuildRoomObjective()
        {
            roomSupplier=null;roomObjectiveMarker=null;nextSupplyVisual=0;RoomCaptureInside=RoomCaptureContested=false;
            var plan=RoomChainRun.Room;
            if(plan.Interlude||plan.Boss)return;
            if(!WorldTraversal.IsWalkable(new Vector3(0,0,14),.65f)||!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,new Vector3(0,0,14),.65f))
            {RoomChainRun.Fail();Notify("房间路线不可达，已安全结束远征");return;}
            if(plan.Objective==RoomObjective.Hunt)return;
            Vector3 first=RoomObjectivePoint;
            Vector3 second=new Vector3(RoomTactics.Mirror(RoomRunSeed)*8,0,9);
            if(!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,first,.65f)||!WorldTraversal.CanReach(first,second,.65f))
            {RoomChainRun.Fail();Notify("目标路线不可达，已安全结束远征");return;}
            roomObjectiveMarker=WorldBuilder.MakeRoomObjective(first);
            roomObjectiveMarker.name="Room objective: stand within 2.4m";
            roomObjectiveMarker.transform.SetParent(world.transform,true);
        }
        private void TickRoomTactics()
        {
            if(RoomChainRun==null||RoomChainRun.Finished||InputBlocked||Player==null)return;
            if(TryOpenPendingRoomChoice())return;
            if(!RoomChainRun.DoorUnlocked && roomObjectiveMarker!=null)
            {
                Vector3 target=RoomObjectivePoint;
                bool contested=false;
                foreach(var enemy in Enemies)
                    if(enemy!=null&&!enemy.IsDead&&Vector3.Distance(enemy.transform.position,target)<3.8f&&WorldTraversal.HasLineOfSight(enemy.transform.position,target)){contested=true;break;}
                RoomCaptureInside=Vector3.Distance(Player.transform.position,target)<2.4f;
                RoomCaptureContested=contested;
                int previousSeals=RoomChainRun.Seals;
                RoomChainRun.Advance(Time.deltaTime,true,RoomCaptureInside,RoomCaptureContested);
                if(previousSeals!=RoomChainRun.Seals)RoomCaptureInside=RoomCaptureContested=false;
                roomObjectiveMarker.transform.position=RoomObjectivePoint;
                if(RoomChainRun.DoorUnlocked){roomObjectiveMarker.SetActive(false);OpenRoomGate();}
            }
            if(roomSupplier!=null&&!roomSupplier.IsDead&&Time.time>=nextSupplyVisual)
            {
                nextSupplyVisual=Time.time+.8f;
                CombatFx.Ring(roomSupplier.transform.position,1.1f,new Color(1,.8f,.25f),.85f,.08f);
                foreach(var enemy in Enemies)
                    if(RoomSupportMultiplier(enemy)<1)CombatFx.Ring(enemy.transform.position,.8f,new Color(.25f,.85f,1),.85f,.06f);
            }
        }
        public float RoomSupportMultiplier(EnemyController enemy)
        {
            if(RoomChainRun==null||RoomChainRun.Finished||roomSupplier==null||roomSupplier.IsDead||enemy==null||enemy==roomSupplier||enemy.IsBoss)return 1;
            return Vector3.Distance(enemy.transform.position,roomSupplier.transform.position)<=6 && WorldTraversal.HasLineOfSight(enemy.transform.position,roomSupplier.transform.position) ? .7f : 1;
        }
        private bool TryOpenPendingRoomChoice()
        {
            bool valid=HasStarted&&InDungeon&&!IsDead&&Player!=null&&RoomChainRun!=null&&!RoomChainRun.Finished&&
                RoomChainRun.Room.Index==0&&RoomChainRun.DoorUnlocked&&RunChoices.CompletedWave==0;
            if(!pendingRoomChoice.TryClaim(RoomChainRun==null?null:RoomChainRun.Room,Player==null?-1:Player.CombatEpoch,
                Time.frameCount,valid,!InputBlocked))return false;
            RunChoices.PrepareRoomChoice(1,Progression.Profile,MobileControls.Active,runSeed);
            UpdateTimeScale();return RunChoices.AwaitingChoice;
        }
        private void OpenRoomGate()
        {
            if(RoomChainRun!=null&&RoomChainRun.Room.Index==0&&RunChoices.CompletedWave==0&&Player!=null)
                pendingRoomChoice.Request(RoomChainRun.Room,Player.CombatEpoch,Time.frameCount);
            if(roomExitMarker!=null)roomExitMarker.SetActive(true);
            Notify(pendingRoomChoice.Pending?"首房完成 · 选择本局打法":"目标完成 · 北门已开；撤离会放弃剩余敌人的击杀收益");
        }
    }
}
