using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed class GameSession : MonoBehaviour
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
        public bool InputBlocked { get { return !HasStarted || Paused || uiBlocking || IsDead; } }
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
            CollectRemainingDungeonLoot();
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
            CollectRemainingDungeonLoot();
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
            CollectRemainingDungeonLoot();
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
            ChangeZone(false);
            UpdateTimeScale();
        }

        private void Update()
        {
            if (!HasStarted || IsDead || Paused || uiBlocking) return;
            if (!InputBlocked)
            {
                if (Input.GetKeyDown(KeyCode.F) || MobileControls.ConsumePotion()) DrinkPotion();
                if (Input.GetKeyDown(KeyCode.T)) { if (InDungeon) { if (DungeonCleared) ReturnToCamp(); else Notify("先击败本轮敌人；按 H 可放弃副本返回营地。"); } else EnterDungeon(); }
                if (Input.GetKeyDown(KeyCode.H)) ReturnToCamp();
            }
            if (!InDungeon)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0 && Enemies.Count < 9)
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
        private void UpdateTimeScale() { Time.timeScale = HasStarted && !Paused && !uiBlocking && !IsDead ? 1 : 0; }

        private bool NearPortal() { return Player != null && Vector3.Distance(Player.transform.position, new Vector3(0, 0, 11)) < 4.3f; }

        public void EnterDungeon()
        {
            if (!HasStarted || IsDead || InDungeon) return;
            if (!NearPortal()) { Notify("请前往原野北方发光的传送门（小地图菱形），靠近后按 T。"); return; }
            if (Progression.Profile.level < 2) { Notify("遗迹需要 2 级。先在原野战斗，并学习第一个职业技能。"); return; }
            ChangeZone(true);
            Notify("已进入沉星遗迹");
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
            ChangeZone(false);
            Notify(abandoned ? "已撤离遗迹，生命恢复。随时可以重新挑战。" : "已回到营地，生命已恢复。按 I 整理战利品，按 K 学习技能。");
        }

        private void ChangeZone(bool dungeon)
        {
            CollectRemainingDungeonLoot();
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
            DungeonTier = 1 + Progression.Profile.clearedRuns;
            world = WorldBuilder.Build(dungeon ? ZoneKind.Dungeon : ZoneKind.Wilderness);
            Player.Teleport(new Vector3(0, 0, dungeon ? -9 : -10));
            Player.RefreshStats(true);
            Camera.main.GetComponent<AdventureCamera>().Snap();
            if (dungeon)
            {
                DungeonWave = 1;
                SpawnDungeonWave();
            }
            else
            {
                for (int i = 0; i < 8; i++) SpawnWildernessEnemy();
                respawnTimer = 8;
            }
            Progression.Save();
            changingZone = false;
        }

        private void SpawnWildernessEnemy()
        {
            Vector3 position = Vector3.zero;
            for (int i = 0; i < 24; i++)
            {
                position = new Vector3(Random.Range(-16f, 16f), 0, Random.Range(-3f, 16f));
                if (position.magnitude < 19 && (Player == null || Vector3.Distance(position, Player.transform.position) > 8)) break;
            }
            // North of camp keeps the starting area safe and enemies on the island.
            position = Vector3.ClampMagnitude(position, 19);
            EnemyKind kind = (EnemyKind)Random.Range(0, Progression.Profile.level >= 3 ? 3 : 2);
            int level = Mathf.Max(1, Progression.Profile.level - 1);
            SpawnEnemy(kind, level, position, false);
        }

        private void SpawnDungeonWave()
        {
            int level = Mathf.Max(2, Progression.Profile.level + DungeonTier - 1);
            int count = DungeonWave == TotalWaves ? 3 : 3 + DungeonWave;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + .35f) / count * Mathf.PI * 2;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 11, 0, Mathf.Sin(angle) * 7 + 4);
                bool boss = DungeonWave == TotalWaves && i == 0;
                SpawnEnemy(boss ? EnemyKind.Guardian : (EnemyKind)((i + DungeonWave) % 3), level, boss ? new Vector3(0, 0, 8) : position, boss);
            }
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
            int level = Progression.Profile.level;
            int experience = boss ? 100 + level * 12 : (InDungeon ? 22 : 16) + level * 2;
            int gold = boss ? 85 + DungeonTier * 20 : Random.Range(7, 15) + level;
            Progression.Profile.kills++;
            Progression.AddGold(gold);
            Progression.GrantExperience(experience);
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
                        Notify("获得 " + GameBalance.RarityName(loot.rarity) + "装备：「" + loot.name + "」 · 按 I 查看" + (string.IsNullOrEmpty(Progression.LastError) ? "" : " · " + Progression.LastError));
                        SpawnLootBeacon(position, GameBalance.RarityColor(loot.rarity));
                    }
                }
            }
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
            Progression.Save();
            if (InDungeon && !changingZone && Enemies.Count == 0 && !DungeonCleared) waveRoutine = StartCoroutine(NextWave());
        }

        private IEnumerator NextWave()
        {
            if (DungeonWave < TotalWaves)
            {
                Notify("第 " + DungeonWave + " 波已肃清 · 3 秒后下一波，恢复 25% 生命");
                Player.Heal(Player.MaxHealth * .25f);
                yield return new WaitForSeconds(3);
                if (!InDungeon || IsDead) yield break;
                DungeonWave++;
                SpawnDungeonWave();
                Notify(DungeonWave == TotalWaves ? "最终波 · 遗迹守卫苏醒！红色预警出现时及时闪避。" : "第 " + DungeonWave + " 波 · 敌人正在逼近");
            }
            else
            {
                DungeonCleared = true;
                GameAudio.Play(SoundCue.Victory);
                Progression.Profile.clearedRuns++;
                Progression.Profile.bestFloor = Mathf.Max(Progression.Profile.bestFloor, DungeonTier);
                Progression.AddGold(120 + DungeonTier * 30);
                Progression.GrantExperience(100 + DungeonTier * 20);
                Player.Heal(Player.MaxHealth);
                Progression.Save();
                Notify("遗迹通关 · 拾取战利品，返回营地");
            }
            waveRoutine = null;
        }

        public void OnPlayerDied()
        {
            if (IsDead) return;
            IsDead = true;
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
            IsDead = false;
            Paused = false;
            uiBlocking = false;
            ChangeZone(false);
            UpdateTimeScale();
            Notify("已在营地复苏。生命恢复，可在背包补充药水。");
        }

        public void DrinkPotion()
        {
            if (!HasStarted || IsDead || Player == null) return;
            if (Player.Health >= Player.MaxHealth - .5f) { Notify("生命已满，无需使用药水。"); return; }
            if (!Progression.UsePotion()) { Notify("药水不足，按 I 在背包中购买。"); return; }
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
            CollectRemainingDungeonLoot();
            if (HasStarted) Progression.Save();
            StopAllCoroutines();
            waveRoutine = null;
            HasStarted = false;
            Paused = false;
            uiBlocking = false;
            IsDead = false;
            foreach (EnemyController enemy in Enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            Enemies.Clear();
            if (Player != null) { Player.gameObject.SetActive(false); Destroy(Player.gameObject); Player = null; }
            if (world != null) { world.SetActive(false); Destroy(world); }
            InDungeon = false;
            DungeonCleared = false;
            world = WorldBuilder.Build(ZoneKind.Wilderness);
            Camera.main.GetComponent<AdventureCamera>().Snap();
            UpdateTimeScale();
        }

        private void OnProgressChanged() { if (Player != null && HasStarted) Player.RefreshStats(false); }
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
                Notify("拾取 " + pending.Item.name + (string.IsNullOrEmpty(Progression.LastError) ? "" : " · " + Progression.LastError));
            }
            return true;
        }

        public void CollectRemainingDungeonLoot()
        {
            if (Progression == null || pendingLoot.Count == 0) return;
            var ids = new List<string>(pendingLoot.Keys);
            foreach (string id in ids) TryCollectGroundLoot(id, false);
        }

        private void OnApplicationPause(bool pause) { if (pause && HasStarted) { Progression.Save(); SetPaused(true); } }
        private void OnApplicationFocus(bool focus) { if (!focus && HasStarted) SetPaused(true); }
        private void OnApplicationQuit() { CollectRemainingDungeonLoot(); if (HasStarted) Progression.Save(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            CollectRemainingDungeonLoot();
            if (Progression != null) { Progression.Changed -= OnProgressChanged; Progression.LeveledUp -= OnLevelUp; }
            Instance = null;
            Time.timeScale = 1;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed class AdventureCamera : MonoBehaviour
    {
        public const float MinimumPitch = 20f;
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
            position.y = Mathf.Max(2.4f, position.y);
            transform.position = position;
            transform.LookAt(lookTarget);
            transform.position += HitFeedback.CameraOffset;
        }

        private void ResetOrbitInput() { orbitInput.Reset(); inputFrame = -1; }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetOrbitInput(); }
        private void OnDisable() { ResetOrbitInput(); }
        private void OnDestroy() { if (active == this) active = null; }
    }

    public sealed class FloatingNumber : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }
        private TextMesh textMesh;
        private float life;
        private Color color;
        public void Initialize(string value, Color tint)
        {
            ActiveCount++;
            color = tint;
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = value;
            textMesh.fontSize = 40;
            textMesh.characterSize = .055f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = tint;
        }
        private void Update()
        {
            life += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * .75f;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            if (textMesh != null) textMesh.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((1.2f - life) * 2));
            if (life > 1.2f) Destroy(gameObject);
        }
        private void OnDestroy() { ActiveCount = Mathf.Max(0, ActiveCount - 1); }
    }
}
