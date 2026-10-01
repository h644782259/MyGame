using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Isolated Play-mode checks using actual pickup Update ticks and scene transitions.</summary>
    public static class GroundLootValidation
    {
        private const string Prefix = "Emberfall.RuntimeValidation.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        public static bool IsCheckingPause { get; private set; }

        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            RequireIsolatedRuntime(game);
            if (check == null) throw new ArgumentNullException(nameof(check));
            var fixture = new Fixture(game);
            try
            {
                fixture.Prepare();
                log?.Invoke("GROUND LOOT — actual landing, range, blocked input and scene-exit settlement");
                ProgressionService progression = game.Progression;
                int before = progression.Profile.inventory.Count;
                ItemData first = fixture.Roll();
                check(progression.Profile.inventory.Count == before && game.PendingLootCount == 0, "RollLoot creates data without inventory or scene collection");
                GroundLootPickup pickup = game.SpawnGroundLoot(first, game.Player.transform.position);
                check(pickup != null && game.PendingLootCount == 1 && !pickup.ReadyToCollect && !fixture.Contains(first), "Fresh gear lands visibly with protection and remains outside inventory");
                check(pickup.GetComponentsInChildren<Renderer>().Length >= 4, "Ground gear has actual model, rarity glow and label renderers");
                check(game.SpawnGroundLoot(first, game.Player.transform.position) == pickup && game.PendingLootCount == 1, "Repeated spawning of one item retains a single pickup");
                foreach (object tick in Wait(.1f)) yield return tick;
                check(pickup != null && !pickup.ReadyToCollect && !fixture.Contains(first), "Pickup cannot happen during the first 0.6 seconds even at player feet");
                foreach (object tick in Wait(.8f)) yield return tick;
                check(fixture.Contains(first) && game.PendingLootCount == 0, "Nearby landed gear enters the actual bag through Update");
                check(!game.TryCollectGroundLoot(first.id) && game.SpawnGroundLoot(first, Vector3.zero) == null && progression.Profile.inventory.Count == before + 1, "Repeated collection and respawning cannot duplicate collected gear");

                ItemData distant = fixture.Roll();
                Vector3 distantPoint = game.Player.transform.position + Vector3.right * 6;
                GroundLootPickup farPickup = game.SpawnGroundLoot(distant, distantPoint);
                foreach (object tick in Wait(.8f)) yield return tick;
                check(farPickup != null && farPickup.ReadyToCollect && !fixture.Contains(distant) && game.PendingLootCount == 1, "Landed gear outside pickup range stays on the ground");
                game.Player.Teleport(distantPoint + Vector3.left * 2.1f);
                foreach (object tick in Wait(.15f)) yield return tick;
                check(!fixture.Contains(distant), "A player 2.1 metres away does not collect gear");
                game.Player.Teleport(distantPoint + Vector3.left * 1.8f);
                foreach (object tick in Wait(.2f)) yield return tick;
                check(fixture.Contains(distant) && game.PendingLootCount == 0, "A player inside two metres automatically collects gear");

                ItemData blocked = fixture.Roll();
                Vector3 blockedPoint = game.Player.transform.position + Vector3.left * 5;
                game.SpawnGroundLoot(blocked, blockedPoint);
                foreach (object tick in Wait(.7f)) yield return tick;
                game.SetUIBlocking(true);
                game.Player.Teleport(blockedPoint);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(!fixture.Contains(blocked) && game.PendingLootCount == 1, "An open blocking panel prevents nearby automatic pickup");
                IsCheckingPause = true;
                game.SetPaused(true);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(game.Paused && !fixture.Contains(blocked), "A real pause prevents nearby pickup until resumed");
                game.SetPaused(false);
                IsCheckingPause = false;
                SetProperty(game, "IsDead", true);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(!fixture.Contains(blocked), "A dead session cannot collect nearby gear");
                SetProperty(game, "IsDead", false);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f)) yield return tick;
                check(fixture.Contains(blocked) && game.PendingLootCount == 0, "Unblocking living gameplay resumes automatic pickup");

                ItemData camp = fixture.Roll();
                game.SpawnGroundLoot(camp, new Vector3(-10, 0, -7));
                check(game.PendingLootCount == 1 && !fixture.Contains(camp), "An uncollected distant drop remains pending before camp return");
                game.ReturnToCamp();
                check(!game.InDungeon && fixture.Contains(camp) && game.PendingLootCount == 0, "Returning to camp settles all pending gear before old scene cleanup");
                fixture.EnterFreshDungeon();
                ItemData title = fixture.Roll();
                game.SpawnGroundLoot(title, new Vector3(10, 0, -6));
                game.QuitToTitle();
                check(!game.HasStarted && game.PendingLootCount == 0 && fixture.Contains(title), "Returning to title collects distant and protected gear before destroying the player");
                game.ContinueGame();
                check(game.HasStarted && fixture.Contains(title) && fixture.Contains(camp), "Scene-exit pickups survive the actual persisted reload");
                fixture.EnterFreshDungeon();
                ItemData settled = fixture.Roll();
                game.SpawnGroundLoot(settled, new Vector3(10, 0, 0));
                int beforeSettle = progression.Profile.inventory.Count;
                game.CollectRemainingDungeonLoot();
                game.CollectRemainingDungeonLoot();
                check(fixture.Contains(settled) && game.PendingLootCount == 0 && progression.Profile.inventory.Count == beforeSettle + 1, "Repeated explicit exit settlement is idempotent");
                log?.Invoke("GROUND LOOT complete; restoring profile and isolated save bytes, then preparing a fresh first wave.");
            }
            finally
            {
                IsCheckingPause = false;
                fixture.Restore();
            }
        }

        private static IEnumerable<object> Wait(float seconds, bool scaled = true)
        {
            double start = scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup;
            double deadline = EditorApplication.timeSinceStartup + 6;
            while ((scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup) - start < seconds)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Ground-loot validation did not receive game-time updates.");
                yield return null;
            }
        }

        private static void RequireIsolatedRuntime(GameSession game)
        {
            if (!Application.isPlaying || !EditorApplication.isPlaying || !SessionState.GetBool(Prefix + "Active", false) ||
                game == null || game != GameSession.Instance || !game.HasStarted || !game.InDungeon || game.InputBlocked || game.DungeonWave != 1 || game.PendingLootCount != 0)
                throw new InvalidOperationException("Ground-loot validation requires the isolated runner in an active fresh dungeon first wave with no pending drops.");
            string result = SessionState.GetString(Prefix + "Results", "");
            string save = SessionState.GetString(Prefix + "Save", "");
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            if (string.IsNullOrWhiteSpace(result) || string.IsNullOrWhiteSpace(save) || string.IsNullOrWhiteSpace(overridden))
                throw new InvalidOperationException("Ground-loot validation refused missing isolated paths.");
            string expectedRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            string runName = Path.GetFileName(Full(result));
            Guid guid;
            if (!Same(Path.GetDirectoryName(Full(result)), expectedRoot) || !runName.StartsWith("PlayMode-", StringComparison.Ordinal) ||
                !Guid.TryParseExact(runName.Substring("PlayMode-".Length), "N", out guid) || !Same(save, Path.Combine(result, "IsolatedSave")) ||
                !Same(save, overridden) || !Same(save, game.Progression.SaveDirectory) || !IsSlotFileInDirectory(game.Progression.SaveFilePath, save))
                throw new InvalidOperationException("Ground-loot validation refused a save path outside its isolated runner.");
        }

        private static bool IsSlotFileInDirectory(string path, string directory)
        {
            if (!Same(Path.GetDirectoryName(Full(path)), directory)) return false;
            string name = Path.GetFileName(path);
            if (name == "emberfall-save.json") return true;
            const string prefix = "emberfall-save-", suffix = ".json";
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal) || name.Length != prefix.Length + 32 + suffix.Length) return false;
            Guid id;
            string token = name.Substring(prefix.Length, 32);
            return Guid.TryParseExact(token, "N", out id) && token == id.ToString("N");
        }

        private static string Full(string path) { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        private static bool Same(string a, string b) { return string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase); }
        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }
        private static void SetProperty(object owner, string name, object value)
        {
            PropertyInfo property = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(owner.GetType().FullName, name);
            setter.Invoke(owner, new[] { value });
        }

        private sealed class Fixture
        {
            private readonly GameSession game;
            private readonly GameProfile originalProfile;
            private readonly string originalError;
            private readonly object notification;
            private readonly object notificationUntil;
            private readonly bool playerEnabled;
            private readonly string[] filePaths;
            private readonly byte[][] fileBytes;
            private readonly HashSet<string> sessionCollected;
            private readonly HashSet<string> progressionCollected;
            private readonly string[] originalSessionCollected;
            private readonly string[] originalProgressionCollected;

            public Fixture(GameSession session)
            {
                game = session;
                originalProfile = game.Progression.Profile;
                originalError = game.Progression.LastError;
                notification = Field(typeof(GameSession), "notification").GetValue(game);
                notificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
                playerEnabled = game.Player.enabled;
                string path = game.Progression.SaveFilePath;
                filePaths = new[] { path, path + ".bak", path + ".tmp" };
                fileBytes = new byte[filePaths.Length][];
                for (int i = 0; i < filePaths.Length; i++) fileBytes[i] = File.Exists(filePaths[i]) ? File.ReadAllBytes(filePaths[i]) : null;
                sessionCollected = (HashSet<string>)Field(typeof(GameSession), "collectedGroundLoot").GetValue(game);
                progressionCollected = (HashSet<string>)Field(typeof(ProgressionService), "collectedLootIds").GetValue(game.Progression);
                originalSessionCollected = new List<string>(sessionCollected).ToArray();
                originalProgressionCollected = new List<string>(progressionCollected).ToArray();
            }

            public void Prepare()
            {
                GameProfile profile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile));
                profile.level = Math.Max(2, profile.level);
                profile.inventory.RemoveAll(item => item.id != profile.weaponId && item.id != profile.armorId && item.id != profile.relicId);
                SetProperty(game.Progression, "Profile", profile);
                FreezeFixture();
            }

            private void FreezeFixture()
            {
                game.SetUIBlocking(false);
                game.SetPaused(false);
                game.Player.enabled = false;
                game.Player.Teleport(new Vector3(0, 0, -12));
                for (int i = 0; i < game.Enemies.Count; i++)
                {
                    EnemyController enemy = game.Enemies[i];
                    if (enemy == null) continue;
                    enemy.enabled = false;
                    enemy.transform.position = new Vector3((i - 2) * 1.5f, 0, 12);
                }
            }

            public ItemData Roll()
            {
                ItemData item = game.Progression.RollLoot(2, true);
                item.id = "ground-validation-" + Guid.NewGuid().ToString("N");
                return item;
            }
            public bool Contains(ItemData item) { return game.Progression.Profile.inventory.Exists(value => value != null && value.id == item.id); }
            public void EnterFreshDungeon()
            {
                game.Player.Teleport(new Vector3(0, 0, 11));
                game.EnterDungeon();
                if (!game.DungeonSelectionOpen) throw new InvalidOperationException("Ground-loot fixture could not open the dungeon selector.");
                game.ConfirmDungeonSelection();
                if (!game.InDungeon || game.DungeonSelectionOpen || game.DungeonWave != 1) throw new InvalidOperationException("Ground-loot fixture could not re-enter a fresh dungeon.");
                FreezeFixture();
            }

            public void Restore()
            {
                try
                {
                    game.CollectRemainingDungeonLoot();
                    SetProperty(game, "IsDead", false);
                    SetProperty(game.Progression, "Profile", originalProfile);
                    if (!game.HasStarted) typeof(GameSession).GetMethod("BeginAdventure", PrivateInstance).Invoke(game, null);
                    typeof(GameSession).GetMethod("ChangeZone", PrivateInstance).Invoke(game, new object[] { true });
                    game.SetPaused(false);
                    game.SetUIBlocking(false);
                    foreach (EnemyController enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
                    game.Player.enabled = playerEnabled;
                }
                finally
                {
                    SetProperty(game.Progression, "Profile", originalProfile);
                    sessionCollected.Clear(); sessionCollected.UnionWith(originalSessionCollected);
                    progressionCollected.Clear(); progressionCollected.UnionWith(originalProgressionCollected);
                    SetProperty(game.Progression, "LastError", originalError);
                    Field(typeof(GameSession), "notification").SetValue(game, notification);
                    Field(typeof(GameSession), "notificationUntil").SetValue(game, notificationUntil);
                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        if (fileBytes[i] != null) File.WriteAllBytes(filePaths[i], fileBytes[i]);
                        else if (File.Exists(filePaths[i])) File.Delete(filePaths[i]);
                    }
                }
            }
        }
    }
}
