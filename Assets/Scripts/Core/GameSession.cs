using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public ProgressionService Progression { get; private set; }
        public PlayerController Player { get; private set; }
        public List<EnemyController> Enemies { get; private set; } = new List<EnemyController>();
        public bool HasStarted { get; private set; }
        public bool Paused { get; private set; }
        public bool InDungeon { get; private set; }
        public bool IsDead { get; private set; }
        public int DungeonWave { get; private set; }
        public int DungeonTier { get; private set; } = 1;
        public int TotalWaves { get { return 3; } }
        public float ArenaRadius { get { return InDungeon ? 18f : 22f; } }
        public bool DungeonCleared { get; private set; }
        public int PendingLootCount { get { return pendingLoot.Count; } }
        public string ZoneName { get { return InDungeon ? "沉星遗迹 · 第 " + DungeonTier + " 阶" : "风语原野"; } }
        public string Notification
        {
            get
            {
                if (Progression != null && !string.IsNullOrEmpty(Progression.LastError) && Progression.LastError.StartsWith("保存失败"))
                    return Progression.LastError;
                return Time.unscaledTime < notificationUntil ? notification : "";
            }
        }
        public bool InputBlocked { get { return !HasStarted || Paused || uiBlocking || IsDead || RunChoices.AwaitingChoice || DungeonSelectionOpen || pauseState.BackgroundPaused; } }
        public bool PointerOverUI { get { return ui != null && ui.IsPointerOverUI; } }
        public bool CanChangeLoadout { get { return HasStarted && !IsDead; } }
        public string Objective
        {
            get
            {
                if (!HasStarted) return "踏入星烬纪元";
                if (IsDead) return "旅途尚未结束 · 返回营地重整旗鼓";
                if (InDungeon) return DungeonCleared ? "遗迹已肃清 · 收集地面战利品 · 按 T 返回" :
                    "肃清遗迹 " + DungeonWave + "/" + TotalWaves + " · 剩余 " + Enemies.Count + " 个敌人";
                if (Progression.Profile.level < 2) return "击败原野怪物，升至 2 级 · 按 K 查看职业技能";
                if (Progression.Profile.skillRanks[0] == 0) return "你已获得技能点！按 K 学习第一个职业技能";
                if (NearPortal()) return "按 T 进入沉星遗迹 · 三波挑战 / 首领 / 稀有装备";
                return "探索原野收集装备 · 前往北方发光的遗迹传送门";
            }
        }

        private GameObject world;
        private GameUI ui;
        private bool uiBlocking;
        private string equipmentFingerprint;
        private readonly ApplicationPauseState pauseState = new ApplicationPauseState();
        public bool BackgroundPaused { get { return pauseState.BackgroundPaused; } }
        private bool changingZone;
        private float notificationUntil;
        private string notification = "";
        private float respawnTimer;
        private float autosaveTimer;
        private float portalHintTimer;
        private Coroutine waveRoutine;
        private readonly List<GameObject> transientObjects = new List<GameObject>();
        private sealed class PendingLoot
        {
            public ItemData Item;
            public GroundLootPickup Pickup;
            public bool Collecting;
        }
        private readonly Dictionary<string, PendingLoot> pendingLoot = new Dictionary<string, PendingLoot>();
        private readonly HashSet<string> collectedGroundLoot = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_6000_0_OR_NEWER
            if (FindAnyObjectByType<GameSession>() == null) new GameObject("Emberfall · Game").AddComponent<GameSession>();
#else
            if (FindObjectOfType<GameSession>() == null) new GameObject("Emberfall · Game").AddComponent<GameSession>();
