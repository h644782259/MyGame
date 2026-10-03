using System;
using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        public bool PracticeActive { get { return practiceOwner!=null; } }
        public CampPracticeRecord PracticeRecord { get; private set; }
        public CampPracticeRecord PreviousPracticeRecord { get; private set; }
        private ProgressionService.BuildDraft practiceDraft;
        private ProgressionService practiceOwner;
        private PlayerController practiceOriginalPlayer;
        private List<EnemyController> practiceOriginalEnemies;
        private readonly HashSet<GameObject> practiceOriginalRoots=new HashSet<GameObject>();
        private readonly List<GameObject> practiceSuspendedRoots=new List<GameObject>();
        private UnityEngine.Random.State practiceRandom;
        private EnemyController practiceSupplier;
        private bool practiceBusy, practiceOriginalUI, practiceOriginalPaused;
        public bool BeginPractice(CampPracticeScenario scenario,int seconds,ProgressionService.BuildDraft draft=null)
        {
            if(practiceBusy||PracticeActive||!IsInCamp||IsDead||seconds!=10&&seconds!=60||(int)scenario<0||(int)scenario>2)return false;
            try{if(!PracticeEntrySafe())return false;}catch(Exception exception){Debug.LogException(exception);return false;}
            var charging=Player.GetComponent<SkillChargeController>();if(charging!=null&&charging.IsCharging)return false;
            ProgressionService copy=draft==null?Progression.CreatePracticeCopy():draft.CreatePracticeCopy();
            if(copy==null||!copy.IsPracticeOnly)return false;
            practiceBusy=true;
            try
            {
                practiceOriginalRoots.Clear();practiceSuspendedRoots.Clear();
                GameObject[] originalRoots=gameObject.scene.GetRootGameObjects();
                foreach(GameObject root in originalRoots)practiceOriginalRoots.Add(root);
                PreviousPracticeRecord=PracticeRecord;
                PracticeRecord=new CampPracticeRecord(scenario,seconds,JsonUtility.ToJson(copy.Profile,true),configurationSummary:copy.PracticeConfigurationSummary());
                practiceDraft=draft;practiceOwner=Progression;practiceOriginalPlayer=Player;practiceOriginalEnemies=Enemies;
                practiceOriginalUI=uiBlocking;practiceOriginalPaused=Paused;practiceRandom=UnityEngine.Random.state;
                foreach(GameObject root in originalRoots)
                {
                    if(root==gameObject||root==world||root.GetComponentInChildren<Camera>()!=null||root.GetComponentInChildren<Light>()!=null||!root.activeSelf)continue;
                    practiceSuspendedRoots.Add(root);root.SetActive(false);
                }
                Progression=copy;Enemies=new List<EnemyController>();Paused=false;uiBlocking=false;UnityEngine.Random.InitState(PracticeRecord.Seed);
                var hero=new GameObject("营地试招 · 临时角色");Player=hero.AddComponent<PlayerController>();Player.Initialize(this,copy.Profile.heroClass);Player.Teleport(new Vector3(0,0,-10));
                SpawnEnemy(EnemyKind.Guardian,copy.Profile.level,new Vector3(0,0,-6),false);
                Enemies[0].ConfigurePracticeTarget();
                if(scenario==CampPracticeScenario.FrontAndSupplier){SpawnEnemy(EnemyKind.Wisp,copy.Profile.level,new Vector3(0,0,-2),false);practiceSupplier=Enemies[1];practiceSupplier.ConfigurePracticeTarget();}
                if(ui!=null)ui.EnterPracticePanel();UpdateTimeScale();return true;
            }
            catch(Exception exception){Debug.LogException(exception);EndPractice("试招异常中止 · 已恢复原角色");return false;}
            finally{practiceBusy=false;}
        }
        private bool PracticeEntrySafe()
        {
            // Suspending an engaged enemy invokes OnDisable/CancelAttack. Refuse
            // before any roots change so practice cannot erase a real attack.
            foreach(var enemy in Enemies)
                if(enemy!=null&&enemy.gameObject.activeInHierarchy&&!enemy.IsDead&&
                    (enemy.IsAggro||enemy.IsPreparingAttack||(enemy.transform.position-Player.transform.position).sqrMagnitude<=144f))
                {Notify("附近有敌人或仍在交战；请先脱离追击、结束预警后再试招。");return false;}
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root.activeSelf&&root.GetComponentInChildren<CombatProjectile>()!=null)
                {Notify("场上仍有飞行弹体；请等战斗结束后再试招。");return false;}
            return true;
        }
        public bool RestartPractice()
        {
            if(!PracticeActive||practiceBusy)return false;
            var scenario=PracticeRecord.Scenario;int duration=PracticeRecord.Duration;var draft=practiceDraft;
            EndPractice("练习刷新 · 前一记录作废");return BeginPractice(scenario,duration,draft);
        }
        public void RecordPracticeEnergy(float delta){if(PracticeActive)PracticeRecord.Energy(delta);}
        public void RecordPracticeCast(int castId,int skill){if(PracticeActive)PracticeRecord.Cast(castId,skill);}
        public void RecordPracticeSkillHit(int castId){if(PracticeActive)PracticeRecord.Hit(castId);}
        private void TickPractice()
        {
            if(Input.GetKeyDown(KeyCode.H)){EndPractice("主动离开 · 记录提前结束");return;}
            if(InputBlocked)return;
            try{PracticeRecord.Advance(Time.deltaTime);if(PracticeRecord.Finished)EndPractice(PracticeRecord.EndReason);}
            catch(Exception exception){Debug.LogException(exception);EndPractice("试招异常中止");}
        }
        public bool MovePracticeTarget(EnemyController enemy)
        {
            if(!PracticeActive||enemy==null||!Enemies.Contains(enemy))return false;
            if(PracticeRecord.Scenario==CampPracticeScenario.Moving)
            {
                Vector3 desired=new Vector3(Mathf.Sin(PracticeRecord.Elapsed*1.4f)*3,0,-5);
                var status=enemy.StatusEffects;
                if(!enemy.IsStunned&&(status==null||!status.IsFrozen&&!status.KnockedDown&&!status.IsAirborne))enemy.transform.position=WorldTraversal.Move(enemy.transform.position,Vector3.ClampMagnitude(desired-enemy.transform.position,2.5f*(status==null?1:status.MoveMultiplier)*Time.deltaTime),enemy.NavigationRadius);
            }
            // Practice targets are real damageable combat actors, but do not issue attacks.
            return true;
        }
        public float PracticeSupportMultiplier(EnemyController enemy)
        {
            return PracticeActive&&PracticeRecord.Scenario==CampPracticeScenario.FrontAndSupplier&&practiceSupplier!=null&&!practiceSupplier.IsDead&&enemy!=practiceSupplier&&Enemies.Contains(enemy)&&
                (enemy.transform.position-practiceSupplier.transform.position).sqrMagnitude<=36&&WorldTraversal.HasLineOfSight(enemy.transform.position,practiceSupplier.transform.position)?.7f:1f;
        }
        public void EndPractice(string reason)
        {
            if(!PracticeActive)return;
            PracticeRecord.Finish(reason);
            // Restore ownership even if Unity destruction/reactivation raises an exception.
            try
            {
                foreach(GameObject root in gameObject.scene.GetRootGameObjects())if(!practiceOriginalRoots.Contains(root)){root.SetActive(false);Destroy(root);}
            }
            finally
            {
                Progression=practiceOwner;Player=practiceOriginalPlayer;Enemies=practiceOriginalEnemies;
                practiceOwner=null;practiceOriginalPlayer=null;practiceOriginalEnemies=null;practiceSupplier=null;
                UnityEngine.Random.state=practiceRandom;Paused=practiceOriginalPaused;uiBlocking=practiceOriginalUI;IsDead=false;
                foreach(GameObject root in practiceSuspendedRoots)if(root!=null)try{root.SetActive(true);}catch(Exception exception){Debug.LogException(exception);}
                practiceOriginalRoots.Clear();practiceSuspendedRoots.Clear();if(ui!=null)ui.LeavePracticePanel();UpdateTimeScale();
            }
            Notify("试招结束 · 原角色生命、能量、冷却与存档未改变");
        }
    }
}
