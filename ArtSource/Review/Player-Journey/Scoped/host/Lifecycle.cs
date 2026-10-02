using System.Collections.Generic;using UnityEngine;namespace Emberfall{public sealed partial class GameSession{private bool ContinueAdventure(string slotId, bool discardUnsaved = false, bool alreadySaved = false)
        {
            string targetId = slotId ?? Progression.CurrentSlotId ?? "legacy";
            ProgressionService candidate;
            string error;
            if (!SaveSlotTransition.TryStage(Progression, targetId, out candidate, out error))
            { SaveLoadError = error; Notify("存档未能读取：" + error); return false; }
            if (HasStarted && !discardUnsaved && !alreadySaved)
            {
                if (!SaveBeforeLeaving()) { SaveLoadError = Progression.LastError; return false; }
                // Saving and reloading the same role must read the newly saved
                // snapshot, never the preflight's older in-memory version.
                if (targetId == Progression.CurrentSlotId && !SaveSlotTransition.TryStage(Progression, targetId, out candidate, out error))
                { SaveLoadError = error; return false; }
            }
            string loadWarning = candidate.LastError;
            // No old-world callbacks may write into the newly selected character.
            DiscardTransientAdventureForLoad();
            Progression.Changed -= OnProgressChanged;
            Progression.LeveledUp -= OnLevelUp;
            ProgressionService previous = Progression;
            Progression = candidate;
            DiscardForeignSideEventRewards();
            if (ui != null) ui.RebindProgressionNotifications(previous, candidate);
            Progression.Changed += OnProgressChanged;
            Progression.LeveledUp += OnLevelUp;
            autosaveTimer = 0;
            lifecycleSave.Observe(false, false, () => true);
            SaveLoadError = null;
            loadingSaveSnapshot = true;
            try { BeginAdventure(); }
            catch (System.Exception exception)
            {
                DiscardTransientAdventureForLoad();
                SaveLoadError = "营地加载失败，角色存档仍保留。请返回列表重试：" + exception.Message;
                Notify(SaveLoadError);
                return false;
            }
            finally { loadingSaveSnapshot = false; }
            Notify(string.IsNullOrEmpty(loadWarning) ? "已读取「" + GameBalance.ClassName(Progression.Profile.heroClass) +
                " · " + Progression.Profile.level + "级」，返回营地。" : loadWarning);
            return true;
        }public bool LoadSaveFromPause(string slotId, bool discardUnsaved, bool alreadySaved = false)
        {
            if (!HasStarted || !Paused) { SaveLoadError = "请先暂停当前冒险。"; return false; }
            return ContinueAdventure(slotId, discardUnsaved, alreadySaved);
        }private bool ChangeZone(bool dungeon)
        {
            // Preserve the old adventure before destroying anything. Staged load
            // and committed hub travel already supply a durable target snapshot.
            if (!loadingSaveSnapshot && !enteringChapter && !SaveBeforeLeaving()) return false;
            if(!dungeon&&InDungeon&&!DungeonCleared&&!IsDead&&!ModeFinished)
            {
                if(RoomChainRun!=null)RoomChainRun.Fail(RoomFailureReason.Abandoned);
                if(ModeRun!=null)ModeRun.Fail(ExpeditionModeFailure.Abandoned);
                LastRunSummary=BuildRunSummary(false, "Abandoned");
            }
            changingZone = true;
            int previousCombatEpoch = Player.CombatEpoch;
            if (waveRoutine != null) { StopCoroutine(waveRoutine); waveRoutine = null; }
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects) if (obj != null) { obj.SetActive(false); Destroy(obj); }
            transientObjects.Clear();
            if (world != null) { world.SetActive(false); Destroy(world); }
            Player.RetireCombatForWorldTransition();
            RetireWorldLootReceipts(previousCombatEpoch);
            InDungeon = dungeon;
            DungeonCleared = false;
            DungeonWave = 0;
            DungeonTier = enteringChapter ? chapterReceipt.Tier : Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            ResetExpedition(dungeon);
            if(ChapterActive){chapterPlan=ChapterRoomGeometry.Plan(ActiveChapterNode,ChapterRoomIndex,ChapterSeed);DungeonLayout=chapterPlan.Layout;}
            world = WorldBuilder.Build(dungeon ? ZoneKind.Dungeon : ZoneKind.Wilderness, DungeonLayout, Progression.HighestAdventureTier,CurrentHub,ChapterSeed);
            if(!dungeon)WorldBuilder.ApplyChapterLandmark(world,Progression.Profile.chapterCompletedMask);
            Player.Teleport(ChapterActive?chapterPlan.Entrance:dungeon&&RoomChainRun!=null?TacticalRoomGeometry.Entrance:new Vector3(0,0,dungeon?-9:-10));
            Player.RefreshStats(true);
            if(dungeon)Player.ResetCooldownsForDungeonEntry();
            Camera.main.GetComponent<AdventureCamera>().Snap();
            if (dungeon)
            {
                DungeonWave = 1;
                if(ChapterActive)BeginChapterRoom();else if(RoomChainRun!=null)BeginRoomChainScene();else if(ModeRun!=null)BeginArenaScene();else {SpawnDungeonWave();BuildSideEvent();}
            }
            else
            {
                if(CurrentHub==0)for (int i = 0; i < Mathf.Min(20,12+Progression.Profile.level/8); i++) SpawnWildernessEnemy();
                respawnTimer = 8;
            }
            changingZone = false;
            // Reset removed terminal chapter/mode gates. Reconcile only after the
            // saved transition and new world are complete, preserving every other pause gate.
            UpdateTimeScale();
            return true;
        }public bool SaveBeforeLeaving()
        {
            if (!HasStarted) return true;
            if(!TrySettleSideEventRewards())return false;
            if(DungeonRewardPending&&!TrySettleDungeonReward())return false;
            if(ModeRewardPending&&!TrySettleArenaReward())return false;
            if (!PreserveWorldLoot()) return false;
            // Settlement callbacks can change the live profile. The service checks
            // the complete document and skips only an already durable snapshot.
            Progression.Save();
            if (!string.IsNullOrEmpty(Progression.LastError)) { Notify(Progression.LastError); return false; }
            return true;
        }private void SpawnEnemy(EnemyKind kind, int level, Vector3 position, bool boss)
        {
            GameObject go = new GameObject(boss ? "Sentinel · Boss" : "Enemy · " + kind);
            go.transform.position = WorldTraversal.NearestWalkable(position, boss ? 1f : .45f);
            EnemyController enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(this, kind, level, boss);
            Enemies.Add(enemy);
        }public void OnEnemyKilled(EnemyController enemy)
        {
            if (enemy == null || !AdventureResultPolicy.AcceptsKill(HasStarted,CombatEnded,Enemies.Contains(enemy))) return;
            int chapterExperience=0;bool chapterKill=ChapterActive;
            if(chapterKill&&!RecordChapterDefeat(enemy,out chapterExperience))return;
            if(!Enemies.Remove(enemy))return;
            Vector3 position = enemy.transform.position;
            bool boss = enemy.IsBoss;
            OnExpeditionEnemyKilled(enemy);
            RecordArenaDefeat(enemy);
            RecordRoomDefeat(enemy);
            int level = Progression.Profile.level;
            int experience = boss ? 100 + level * 12 : (InDungeon ? 22 : 16) + level * 2;
            int gold = boss ? 85 + DungeonTier * 20 : Random.Range(7, 15) + level;
            if(InDungeon&&!boss) { float share=Mathf.Clamp(6f/Mathf.Max(6,wavePopulation),.5f,1f);experience=Mathf.RoundToInt(experience*share);gold=Mathf.Max(1,Mathf.RoundToInt(gold*share)); }
            if(chapterKill)experience=chapterExperience;
            Progression.GrantEnemyKillReward(gold, experience);
            LogSystem("+" + gold + " 金币 · +" + experience + " 经验");
            SpawnFloatingText(position + Vector3.up * 2, "+" + experience + " XP  +" + gold + " G", new Color(.95f, .83f, .4f));
            if (boss || Random.value < (InDungeon ? .7f : .5f))
            {
                ItemData loot = Progression.RollLoot(Progression.Profile.level + (boss ? 1 : 0), boss, InDungeon ? DungeonTier : 0);
                DeliverEnemyLoot(loot, position);
            }
            enemy.BeginDeath();
            transientObjects.RemoveAll(go => go == null);
            transientObjects.Add(enemy.gameObject);
            // Capture real callback mutations; unchanged rewards do not rotate backups.
            Progression.Save();
            if(ChapterActive)FinalizeChapterBoss();
            if(RoomChainRun!=null)FinalizeRoomChain();
            if(ModeRun!=null)FinalizeArenaResult();
            if (InDungeon && !ChapterActive && ModeRun==null && RoomChainRun==null && !changingZone && Enemies.Count == 0 && !DungeonCleared)
            {
                TrySpawnReinforcements();
                if(Enemies.Count==0 && reinforcementQueue.Count==0) waveRoutine=StartCoroutine(NextWave());
            }
        }public void OnPlayerDied()
        {
            if (IsDead) return;
            IsDead = true;
            if(ModeRun!=null)ModeRun.Fail(ExpeditionModeFailure.PlayerDefeated);
            if(RoomChainRun!=null)RoomChainRun.Fail(RoomFailureReason.Death);
            if(ChapterActive)FailChapter("角色倒下：本次章节挑战失败。");
            pendingRoomChoice.Cancel();
            DungeonSelectionOpen = false;
            LastRunSummary = BuildRunSummary(false);
            GameAudio.Play(SoundCue.Death);
            int lost = Mathf.FloorToInt(Progression.Profile.gold * .1f);
            Progression.AddGold(-lost);
            RecordRecapGoldLoss(lost);
            Progression.Save();
            Notify("你暂时倒下了，损失 " + lost + " 金币。装备、经验与技能均已保留。");
            UpdateTimeScale();
        }public void ReturnToCamp()
        {
            if (!HasStarted || IsDead) return;
            if (!DungeonCleared && !ModeFinished)
            {
                foreach (EnemyController enemy in Enemies)
                    if (enemy != null && !enemy.IsDead && Vector3.Distance(enemy.transform.position, Player.transform.position) < 6f)
                    { Notify("附近有敌人！拉开至少 6 米距离后可按 H 返回营地。"); return; }
            }
            bool abandoned = InDungeon && !DungeonCleared;
            if (!ChangeZone(false)) return;
            Notify(abandoned ? "已撤离遗迹，生命恢复。随时可以重新挑战。" : "已回到营地，生命已恢复。按 I 整理战利品，按 K 学习技能。");
        }private void UpdateTimeScale() { Time.timeScale = pauseState.CanAdvance(HasStarted, Paused, uiBlocking || RunChoices.AwaitingChoice || DungeonSelectionOpen || ModeFinished, IsDead) ? 1 : 0; }private void BeginAdventure()
        {
            HasStarted = true;
            IsDead = false;
            Paused = false;
            uiBlocking = false;
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); }
            GameObject hero = new GameObject("Hero");
            Player = hero.AddComponent<PlayerController>();
            Player.Initialize(this, Progression.Profile.heroClass);
            equipmentFingerprint=Progression.Profile.weaponId+"|"+Progression.Profile.armorId+"|"+Progression.Profile.relicId;
            ChangeZone(false);
            UpdateTimeScale();
        }private void DiscardTransientAdventureForLoad()
        {
            // Only after staging succeeded and the user chose how to handle the
            // current progress. This path intentionally never saves/settles loot.
            HasStarted = false;
            Paused = true;
            uiBlocking = true;
            changingZone = true;
            StopAllCoroutines(); waveRoutine = null;
            foreach (PendingLoot loot in pendingLoot.Values)
                if (loot.Pickup != null) loot.Pickup.Retire();
            pendingLoot.Clear(); collectedGroundLoot.Clear();
            foreach (EnemyController enemy in Enemies)
                if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects)
                if (obj != null) { obj.SetActive(false); Destroy(obj); }
            transientObjects.Clear();
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); Player = null; }
            if (world != null) { world.SetActive(false); Destroy(world); world = null; }
            // In particular, unclaimed old-mode rewards cannot settle into target.
            ResetExpedition(false);
            InDungeon = false; DungeonCleared = false; IsDead = false;
            equipmentFingerprint = null;
            changingZone = false;
            UpdateTimeScale();
        }public void Respawn()
        {
            if (!IsDead) return;
            if (!PreserveWorldLoot()) return;
            if (!ChangeZone(false)) return;
            IsDead = false;
            Paused = false;
            uiBlocking = false;
            UpdateTimeScale();
            Notify("已在营地复苏。生命恢复，可在背包补充药水。");
        }public bool QuitToTitle(bool alreadySaved = false)
        {
            if (!alreadySaved && !SaveBeforeLeaving()) return false;
            SuspendInputs();
            StopAllCoroutines();
            waveRoutine = null;
            HasStarted = false;
            Paused = false;
            uiBlocking = false;
            IsDead = false;
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects) if (obj != null) Destroy(obj);
            transientObjects.Clear();
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); Player = null; }
            if (world != null) { world.SetActive(false); Destroy(world); }
            InDungeon = false;
            ResetExpedition(false);
            DungeonCleared = false;
            world = WorldBuilder.Build(ZoneKind.Wilderness);
            Camera.main.GetComponent<AdventureCamera>().Snap();
            UpdateTimeScale();
            return true;
        }private static bool LiveRoomEnemy(EnemyController enemy)
        {return enemy!=null&&!enemy.IsDead&&enemy.isActiveAndEnabled&&enemy.gameObject.activeInHierarchy;}private void ResetExpedition(bool dungeon)
        {
            ClearDungeonSettlement();
            if(!enteringChapter)ResetChapterRun();
            MechanismEvidence.Reset();RunChoices.Reset(); reinforcementQueue.Clear(); nextReinforcementAt=0; DungeonSelectionOpen = false; AbandonSideEvent();
            runDamageTaken=runHealingReceived=0;
            combatActions.Clear(); lastDamageSource = "未记录"; lastDamageAmount = 0; lastInterruptAt = -10; recapGoldLost = 0;
            if (dungeon)
            {
                DungeonEntryLevel = Mathf.Clamp(Progression.Profile.level,2,100);
                runSeed = Random.Range(0, 1000000);
                DungeonLayout = runSeed % 2;
                HealingCharges = 3;

            }
            else { ChallengeRun = false; HealingCharges = 0; }
            ResetArenaMode(dungeon);
        }public bool ModeFinished {get{return ChapterActive?ChapterFinished:RoomChainRun!=null?RoomChainRun.Finished:ModeRun!=null&&ModeRun.IsTerminal&&ModeRun.Status!=ExpeditionModeStatus.Disposed;}}}}