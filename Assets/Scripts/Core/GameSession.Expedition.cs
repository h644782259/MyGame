using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        public RunChoices RunChoices { get; private set; } = new RunChoices();
        public bool DungeonSelectionOpen { get; private set; }
        public int SelectedDungeonTier { get; set; } = 1;
        public int MaximumDungeonTier { get { return Mathf.Clamp(Progression.Profile.bestFloor + 1, 1, 100); } }
        public bool SelectedChallengeMode { get; set; }
        public bool ChallengeRun { get; private set; }
        public int HealingCharges { get; private set; }
        public int DungeonLayout { get; private set; }
        public string LastRunSummary { get; private set; } = "";
        public bool IsInCamp { get { return HasStarted && !InDungeon && !IsDead && Player != null && Vector3.Distance(Player.transform.position, new Vector3(0,0,-10)) < 7f; } }
        public bool SideEventAvailable { get { return InDungeon && !DungeonCleared && !sideEventStarted && Player != null && Vector3.Distance(Player.transform.position, sideEventPosition) < 3.5f; } }
        private int runSeed, wavePopulation;
        private readonly Queue<EncounterSpawn> reinforcementQueue=new Queue<EncounterSpawn>();
        private float nextReinforcementAt;
        private readonly Dictionary<string, int> combatActions = new Dictionary<string, int>();
        private readonly HashSet<EnemyController> sideEventEnemies = new HashSet<EnemyController>();
        private readonly Vector3 sideEventPosition = new Vector3(12,0,-3);
        private string lastDamageSource = "未记录";
        private float lastDamageAmount, lastInterruptAt = -10;
        private bool objectiveHealedThisWave, sideEventStarted;
        private GameObject sideCrystal;

        public bool HasBlessing(RunBlessing blessing) { return InDungeon && RunChoices.Has(blessing); }
        public void CancelDungeonSelection() { DungeonSelectionOpen = false; UpdateTimeScale(); }
        public void ConfirmDungeonSelection()
        {
            if (!DungeonSelectionOpen || InDungeon || IsDead || !NearPortal()) return;
            SelectedDungeonTier = Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            DungeonSelectionOpen = false;
            ChallengeRun = SelectedChallengeMode;
            ChangeZone(true);
            UpdateTimeScale();
            Notify("沉星遗迹 · " + DungeonTier + " 阶 · " + (DungeonLayout == 0 ? "双廊" : "断柱") + (ChallengeRun ? " · 限疗挑战" : " · 普通模式"));
        }

        private void ResetExpedition(bool dungeon)
        {
            RunChoices.Reset(); reinforcementQueue.Clear(); nextReinforcementAt=0; DungeonSelectionOpen = false; sideEventEnemies.Clear(); sideEventStarted = false; sideCrystal = null;
            if (dungeon)
            {
                runSeed = Random.Range(0, 1000000);
                DungeonLayout = runSeed % 2;
                HealingCharges = 3;
                combatActions.Clear(); lastDamageSource = "未记录"; lastDamageAmount = 0; lastInterruptAt = -10;
            }
            else { ChallengeRun = false; HealingCharges = 0; }
        }

        private void TrySpawnReinforcements()
        {
            if(!InDungeon || IsDead || reinforcementQueue.Count==0 || Time.time<nextReinforcementAt || Enemies.Count>6)return;
            nextReinforcementAt=Time.time+2f;
            int spawned=0;
            while(reinforcementQueue.Count>0 && spawned<4 && Enemies.Count<EncounterPlan.MaximumSimultaneous)
            {
                EncounterSpawn next=reinforcementQueue.Peek(); Vector3 position;
                if(!TrySafeSpawn(new Vector3(next.X,0,next.Z),next.Kind==EnemyKind.Guardian?.65f:.5f,7f,out position))break;
                reinforcementQueue.Dequeue(); SpawnEnemy(next.Kind,Mathf.Clamp(Progression.Profile.level+DungeonTier-1,2,100),position,false); spawned++;
            }
            if(spawned>0)Notify("遗迹援军接近 · "+spawned+" 名");
        }

        public bool ConfirmBlessing(int index)
        {
            if (!InDungeon || IsDead || DungeonCleared || Enemies.Count > 0 || reinforcementQueue.Count>0 || !RunChoices.Choose(index)) return false;
            DungeonWave++; SpawnDungeonWave(); UpdateTimeScale();
            Notify(DungeonWave == TotalWaves ? "最终波 · 星蚀巨像" : "第 " + DungeonWave + " 波");
            return true;
        }

        public bool TrySpendHealingCharge()
        {
            if (!InDungeon || !ChallengeRun) return true;
            if (HealingCharges <= 0) { Notify("治疗充能已耗尽；击败守卫完成目标或推进波次可补充。"); return false; }
            HealingCharges--; return true;
        }

        public void RecordCombatAction(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            int tutorialBit=key=="普攻回能"?1:key=="完美闪避"?2:key=="职业能力"?4:key=="换装"?8:0;
            if(tutorialBit!=0&&(Progression.Profile.tutorialMask&tutorialBit)==0) { Progression.Profile.tutorialMask|=tutorialBit; Progression.Save(); LogSystem("实战试炼完成一项 · "+key); }
            int count; combatActions.TryGetValue(key, out count); combatActions[key] = Mathf.Min(9999, count + 1);
        }
        public void RecordIncomingDamage(string source, float amount)
        {
            if (amount <= 0) return;
            lastDamageSource = source; lastDamageAmount = amount;
            // Death may be signalled inside TakeDamage before this telemetry callback.
            if (IsDead) LastRunSummary = BuildRunSummary(false);
        }
        public void OnEnemyInterrupted(EnemyController enemy)
        {
            if (enemy == null || enemy.IsBoss || Player == null) return;
            RecordCombatAction("打断");
            if (HasBlessing(RunBlessing.InterruptFlow) && Time.time - lastInterruptAt >= 1f)
            {
                lastInterruptAt = Time.time; Player.RestoreSkillEnergy(12); Notify("断势回流 · +12 能量");
            }
        }

        private void OnExpeditionEnemyKilled(EnemyController enemy)
        {
            if (enemy.StatusEffects != null && enemy.StatusEffects.IsMarked) Player.OnMarkedEnemyKilled();
            if (!InDungeon) return;
            if (!objectiveHealedThisWave && enemy.Kind == EnemyKind.Guardian)
            {
                objectiveHealedThisWave = true;
                if (ChallengeRun) HealingCharges = Mathf.Min(3, HealingCharges + 1);
                if (HasBlessing(RunBlessing.ExecutionMend)) Player.Heal(Player.MaxHealth * .12f);
            }
            if (sideEventEnemies.Remove(enemy) && sideEventEnemies.Count == 0)
            {
                Progression.Profile.mechanicMaterials = Mathf.Min(999999, Progression.Profile.mechanicMaterials + 1);
                if (ChallengeRun) HealingCharges = Mathf.Min(3, HealingCharges + 1); else Player.Heal(Player.MaxHealth * .15f);
                RecordCombatAction("支线"); Progression.Save(); LogSystem("供能晶核已净化 · 星烬碎片 +1 · 补给恢复");
            }
        }

        private void BuildSideEvent()
        {
            sideCrystal = WorldBuilder.MakeLootBeacon(sideEventPosition, new Color(.33f,.85f,1));
            sideCrystal.name = "Optional power crystal"; transientObjects.Add(sideCrystal);
        }
        public bool StartSideEvent()
        {
            if (!SideEventAvailable || InputBlocked) return false;
            if (Enemies.Count > EncounterPlan.MaximumSimultaneous-2) { Notify("先清理部分敌人，再唤醒晶核；最多14名敌人同时在场。"); return false; }
            Vector3 guardPosition, wispPosition;
            if (!TrySafeSpawn(sideEventPosition+new Vector3(-2,0,2),.65f,5.5f,out guardPosition) ||
                !TrySafeSpawn(sideEventPosition+new Vector3(-1,0,6),.5f,5.5f,out wispPosition) ||
                Vector3.Distance(guardPosition,wispPosition)<2.5f)
            { Notify("晶核附近暂时没有安全来袭位置，请拉开距离后重试。"); return false; }
            sideEventStarted = true;
            if (sideCrystal != null) { Destroy(sideCrystal); sideCrystal = null; }
            int level = Mathf.Clamp(Progression.Profile.level + DungeonTier, 2, 100);
            SpawnEnemy(EnemyKind.Guardian, level, guardPosition, false);
            sideEventEnemies.Add(Enemies[Enemies.Count - 1]);
            SpawnEnemy(EnemyKind.Wisp, level, wispPosition, false);
            sideEventEnemies.Add(Enemies[Enemies.Count - 1]);
            Notify("晶核守卫来袭 · 击败两名守卫获得碎片与补给"); return true;
        }

        private string BuildRunSummary(bool won)
        {
            var entries = new List<string>();
            foreach (var entry in combatActions) entries.Add(entry.Key + " " + entry.Value);
            string blessings = "";
            foreach (RunBlessing blessing in RunChoices.Active) blessings += (blessings.Length == 0 ? "" : "、") + Emberfall.RunChoices.Name(blessing);
            string mechanics = "";
            foreach (EquipmentMechanic mechanic in BuildCatalog.MechanicsFor(Progression.Profile.heroClass))
                if (Progression.HasMechanic(mechanic)) mechanics += (mechanics.Length == 0 ? "" : "、") + BuildCatalog.MechanicName(mechanic);
            return (won ? "通关 · 种子 " + runSeed : "最后受击：" + lastDamageSource + " · " + Mathf.CeilToInt(lastDamageAmount)) +
                "\n成功操作：" + (entries.Count == 0 ? "尚无记录" : string.Join(" · ", entries.ToArray())) +
                "\n机制装备：" + (mechanics.Length == 0 ? "未装备" : mechanics) + "\n祝福：" + (blessings.Length == 0 ? "无" : blessings) +
                "\n星烬碎片 " + Progression.Profile.mechanicMaterials + "/" + ProgressionService.MechanicExchangeCost +
                (won ? " · 下次可自由选择已通关阶数或挑战下一阶" : "\n再试建议：先以守卫蓄力练习侧向闪现，再从侧后方输出；可降低到已通关阶数。");
        }
    }
}