#endif
        }

        private void Awake()
        {
            Debug.Log("Emberfall " + Application.version + " · " + Application.platform);
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
#if UNITY_EDITOR
            string validationDirectory = UnityEditor.SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            Progression = new ProgressionService(string.IsNullOrEmpty(validationDirectory) ? null : validationDirectory);
#elif EMBERFALL_VISUAL_VALIDATION
            Progression = new ProgressionService(VisualValidationPlayer.SaveDirectory);
#else
            Progression = new ProgressionService();
#endif
            Application.wantsToQuit += CanQuitSafely;
            Progression.Changed += OnProgressChanged;
            Progression.LeveledUp += OnLevelUp;
            world = WorldBuilder.Build(ZoneKind.Wilderness);
            ConfigureCamera();
            ui = gameObject.AddComponent<GameUI>();
            ui.Initialize(this);
            gameObject.AddComponent<MobileControls>().Initialize(this);
            Time.timeScale = 0f;
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            camera.orthographic = false;
            camera.fieldOfView = 48;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.065f, .1f, .16f);
            camera.transform.position = new Vector3(0, 19, -24);
            camera.transform.rotation = Quaternion.Euler(48f, 0, 0);
            if (camera.GetComponent<AdventureCamera>() == null) camera.gameObject.AddComponent<AdventureCamera>();
        }

        public void StartNew(HeroClass heroClass)
        {
            if (!PreserveWorldLoot()) return;
            if (!Progression.CreateNewSlot(heroClass)) { Notify(Progression.LastError); return; }
            collectedGroundLoot.Clear();
            BeginAdventure();
            Notify("已创建独立存档 · 风语原野");
        }

        public void ContinueGame()
        {
            ContinueAdventure(null);
        }

        public bool ContinueGame(string slotId)
        {
            return ContinueAdventure(slotId);
        }

        private bool ContinueAdventure(string slotId)
        {
            if (!PreserveWorldLoot()) return false;
            bool loaded = slotId == null ? Progression.Load() : Progression.LoadSlot(slotId);
            if (!loaded) { Notify("存档未能读取：" + Progression.LastError); return false; }
            string loadWarning = Progression.LastError;
            collectedGroundLoot.Clear();
            BeginAdventure();
            Notify(string.IsNullOrEmpty(loadWarning) ? "欢迎回来，" + GameBalance.ClassName(Progression.Profile.heroClass) + "。冒险进度已恢复。" : loadWarning);
            return true;
        }

        public bool SaveAsNewSlot()
        {
            if (!HasStarted || IsDead) return false;
            if (!PreserveWorldLoot()) return false;
            bool saved = Progression.SaveAsNewSlot();
            Notify(saved ? "已另存为新存档。原进度保留，后续自动保存到新存档。" : Progression.LastError);
            return saved;
        }

        private void BeginAdventure()
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
        }

        private void Update()
        {
            if (InputBlocked) return;
            if (!InputBlocked)
            {
                if (Input.GetKeyDown(KeyCode.F) || MobileControls.ConsumePotion()) DrinkPotion();
                if (Input.GetKeyDown(KeyCode.T)) { if (InDungeon) { if (DungeonCleared) ReturnToCamp(); else Notify("先击败本轮敌人；按 H 可放弃副本返回营地。"); } else EnterDungeon(); }
                if (Input.GetKeyDown(KeyCode.H)) ReturnToCamp();
            }
            if(InDungeon && reinforcementQueue.Count>0 && Enemies.Count<=6)TrySpawnReinforcements();
            if (!InDungeon)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0 && Enemies.Count < Mathf.Min(22,14+Progression.Profile.level/8))
                {
                    SpawnWildernessEnemy();
                    respawnTimer = 4f;
                }
                portalHintTimer -= Time.deltaTime;
                if (NearPortal() && portalHintTimer <= 0) { Notify("沉星遗迹传送门 · 按 T 开始副本挑战"); portalHintTimer = 16f; }
            }
            autosaveTimer += Time.deltaTime;
            if (autosaveTimer > 25) { autosaveTimer = 0; Progression.Save(); }
        }

        public void SetPaused(bool value) { Paused = value; UpdateTimeScale(); }
        public void SetUIBlocking(bool value) { uiBlocking = value; UpdateTimeScale(); }
        public bool AssignSkill(int hotbarSlot, int skillIndex)
        {
            if (!CanChangeLoadout) { Notify("请先开始冒险，再配置技能快捷栏。"); return false; }
            bool changed = Progression.AssignSkill(hotbarSlot, skillIndex);
            Notify(changed ? "快捷栏已更新 · 技能冷却保留" : Progression.LastError);
            return changed;
        }
        public bool SetHotbarPage(int page)
        {
            return HasStarted && Progression.SetHotbarPage(page);
        }
        public bool MoveHotbarSkill(int sourceSlot, int targetSlot)
        {
            if (!CanChangeLoadout) return false;
            bool changed = Progression.MoveHotbarSkill(sourceSlot, targetSlot);
            if (changed) Notify("快捷栏已更新 · 技能冷却保留");
            else if (!string.IsNullOrEmpty(Progression.LastError)) Notify(Progression.LastError);
            return changed;
        }
        private void UpdateTimeScale() { Time.timeScale = pauseState.CanAdvance(HasStarted, Paused, uiBlocking || RunChoices.AwaitingChoice || DungeonSelectionOpen, IsDead) ? 1 : 0; }

        private bool NearPortal() { return Player != null && Vector3.Distance(Player.transform.position, new Vector3(0, 0, 11)) < 4.3f; }

        public void EnterDungeon()
        {
            if (!HasStarted || IsDead || InDungeon) return;
            if (!Progression.CanEnterDungeon) { Notify("先领取营地恢复栏的装备，并为珍贵战利品腾出位置。"); return; }
            if (Progression.Profile.pendingFashionChest || Progression.Profile.pendingChestReveal) { Notify("请先开启上次通关的宝箱。"); return; }
            if (!NearPortal()) { Notify("请前往原野北方发光的传送门（小地图菱形），靠近后按 T。"); return; }
            if (Progression.Profile.level < 2) { Notify("遗迹需要 2 级。先在原野战斗，并学习第一个职业技能。"); return; }
            DungeonSelectionOpen = true;
            SelectedDungeonTier = Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            UpdateTimeScale();
        }

        public void ReturnToCamp()
        {
            if (!HasStarted || IsDead) return;
            if (!DungeonCleared)
            {
                foreach (EnemyController enemy in Enemies)
                    if (enemy != null && !enemy.IsDead && Vector3.Distance(enemy.transform.position, Player.transform.position) < 6f)
                    { Notify("附近有敌人！拉开至少 6 米距离后可按 H 返回营地。"); return; }
            }
            bool abandoned = InDungeon && !DungeonCleared;
            if (!ChangeZone(false)) return;
            Notify(abandoned ? "已撤离遗迹，生命恢复。随时可以重新挑战。" : "已回到营地，生命已恢复。按 I 整理战利品，按 K 学习技能。");
        }

        private bool ChangeZone(bool dungeon)
        {
            if (!PreserveWorldLoot()) return false;
            changingZone = true;
            if (waveRoutine != null) { StopCoroutine(waveRoutine); waveRoutine = null; }
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            foreach (GameObject obj in transientObjects) if (obj != null) Destroy(obj);
            transientObjects.Clear();
            if (world != null) { world.SetActive(false); Destroy(world); }
            InDungeon = dungeon;
            DungeonCleared = false;
            DungeonWave = 0;
            DungeonTier = Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            ResetExpedition(dungeon);
            world = WorldBuilder.Build(dungeon ? ZoneKind.Dungeon : ZoneKind.Wilderness, DungeonLayout, Progression.Profile.bestFloor);
            Player.Teleport(new Vector3(0, 0, dungeon ? -9 : -10));
            Player.RefreshStats(true);
            Camera.main.GetComponent<AdventureCamera>().Snap();
            if (dungeon)
            {
                DungeonWave = 1;
                SpawnDungeonWave();
                BuildSideEvent();
            }
            else
            {
                for (int i = 0; i < Mathf.Min(20,12+Progression.Profile.level/8); i++) SpawnWildernessEnemy();
                respawnTimer = 8;
            }
            Progression.Save();
            changingZone = false;
            return true;
        }

        private void SpawnWildernessEnemy()
        {
            for(int attempt=0;attempt<20;attempt++)
            {
                Vector3 desired=new Vector3(Random.Range(-16f,16f),0,Random.Range(-3f,16f));
                Vector3 position;
                if(!TrySafeSpawn(desired,.6f,8f,out position))continue;
                EnemyKind kind=position.z>7&&Progression.Profile.level>=3?(Random.value<.4f?EnemyKind.Guardian:EnemyKind.Wisp):
                    position.x< -3?(Random.value<.6f?EnemyKind.Slime:EnemyKind.Goblin):(Random.value<.55f?EnemyKind.Goblin:EnemyKind.Wisp);
                SpawnEnemy(kind,Mathf.Max(1,Progression.Profile.level-1),position,false);return;
            }
        }

        private void SpawnDungeonWave()
        {
            int level = Mathf.Clamp(Progression.Profile.level, 2, 100);
            List<EncounterSpawn> plan=EncounterPlan.Create(DungeonTier,DungeonWave,DungeonLayout,runSeed);
            wavePopulation=plan.Count;
            int initial=0;
            foreach(EncounterSpawn spawn in plan)
            {
                if(DungeonTier>=4 && initial>=8 && !spawn.Boss) { reinforcementQueue.Enqueue(spawn); continue; }
                Vector3 preferred=new Vector3(spawn.X,0,spawn.Z);
                Vector3 position;
                if(TrySafeSpawn(preferred,spawn.Boss?.95f:spawn.Kind==EnemyKind.Guardian?.65f:.5f,5.5f,out position)) { SpawnEnemy(spawn.Kind,level,position,spawn.Boss); initial++; }
            }
            // A wave can never auto-clear into a dead end because all sampled tiles failed.
            if(Enemies.Count==0) SpawnEnemy(EnemyKind.Guardian,level,WorldTraversal.NearestWalkable(new Vector3(0,0,8),1),DungeonWave==TotalWaves);
            objectiveHealedThisWave=false;
        }

        private bool TrySafeSpawn(Vector3 preferred,float radius,float playerDistance,out Vector3 position)
        {
            for(int attempt=0;attempt<48;attempt++)
            {
                Vector3 candidate=attempt==0?preferred:preferred+new Vector3(Mathf.Sin(attempt*2.39996f),0,Mathf.Cos(attempt*2.39996f))*(.5f+attempt*.23f);
                candidate=WorldTraversal.NearestWalkable(candidate,radius);
                if(candidate.magnitude>ArenaRadius-radius-1||!WorldTraversal.IsWalkable(candidate,radius))continue;
                if(Player!=null && Vector3.Distance(candidate,Player.transform.position)<playerDistance)continue;
                // This deliberately requires a clear traversable approach; it rejects
                // unreachable river banks and sealed pockets, not just visible ground.
                if(WorldTraversal.FindPath(candidate,InDungeon?Vector3.zero:new Vector3(0,0,-10),radius).Count==0)continue;
                bool crowded=false;
                foreach(EnemyController enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&Vector3.Distance(candidate,enemy.transform.position)<radius+enemy.NavigationRadius+1.1f){crowded=true;break;}
                if(crowded)continue;
                position=candidate;return true;
            }
            position=Vector3.zero;return false;
        }

        private void SpawnEnemy(EnemyKind kind, int level, Vector3 position, bool boss)
        {
            GameObject go = new GameObject(boss ? "Sentinel · Boss" : "Enemy · " + kind);
            go.transform.position = WorldTraversal.NearestWalkable(position, boss ? 1f : .45f);
            EnemyController enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(this, kind, level, boss);
            Enemies.Add(enemy);
        }

        public void OnEnemyKilled(EnemyController enemy)
        {
            if (enemy == null || !Enemies.Remove(enemy)) return;
            Vector3 position = enemy.transform.position;
            bool boss = enemy.IsBoss;
            OnExpeditionEnemyKilled(enemy);
            int level = Progression.Profile.level;
            int experience = boss ? 100 + level * 12 : (InDungeon ? 22 : 16) + level * 2;
            int gold = boss ? 85 + DungeonTier * 20 : Random.Range(7, 15) + level;
            if(InDungeon&&!boss) { float share=Mathf.Clamp(6f/Mathf.Max(6,wavePopulation),.5f,1f);experience=Mathf.RoundToInt(experience*share);gold=Mathf.Max(1,Mathf.RoundToInt(gold*share)); }
            Progression.Profile.kills++;
            Progression.AddGold(gold);
            Progression.GrantExperience(experience);
            LogSystem("+" + gold + " 金币 · +" + experience + " 经验");
            SpawnFloatingText(position + Vector3.up * 2, "+" + experience + " XP  +" + gold + " G", new Color(.95f, .83f, .4f));
            if (boss || Random.value < (InDungeon ? .7f : .5f))
            {
                ItemData loot = InDungeon ? Progression.RollLoot(Progression.Profile.level + (boss ? 1 : 0), boss)
                    : Progression.CreateLoot(Progression.Profile.level + (boss ? 1 : 0), boss);
                if (loot != null)
                {
                    if (InDungeon) SpawnGroundLoot(loot, position);
                    else
                    {
                        GameAudio.Play(SoundCue.Loot);
                        LogSystem("获得 " + GameBalance.RarityName(loot.rarity) + "装备：「" + loot.name + "」 · 按 I 查看" + (string.IsNullOrEmpty(Progression.LastError) ? "" : " · " + Progression.LastError));
                        SpawnLootBeacon(position, GameBalance.RarityColor(loot.rarity));
                    }
                }
            }
            enemy.BeginDeath();
            transientObjects.RemoveAll(go => go == null);
            transientObjects.Add(enemy.gameObject);
            Progression.Save();
            if (InDungeon && !changingZone && Enemies.Count == 0 && !DungeonCleared)
            {
                TrySpawnReinforcements();
                if(Enemies.Count==0 && reinforcementQueue.Count==0) waveRoutine=StartCoroutine(NextWave());
            }
        }

        private IEnumerator NextWave()
        {
            if (DungeonWave < TotalWaves)
            {
                if (!ChallengeRun) Player.Heal(Player.MaxHealth * .25f);
                else HealingCharges = Mathf.Min(3, HealingCharges + 1);
                RunChoices.Prepare(DungeonWave, Progression.Profile.heroClass, Progression.Profile.skillRanks, runSeed + DungeonWave * 97);
                UpdateTimeScale();
                waveRoutine = null;
                yield break;
            }
            else
            {
                DungeonCleared = true;
                GameAudio.Play(SoundCue.Victory);
                Progression.Profile.clearedRuns++;
                Progression.Profile.bestFloor = Mathf.Max(Progression.Profile.bestFloor, DungeonTier);
                int completionGold = 120 + DungeonTier * 30;
                if (HasBlessing(RunBlessing.RiskContract)) completionGold = Mathf.RoundToInt(completionGold * 1.3f);
                Progression.AddGold(completionGold);
                Progression.GrantExperience(100 + DungeonTier * 20);
                Progression.PrepareDungeonChest();
                LastRunSummary = BuildRunSummary(true);
                Player.Heal(Player.MaxHealth);
                Progression.Save();
                Notify("遗迹通关 · 选择一个宝箱开启，然后拾取战利品");
            }
            waveRoutine = null;
        }

        public void OnPlayerDied()
        {
            if (IsDead) return;
            IsDead = true;
            DungeonSelectionOpen = false;
            LastRunSummary = BuildRunSummary(false);
            GameAudio.Play(SoundCue.Death);
            int lost = Mathf.FloorToInt(Progression.Profile.gold * .1f);
            Progression.AddGold(-lost);
            Progression.Save();
            Notify("你暂时倒下了，损失 " + lost + " 金币。装备、经验与技能均已保留。");
            UpdateTimeScale();
        }

        public void Respawn()
        {
            if (!IsDead) return;
            if (!PreserveWorldLoot()) return;
            if (!ChangeZone(false)) return;
            IsDead = false;
            Paused = false;
            uiBlocking = false;
            UpdateTimeScale();
            Notify("已在营地复苏。生命恢复，可在背包补充药水。");
        }

        public void DrinkPotion()
        {
            if (!HasStarted || IsDead || Player == null) return;
            if (Player.Health >= Player.MaxHealth - .5f) { Notify("生命已满，无需使用药水。"); return; }
            if (ChallengeRun && InDungeon) { if (!TrySpendHealingCharge()) return; }
            else if (!Progression.UsePotion()) { Notify("药水不足，按 I 在背包中购买。"); return; }
            Player.Heal(Player.MaxHealth * .5f);
            SpawnFloatingText(Player.transform.position + Vector3.up * 2, "+50% HP", new Color(.4f, 1, .68f));
            Progression.Save();
        }

        public void UseHotbarConsumable()
        {
            if (InputBlocked || Player == null || Player.TraversalStartedThisFrame) return;
            DrinkPotion();
        }

        public void QuitToTitle()
        {
            if (!PreserveWorldLoot()) return;
            if (HasStarted) Progression.Save();
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
        }

        private void OnProgressChanged()
        {
            if (Player != null && HasStarted)
            {
                Player.RefreshStats(false);
                string now=Progression.Profile.weaponId+"|"+Progression.Profile.armorId+"|"+Progression.Profile.relicId;
                if(equipmentFingerprint!=null && now!=equipmentFingerprint) { equipmentFingerprint=now;RecordCombatAction("换装"); }
                equipmentFingerprint=now;
            }
        }
        private void OnLevelUp(int level)
        {
            GameAudio.Play(SoundCue.LevelUp);
            if (Player != null) { Player.RefreshStats(true); SpawnFloatingText(Player.transform.position + Vector3.up * 3, "LEVEL " + level + "  +1 SP", new Color(.9f, .82f, .4f)); }
            Notify("升至 " + level + " 级！生命恢复，获得 1 技能点 · 按 K 学习或强化技能。");
        }
        public void Notify(string message) { notification = message; notificationUntil = Time.unscaledTime + 6; }

        public void SpawnFloatingText(Vector3 position, string value, Color color)
        {
            if (FloatingNumber.ActiveCount >= 48) return;
            GameObject go = new GameObject("Combat Text");
            go.transform.position = position;
            go.AddComponent<FloatingNumber>().Initialize(value, color);
        }

        private void SpawnLootBeacon(Vector3 position, Color color)
        {
            GameObject beacon = WorldBuilder.MakeLootBeacon(position, color);
            transientObjects.RemoveAll(go => go == null);
            transientObjects.Add(beacon);
            Destroy(beacon, 2.5f);
        }

        public GroundLootPickup SpawnGroundLoot(ItemData item, Vector3 position)
        {
            if (!HasStarted || !InDungeon || item == null || string.IsNullOrWhiteSpace(item.id) ||
                !System.Enum.IsDefined(typeof(ItemSlot), item.slot) || !System.Enum.IsDefined(typeof(Rarity), item.rarity) ||
                collectedGroundLoot.Contains(item.id) || Progression.Profile.inventory.Exists(value => value != null && value.id == item.id)) return null;
            PendingLoot existing;
            if (pendingLoot.TryGetValue(item.id, out existing)) return existing.Pickup;
            position.y = 0;
            position = WorldTraversal.NearestWalkable(position, .35f);
            GameObject root = new GameObject("Ground loot · " + item.name);
            if (world != null) root.transform.SetParent(world.transform, false);
            root.transform.position = position;
            GroundLootPickup pickup = root.AddComponent<GroundLootPickup>();
            pendingLoot.Add(item.id, new PendingLoot { Item = item, Pickup = pickup });
            pickup.Initialize(this, item);
            return pickup;
        }

        public bool TryCollectGroundLoot(string itemId, bool feedback = true)
        {
            PendingLoot pending;
            if (string.IsNullOrEmpty(itemId) || !pendingLoot.TryGetValue(itemId, out pending) || pending.Collecting) return false;
            pending.Collecting = true;
            bool accepted = Progression.CollectLoot(pending.Item);
            pending.Collecting = false;
            if (!accepted) return false;
            pendingLoot.Remove(itemId);
            collectedGroundLoot.Add(itemId);
            if (pending.Pickup != null) pending.Pickup.Retire();
            if (feedback)
            {
                GameAudio.Play(SoundCue.Loot);
                LogSystem("拾取 " + pending.Item.name + (string.IsNullOrEmpty(Progression.LastError) ? "" : " · " + Progression.LastError));
            }
            return true;
        }

        public void CollectRemainingDungeonLoot()
        {
            if (Progression == null || pendingLoot.Count == 0) return;
            var ids = new List<string>(pendingLoot.Keys);
            foreach (string id in ids) TryCollectGroundLoot(id, false);
        }

        private bool CanQuitSafely() { return PreserveWorldLoot(); }
        private bool PreserveWorldLoot()
        {
            if (Progression == null || pendingLoot.Count == 0) return true;
            CollectRemainingDungeonLoot();
            if (pendingLoot.Count == 0) return true;
            var items = new List<ItemData>();
            foreach (PendingLoot loot in pendingLoot.Values) items.Add(loot.Item);
            if (!Progression.PreserveGroundLoot(items)) { Notify(Progression.LastError); return false; }
            foreach (PendingLoot loot in pendingLoot.Values)
            {
                collectedGroundLoot.Add(loot.Item.id);
                if (loot.Pickup != null) loot.Pickup.Retire();
            }
            pendingLoot.Clear();
            LogSystem("珍贵地面装备已保存在营地恢复栏");
            return true;
        }

        private void OnApplicationPause(bool pause)
        {
            pauseState.SetSuspended(pause);
            if (pause) SuspendInputs();
            if (pause && HasStarted) Progression.Save();
            UpdateTimeScale();
        }
        private void OnApplicationFocus(bool focus)
        {
            pauseState.SetFocus(focus);
            if (!focus) SuspendInputs();
            UpdateTimeScale();
        }
        private void SuspendInputs()
        {
            MobileControls.ResetInput();
            if (Player != null)
            {
                var targeting = Player.GetComponent<SkillTargetingController>();
                if (targeting != null) targeting.Cancel();
                var charge = Player.GetComponent<SkillChargeController>();
                if (charge != null) charge.Cancel();
            }
            if (ui != null) ui.CancelBackgroundInput();
        }
        private void OnApplicationQuit() { PreserveWorldLoot(); if (HasStarted) Progression.Save(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Application.wantsToQuit -= CanQuitSafely;
            PreserveWorldLoot();
            if (Progression != null) { Progression.Changed -= OnProgressChanged; Progression.LeveledUp -= OnLevelUp; }
            Instance = null;
            Time.timeScale = 1;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed class AdventureCamera : MonoBehaviour
    {
        public const float MinimumPitch = -18f;
        public const float MaximumPitch = 75f;
        private const float DefaultPitch = 48.36646f;
        private const float DistanceScale = 1.2041595f;
        private static AdventureCamera active;
        private readonly CameraOrbitInput orbitInput = new CameraOrbitInput();
        private int inputFrame = -1;
        private float distance = 19f;
        private float yaw, pitch = DefaultPitch;
        private float smoothYaw, smoothPitch = DefaultPitch, smoothDistance = 19f;
        private Vector3 lookTarget;
        public static bool CancelSkillRequested { get { return !MobileControls.Active && active != null && active.inputFrame == Time.frameCount && active.orbitInput.Clicked; } }
        public static bool IsOrbitDragging { get { return !MobileControls.Active && active != null && active.orbitInput.IsDragging; } }
        public float Pitch { get { return pitch; } }
        public float Yaw { get { return yaw; } }

        private void Awake() { active = this; }
        public void Snap()
        {
            HitFeedback.ClearCamera();
            ResetOrbitInput();
            lookTarget = DesiredTarget();
            MoveCamera(true);
        }

        public static Vector3 CameraRelativeMovement(Vector2 input, Transform view)
        {
            Vector3 forward = view == null ? Vector3.forward : Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < .0001f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        private Vector3 DesiredTarget()
        {
            GameSession game = GameSession.Instance;
            return game != null && game.Player != null && game.HasStarted ? game.Player.transform.position + Vector3.up * .7f : new Vector3(0, 0, 1);
        }
        private void Update()
        {
            GameSession game = GameSession.Instance;
            bool canContinue = !MobileControls.Active && Application.isFocused && game != null && !game.InputBlocked;
            bool canStart = canContinue && !game.PointerOverUI;
            ProcessOrbitInput(Input.mousePosition, Input.GetMouseButtonDown(1), Input.GetMouseButton(1), Input.GetMouseButtonUp(1), canStart, canContinue, Input.mouseScrollDelta.y);
        }

        private void ProcessOrbitInput(Vector2 position, bool pressed, bool held, bool released, bool canStart, bool canContinue, float scroll)
        {
            inputFrame = Time.frameCount;
            orbitInput.Advance(position, pressed, held, released, canStart, canContinue);
            if (orbitInput.DragDelta.sqrMagnitude > 0)
            {
                yaw = Mathf.Repeat(yaw + orbitInput.DragDelta.x * .22f, 360f);
                pitch = Mathf.Clamp(pitch + orbitInput.DragDelta.y * .18f, MinimumPitch, MaximumPitch);
            }
            if (canStart && canContinue) distance = Mathf.Clamp(distance - scroll * 1.5f, 13f, 25f);
        }

        private void LateUpdate() { MoveCamera(false); }
        private void MoveCamera(bool snap)
        {
            Vector3 desired = DesiredTarget();
            float follow = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            float orbit = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            lookTarget = snap ? desired : Vector3.Lerp(lookTarget, desired, follow);
            smoothYaw = snap ? yaw : Mathf.LerpAngle(smoothYaw, yaw, orbit);
            smoothPitch = snap ? pitch : Mathf.Lerp(smoothPitch, pitch, orbit);
            smoothDistance = snap ? distance : Mathf.Lerp(smoothDistance, distance, orbit);
            Vector3 position = lookTarget + Quaternion.Euler(smoothPitch, smoothYaw, 0) * Vector3.back * (smoothDistance * DistanceScale);
            // Near the horizon the camera approaches the ground; look slightly
            // above the hero so dragging farther can produce a real upward view.
            position.y = Mathf.Max(.45f, position.y);
            transform.position = position;
            transform.LookAt(lookTarget + Vector3.up * (Mathf.Clamp01(-smoothPitch / 18f) * 2.4f));
            transform.position += HitFeedback.CameraOffset;
        }

        private void ResetOrbitInput() { orbitInput.Reset(); inputFrame = -1; }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetOrbitInput(); }
        private void OnDisable() { ResetOrbitInput(); }
        private void OnDestroy() { if (active == this) active = null; }
    }

}
