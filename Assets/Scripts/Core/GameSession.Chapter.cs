using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public ChapterNode SelectedChapterNode {get;set;}
        public ChapterDifficulty SelectedChapterDifficulty {get;set;}
        public int SelectedChapterTier {get;set;}=1;
        public bool SelectedChapterLimitedHealing {get;set;}
        public ChapterCombatRun ChapterRun {get;private set;}
        public int ChapterRewardMaterials {get{return chapterReceipt==null?0:chapterReceipt.Materials;}}
        public bool ChapterActive {get{return ChapterRun!=null;}}
        public bool ChapterFinished {get{return ChapterActive&&ChapterRun.Finished;}}
        public bool ChapterRewardPending {get{return ChapterFinished&&!ChapterRun.Failed&&!ChapterRun.RewardClaimed;}}
        public ChapterNode ActiveChapterNode {get{return ChapterActive?ChapterRun.Node:SelectedChapterNode;}}
        public ChapterDifficulty ActiveChapterDifficulty {get{return ChapterActive?ChapterRun.Difficulty:ChapterDifficulty.Normal;}}
        public int ChapterRoomIndex {get{return ChapterActive?ChapterRun.RoomIndex:-1;}}
        public int ChapterSeed {get{return ChapterActive?ChapterRun.Seed:0;}}
        public bool OpenChapterSelectionAllowed {get{return HasStarted&&!InDungeon&&!IsDead&&!Paused&&!BackgroundPaused&&IsInCamp;}}
        public bool NearChapterExit {get{return ChapterActive&&!ChapterFinished&&ChapterRun.DoorUnlocked&&chapterPlan!=null&&Player!=null&&Vector3.Distance(Player.transform.position,chapterPlan.Exit)<3.8f;}}
        private ChapterRunReceipt chapterReceipt;
        private ChapterRoomPlan chapterPlan;
        private bool enteringChapter;
        private GameObject chapterExit,chapterObjective;
        private EnemyController chapterSupplier;
        private float chapterSupplyVisual;
        private sealed class ChapterEnemyReceipt {public ChapterCombatRun Run;public int Room,Epoch,Index;}
        private readonly Dictionary<EnemyController,ChapterEnemyReceipt> chapterEnemies=new Dictionary<EnemyController,ChapterEnemyReceipt>();
        private Vector3 ChapterObjectivePoint {get{return chapterPlan.Objectives[Mathf.Clamp(ChapterRun.Seals,0,chapterPlan.Objectives.Length-1)];}}
        public string ChapterObjectiveStatus
        {
            get
            {
                if(!ChapterActive)return "";
                if(ChapterFinished)return ChapterRun.Failed?"章节挑战结束 · 返回营地":ChapterRewardPending?"节点完成 · 奖励待保存":"节点完成 · 奖励已保存";
                string prefix="房间 "+(ChapterRoomIndex+1)+" / 2 · ";
                if(ChapterRun.DoorUnlocked)return prefix+(ChapterRoomIndex==0?"目标完成 · 前往出口进入下一房":"目标完成 · 前往出口完成节点");
                if(ChapterRun.Objective==RoomObjective.Hunt)return prefix+"击败金环断供目标 · 可绕开其余敌人";
                if(ChapterRun.Objective==RoomObjective.Boss)return prefix+"击败星环执政官与护卫 · 摧毁供能锚可中止扫射";
                return prefix+(ChapterRun.Objective==RoomObjective.Purify?"净化 "+ChapterRun.Seals+"/2":"撤离")+" · 站入光环 "+ChapterRun.Progress.ToString("0.0")+"/"+(ChapterRun.Objective==RoomObjective.Purify?"3":"4")+"秒 · 敌人入环时暂停累计";
            }
        }
        public string ChapterObjectiveCompact
        {
            get
            {
                if(!ChapterActive)return "";
                if(ChapterFinished)return ChapterRun.Failed?"挑战结束\n返回营地重整":"节点完成\n"+(ChapterRewardPending?"奖励待保存 · 可重试":"奖励已保存 · 返回营地");
                if(ChapterRun.DoorUnlocked)return ChapterRoomIndex==0?"目标完成\n前往出口 · 进入下一房":"目标完成\n前往出口 · 完成节点";
                if(ChapterRun.Objective==RoomObjective.Hunt)return "猎杀金环断供目标\n可绕开其余敌人";
                if(ChapterRun.Objective==RoomObjective.Boss)return "击败首领与护卫\n摧毁供能锚中止扫射";
                return (ChapterRun.Objective==RoomObjective.Purify?"净化 "+ChapterRun.Seals+"/2":"撤离")+" · "+ChapterRun.Progress.ToString("0.0")+"/"+(ChapterRun.Objective==RoomObjective.Purify?"3":"4")+"秒\n敌人入环暂停累计";
            }
        }
        public bool ConfirmChapterEnter()
        {
            if(!OpenChapterSelectionAllowed||Player==null)return false;
            if(!SaveBeforeLeaving())return false;
            ChapterRunReceipt receipt;
            if(!Progression.TryBeginChapterNode(SelectedChapterNode,SelectedChapterDifficulty,SelectedChapterTier,out receipt)){Notify(Progression.LastError);return false;}
            chapterReceipt=receipt;ChapterRun=new ChapterCombatRun(receipt.Node,receipt.Difficulty,Random.Range(0,1000000));
            enteringChapter=true;ChallengeRun=SelectedChapterLimitedHealing;
            try {if(!ChangeZone(true)){ResetChapterRun();return false;}}
            catch(System.Exception error){FailChapter("章节生成失败："+error.Message);return InDungeon;}
            finally {enteringChapter=false;changingZone=false;}
            UpdateTimeScale();Notify(ChapterDefinition.Get(receipt.Node).Story+" · "+ChapterObjectiveStatus);return true;
        }
        private void ResetChapterRun()
        {
            if(ChapterRun!=null)ChapterRun.Fail();ChapterRun=null;chapterReceipt=null;chapterPlan=null;
            chapterEnemies.Clear();chapterSupplier=null;chapterExit=chapterObjective=null;
            if(Progression!=null)Progression.CancelChapterRun();
        }
        private void BeginChapterRoom()
        {
            ChapterRun.BindRoom(Player.CombatEpoch);chapterEnemies.Clear();chapterSupplier=null;chapterSupplyVisual=0;
            DungeonWave=ChapterRoomIndex+1;wavePopulation=Mathf.Max(1,ChapterRun.EnemyCount);objectiveHealedThisWave=false;
            chapterExit=WorldBuilder.MakeLootBeacon(chapterPlan.Exit,new Color(.25f,.8f,1));chapterExit.transform.SetParent(world.transform,true);chapterExit.SetActive(ChapterRun.DoorUnlocked);
            if(!WorldTraversal.CanReach(chapterPlan.Entrance,chapterPlan.Exit,.65f)){FailChapter("章节出口不可达，已安全结束挑战");return;}
            if(ChapterRun.Objective==RoomObjective.Rest)
            {if(!ChallengeRun)Player.Heal(Player.MaxHealth*.25f);else HealingCharges=Mathf.Min(3,HealingCharges+1);Notify("星台休憩 · 整备后前往出口迎战首领");return;}
            if(ChapterRun.Objective==RoomObjective.Purify||ChapterRun.Objective==RoomObjective.Escape)
            {
                foreach(var point in chapterPlan.Objectives)if(!WorldTraversal.CanReach(chapterPlan.Entrance,point,.65f)){FailChapter("章节目标不可达，已安全结束挑战");return;}
                chapterObjective=WorldBuilder.MakeRoomObjective(ChapterObjectivePoint);chapterObjective.transform.SetParent(world.transform,true);
            }
            var occupied=new List<Vector3>();
            for(int index=0;index<ChapterRun.EnemyCount;index++)
            {
                bool boss=ChapterRun.Objective==RoomObjective.Boss&&index==0;
                EnemyKind kind=boss?EnemyKind.Guardian:index==0?EnemyKind.Wisp:index%3==1?EnemyKind.Guardian:index%3==2?EnemyKind.Goblin:EnemyKind.Slime;
                Vector3 point;float radius=boss?1.3f:kind==EnemyKind.Guardian?.65f:.5f;
                if(Enemies.Count>=ChapterRun.EnemyCount||!ChapterRoomGeometry.TrySpawn(chapterPlan,index,occupied,radius,out point)||
                    !WorldTraversal.CanReach(chapterPlan.Entrance,point,radius)||!ChapterRun.Register(ChapterRoomIndex,Player.CombatEpoch,index))
                {FailChapter("章节敌人生成位置不可达，已安全结束挑战");return;}
                occupied.Add(point);SpawnEnemy(kind,DungeonEntryLevel,point,boss);var enemy=Enemies[Enemies.Count-1];
                chapterEnemies.Add(enemy,new ChapterEnemyReceipt{Run=ChapterRun,Room=ChapterRoomIndex,Epoch=Player.CombatEpoch,Index=index});
                if(index==0&&!boss)chapterSupplier=enemy;
                if(ActiveChapterNode==ChapterNode.Redrock&&ActiveChapterDifficulty!=ChapterDifficulty.Normal)
                    enemy.ConfigureEscapePost(EscapeRoomFormation.Role(index),point);
                if(boss)LargeExpeditionBoss.ConfigureChapter(enemy,DungeonTier,ChapterSeed,ActiveChapterDifficulty);
            }
            ChapterHazards.Configure(this,ActiveChapterNode,ActiveChapterDifficulty,ChapterRoomIndex,ChapterSeed,chapterPlan);
        }
        private void TickChapterRun()
        {
            if(!ChapterActive||ChapterFinished||InputBlocked||Player==null)return;
            if(chapterObjective!=null&&!ChapterRun.DoorUnlocked)
            {
                Vector3 target=ChapterObjectivePoint;bool contested=false;
                foreach(var enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&enemy.isActiveAndEnabled&&RoomTacticalRegion.Contests(CombatFx.Flat(enemy.transform.position-target).sqrMagnitude,enemy.NavigationRadius,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,target)){contested=true;break;}
                ChapterRun.Advance(Time.deltaTime,true,RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-target).sqrMagnitude),contested);
                chapterObjective.transform.position=ChapterObjectivePoint;
            }
            if(ChapterRun.DoorUnlocked){if(chapterExit!=null)chapterExit.SetActive(true);if(chapterObjective!=null)chapterObjective.SetActive(false);}
            if(chapterSupplier!=null&&!chapterSupplier.IsDead&&Time.time>=chapterSupplyVisual)
            {
                chapterSupplyVisual=Time.time+.8f;
                if(ChapterRun.Objective==RoomObjective.Hunt||ChapterHasSupport)CombatFx.Ring(chapterSupplier.transform.position,1.1f,new Color(1,.8f,.25f),.85f,.08f);
                if(ChapterHasSupport)foreach(var enemy in Enemies)if(ChapterSupportMultiplier(enemy)<1)CombatFx.Ring(enemy.transform.position,.8f,new Color(.25f,.85f,1),.85f,.06f);
            }
        }
        private bool ChapterHasSupport {get{return ChapterActive&&!ChapterFinished&&ActiveChapterNode==ChapterNode.ForestCourt&&ActiveChapterDifficulty!=ChapterDifficulty.Normal&&chapterSupplier!=null&&!chapterSupplier.IsDead&&chapterSupplier.isActiveAndEnabled;}}
        public bool IsChapterSupplier(EnemyController enemy){return ChapterHasSupport&&enemy==chapterSupplier;}
        public float ChapterSupportMultiplier(EnemyController enemy)
        {return ChapterHasSupport&&enemy!=null&&enemy!=chapterSupplier&&!enemy.IsBoss&&!enemy.IsDead&&enemy.isActiveAndEnabled&&RoomTacticalRegion.ReceivesSupport(CombatFx.Flat(enemy.transform.position-chapterSupplier.transform.position).sqrMagnitude,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,chapterSupplier.transform.position)?.7f:1;}
        private void RecordChapterDefeat(EnemyController enemy)
        {ChapterEnemyReceipt receipt;if(!ChapterActive||Player==null||!chapterEnemies.TryGetValue(enemy,out receipt)||receipt.Run!=ChapterRun||receipt.Epoch!=Player.CombatEpoch)return;chapterEnemies.Remove(enemy);ChapterRun.Defeat(receipt.Room,receipt.Epoch,receipt.Index);}
        private void FinalizeChapterBoss(){if(ChapterActive&&ChapterRun.FinishBoss())FinalizeChapter();}
        private void FailChapter(string reason){ChapterRun.Fail();Progression.CancelChapterRun();Notify(reason);SuspendInputs();UpdateTimeScale();}
        private void FinalizeChapter()
        {DungeonCleared=true;TrySettleChapterReward();LastRunSummary=BuildRunSummary(true);SuspendInputs();UpdateTimeScale();GameAudio.Play(SoundCue.Victory);}
        public bool TrySettleChapterReward()
        {
            if(!ChapterRewardPending)return true;
            if(!Progression.TryCompleteChapterNode(chapterReceipt)){Notify(Progression.LastError);return false;}
            ChapterRun.ClaimReward();Notify("章节节点已保存 · 碎片 +"+chapterReceipt.Materials);return true;
        }
        public bool EnterNextChapterRoom()
        {
            if(!NearChapterExit||InputBlocked||Player==null||!SaveBeforeLeaving())return false;
            if(!ChapterRun.Exit(true,false))return false;
            if(ChapterFinished){FinalizeChapter();return true;}
            SuspendInputs();changingZone=true;
            try
            {
            AbandonSideEvent();int oldEpoch=Player.CombatEpoch;
            foreach(var enemy in Enemies)if(enemy!=null){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}Enemies.Clear();chapterEnemies.Clear();
            foreach(var obj in transientObjects)if(obj!=null){obj.SetActive(false);Destroy(obj);}transientObjects.Clear();
            if(world!=null){world.SetActive(false);Destroy(world);}Player.RetireCombatForWorldTransition();RetireWorldLootReceipts(oldEpoch);
            chapterPlan=ChapterRoomGeometry.Plan(ActiveChapterNode,ChapterRoomIndex,ChapterSeed);DungeonLayout=chapterPlan.Layout;
            world=WorldBuilder.Build(ZoneKind.Dungeon,DungeonLayout,Progression.HighestAdventureTier,CurrentHub,ChapterSeed);
            Player.Teleport(chapterPlan.Entrance);Camera.main.GetComponent<AdventureCamera>().Snap();chapterObjective=null;
            BeginChapterRoom();UpdateTimeScale();Notify(ChapterObjectiveStatus);return true;
            }
            catch(System.Exception error){FailChapter("章节生成失败："+error.Message);return false;}
            finally {changingZone=false;}
        }
    }
}
