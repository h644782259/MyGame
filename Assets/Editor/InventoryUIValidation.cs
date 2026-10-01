using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Behavior checks invoked by RuntimeValidation after a hero starts. Never starts Play mode.</summary>
    public static class InventoryUIValidation
    {
        private const string Prefix = "Emberfall.RuntimeValidation.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static int Validate(GameSession game)
        {
            RequireIsolatedRuntime(game);
            var validation = new Cases(game);
            try
            {
                validation.Run();
                return validation.Assertions;
            }
            finally
            {
                validation.Restore();
            }
        }

        private static void RequireIsolatedRuntime(GameSession game)
        {
            if (!Application.isPlaying || !EditorApplication.isPlaying || !SessionState.GetBool(Prefix + "Active", false))
                throw new InvalidOperationException("Inventory UI validation is only available during RuntimeValidation Play mode.");
            if (game == null || game != GameSession.Instance || !game.HasStarted || game.Progression == null || game.GetComponent<GameUI>() == null)
                throw new InvalidOperationException("Inventory UI validation requires the active RuntimeValidation hero and UI.");

            string saved = SessionState.GetString(Prefix + "Save", "");
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            string results = SessionState.GetString(Prefix + "Results", "");
            if (string.IsNullOrWhiteSpace(saved) || string.IsNullOrWhiteSpace(overridden) || string.IsNullOrWhiteSpace(results))
                throw new InvalidOperationException("RuntimeValidation did not provide its isolated save paths.");
            string save = Full(saved);
            string result = Full(results);
            string expectedRoot = Full(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            string runName = Path.GetFileName(result);
            Guid runId;
            if (!Same(Path.GetDirectoryName(result), expectedRoot) || !runName.StartsWith("PlayMode-", StringComparison.Ordinal) ||
                !Guid.TryParseExact(runName.Substring("PlayMode-".Length), "N", out runId) ||
                !Same(save, Path.Combine(result, "IsolatedSave")) || !Same(save, overridden) ||
                !Same(game.Progression.SaveDirectory, save) || !Same(game.Progression.SaveFilePath, Path.Combine(save, "emberfall-save.json")))
                throw new InvalidOperationException("Inventory UI validation refused a save path outside this RuntimeValidation run.");
        }

        private static string Full(string path) { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        private static bool Same(string a, string b) { return string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase); }
        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }
        private static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException("Inventory UI call failed: " + name, exception.InnerException ?? exception);
            }
        }
        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(target.GetType().FullName, name);
            setter.Invoke(target, new[] { value });
        }

        private sealed class SavedFile
        {
            private readonly string path;
            private readonly byte[] bytes;
            public SavedFile(string value) { path = value; bytes = File.Exists(path) ? File.ReadAllBytes(path) : null; }
            public void Restore()
            {
                if (bytes != null) File.WriteAllBytes(path, bytes);
                else if (File.Exists(path)) File.Delete(path);
            }
        }

        private sealed class Cases
        {
            private readonly GameSession game;
            private readonly GameUI ui;
            private readonly ProgressionService progression;
            private readonly GameProfile originalProfile;
            private readonly string originalError;
            private readonly object originalNotification;
            private readonly object originalNotificationUntil;
            private readonly Dictionary<string, object> originalFields = new Dictionary<string, object>();
            private readonly ItemData[] originalBag;
            private readonly ItemData[] equipped = new ItemData[3];
            private readonly SavedFile[] files;
            public int Assertions { get; private set; }
            private GameProfile Profile { get { return progression.Profile; } }
            private List<ItemData> Bag { get { return (List<ItemData>)Get("bagItems"); } }

            public Cases(GameSession session)
            {
                game = session;
                ui = game.GetComponent<GameUI>();
                progression = game.Progression;
                originalProfile = progression.Profile;
                originalError = progression.LastError;
                originalNotification = Field(typeof(GameSession), "notification").GetValue(game);
                originalNotificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
                foreach (string name in new[] { "inventoryFilter", "inventorySort", "inventoryScroll", "selectedItem", "unequippedCount" })
                    originalFields[name] = Get(name);
                originalBag = Bag.ToArray();
                for (int i = 0; i < equipped.Length; i++)
                {
                    ItemData item = progression.Equipped((ItemSlot)i);
                    if (item == null) throw new InvalidOperationException("Inventory UI validation requires all three starter equipment slots.");
                    equipped[i] = Clone(item);
                }
                files = new[] { new SavedFile(progression.SaveFilePath), new SavedFile(progression.SaveFilePath + ".bak"), new SavedFile(progression.SaveFilePath + ".tmp") };
            }

            public void Run()
            {
                // The original profile object and all of its item references remain untouched.
                SetProperty(progression, "Profile", JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile)));
                ValidateScoreAndOrdering();
                ValidateFiltersAndSelection();
                ValidateSalesAndScrolling();
                ValidateUpgradeReordering();
                ValidateGoldCapAndEmptyInventory();
            }

            private void ValidateScoreAndOrdering()
            {
                ItemData score = Item("score", ItemSlot.Weapon, 1, Rarity.Common, 2, 4, 30);
                score.upgradeLevel = 8;
                Check(Mathf.Approximately(ProgressionService.EquipmentScore(score), 28), "score weights actual attack, defense and health exactly once");
                score.upgradeLevel = 1;
                Check(Mathf.Approximately(ProgressionService.EquipmentScore(score), 28), "upgrade counter does not double-count already upgraded stats");
                ResetItems(
                    Item("weapon-strong", ItemSlot.Weapon, 5, Rarity.Common, 100),
                    Item("armor-level", ItemSlot.Armor, 90, Rarity.Rare, 0, 10, 50),
                    Item("relic-legend", ItemSlot.Relic, 10, Rarity.Legendary, 1),
                    Item("tie-b", ItemSlot.Weapon, 20, Rarity.Epic, 10),
                    Item("tie-a", ItemSlot.Weapon, 20, Rarity.Epic, 10),
                    Item("weapon-level", ItemSlot.Weapon, 25, Rarity.Rare, 10),
                    Item("armor-rare", ItemSlot.Armor, 20, Rarity.Rare, 10),
                    Item("armor-legend", ItemSlot.Armor, 20, Rarity.Legendary, 10));
                Set("inventorySort", 0);
                Rebuild();
                Order("score descending with level, rarity and ordinal-ID ties", "weapon-strong", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "armor-level", "relic-legend");
                Profile.inventory.Reverse();
                Rebuild();
                Order("identical sort survives opposite source-list order", "weapon-strong", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "armor-level", "relic-legend");
                Set("inventorySort", 1);
                Rebuild();
                Order("level descending with rarity and stable-ID ties", "armor-level", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "relic-legend", "weapon-strong");
                Set("inventorySort", 2);
                Rebuild();
                Order("rarity descending with level and stable-ID ties", "armor-legend", "relic-legend", "tie-a", "tie-b", "armor-level", "weapon-level", "armor-rare", "weapon-strong");
            }

            private void ValidateFiltersAndSelection()
            {
                Set("inventorySort", 0);
                Set("inventoryFilter", -1);
                Rebuild();
                Check(Bag.Count == 8 && (int)Get("unequippedCount") == 8, "all filter contains every unequipped item exactly once");
                foreach (ItemData item in equipped) Check(!Bag.Exists(value => value.id == item.id), "equipped slot excluded from sale list: " + item.slot);
                Set("inventoryFilter", 0);
                Rebuild();
                Order("weapon filter", "weapon-strong", "weapon-level", "tie-a", "tie-b");
                Set("selectedItem", "tie-a");
                Set("inventoryFilter", 1);
                Rebuild();
                Order("armor filter", "armor-legend", "armor-rare", "armor-level");
                ItemData selected = Resolve();
                Check(selected != null && selected.id == "armor-legend", "hidden previous selection resolves to the first visible item");
                Check((int)Get("unequippedCount") == 8, "filtered count does not alter total unequipped count");
                Set("selectedItem", equipped[0].id);
                Check(Resolve().id == equipped[0].id, "visible equipped selection remains inspectable while filtering bag items");
                int count = Profile.inventory.Count;
                int gold = Profile.gold;
                Call(ui, "SellInventoryItem", "tie-a");
                Check(Profile.inventory.Count == count && Profile.gold == gold, "sale entry rejects items outside current filter");
                Bag.Add(Profile.inventory.Find(value => value.id == equipped[0].id));
                Call(ui, "SellInventoryItem", equipped[0].id);
                Check(Profile.inventory.Count == count && Profile.gold == gold, "sale entry protects equipped items even in a stale list");
                Set("inventoryFilter", 2);
                Rebuild();
                Order("relic filter", "relic-legend");
                Set("selectedItem", "missing-item");
                Check(Resolve().id == "relic-legend", "missing selection resolves to a valid filtered item");
            }

            private void ValidateSalesAndScrolling()
            {
                var items = new List<ItemData>();
                for (int i = 0; i < 10; i++) items.Add(Item("sale-" + i.ToString("00"), ItemSlot.Weapon, 5, Rarity.Common, 100 - i * 5));
                items.Add(Item("kept-armor", ItemSlot.Armor, 1, Rarity.Legendary, 0, 400));
                ResetItems(items.ToArray());
                Set("inventoryFilter", 0);
                Set("inventorySort", 2);
                Rebuild();
                Set("selectedItem", "sale-03");
                Set("inventoryScroll", new Vector2(0, 99999));
                int gold = Profile.gold;
                int price = progression.SellValue(Bag[3]);
                Call(ui, "SellInventoryItem", "sale-03");
                Check(Profile.gold == gold + price && !Profile.inventory.Exists(item => item.id == "sale-03"), "UI sale removes item and grants exact quoted gold");
                Check(game.Notification.Contains("sale-03") && game.Notification.Contains("+" + price + " 金币"), "sale feedback names item and actual gold gain");
                Check(string.IsNullOrEmpty(progression.LastError), "UI sale writes its isolated save without errors");
                Check((int)Get("inventoryFilter") == 0 && (int)Get("inventorySort") == 2, "sale preserves non-default filter and sort");
                Check((string)Get("selectedItem") == "sale-04", "selling selected middle row selects its following neighbor");
                Order("sale rebuilds visible sorted list", "sale-00", "sale-01", "sale-02", "sale-04", "sale-05", "sale-06", "sale-07", "sale-08", "sale-09");
                // The current bag viewport is 330 units tall, with 76-unit rows and 4 units of padding.
                Check(Mathf.Approximately(((Vector2)Get("inventoryScroll")).y, 358), "sale clamps excessive scrolling to the shortened list");
                Call(ui, "SellInventoryItem", "sale-00");
                Check((string)Get("selectedItem") == "sale-04", "selling another row preserves selected item identity");
                Set("selectedItem", "sale-09");
                Call(ui, "SellInventoryItem", "sale-09");
                Check((string)Get("selectedItem") == "sale-08", "selling final row selects the previous neighbor without out-of-range access");
                while (Bag.Count > 0)
                {
                    string id = Bag[0].id;
                    Set("selectedItem", id);
                    Call(ui, "SellInventoryItem", id);
                }
                Check(Profile.inventory.Count == 4 && Profile.inventory.Exists(item => item.id == "kept-armor"), "selling filtered items preserves equipment and other categories");
                Check((int)Get("inventoryFilter") == 0 && (int)Get("inventorySort") == 2, "empty filtered bag retains view preferences");
                Check((string)Get("selectedItem") == equipped[0].id && Resolve().id == equipped[0].id, "empty filtered bag falls back to valid equipped detail");
                Check(Mathf.Approximately(((Vector2)Get("inventoryScroll")).y, 0), "empty filtered bag resets vertical scroll to zero");
                gold = Profile.gold;
                Call(ui, "SellInventoryItem", "sale-03");
                Call(ui, "SellInventoryItem", "unknown");
                Check(Profile.gold == gold && Profile.inventory.Count == 4, "repeated and unknown sale IDs cannot grant gold twice");
            }

            private void ValidateUpgradeReordering()
            {
                ResetItems(Item("leader", ItemSlot.Weapon, 1, Rarity.Common, 21), Item("upgrade-me", ItemSlot.Weapon, 1, Rarity.Common, 20));
                Set("selectedItem", "upgrade-me");
                Order("score order before enhancement", "leader", "upgrade-me");
                Check(progression.Upgrade("upgrade-me"), "fixture enhancement succeeds through production economy");
                Rebuild();
                Order("score order updates after actual enhancement", "upgrade-me", "leader");
                Check(Resolve().id == "upgrade-me", "enhancement reordering preserves item selection");
            }

            private void ValidateGoldCapAndEmptyInventory()
            {
                ResetItems(Item("cap-item", ItemSlot.Weapon, 1, Rarity.Common, 10));
                Profile.gold = 999999998;
                Call(ui, "SellInventoryItem", "cap-item");
                Check(Profile.gold == 999999999 && game.Notification.Contains("+1 金币"), "gold-cap sale reports actual credited amount rather than full price");
                Profile.inventory.Clear();
                Profile.weaponId = Profile.armorId = Profile.relicId = null;
                Set("selectedItem", "missing-item");
                Rebuild();
                Check(Bag.Count == 0 && Resolve() == null && Get("selectedItem") == null, "fully empty inventory resolves a null detail without indexing errors");
            }

            private void ResetItems(params ItemData[] items)
            {
                Profile.inventory = new List<ItemData>();
                foreach (ItemData item in equipped) Profile.inventory.Add(Clone(item));
                foreach (ItemData item in items) Profile.inventory.Add(item);
                Profile.weaponId = equipped[0].id;
                Profile.armorId = equipped[1].id;
                Profile.relicId = equipped[2].id;
                Profile.gold = 5000;
                Set("inventoryFilter", -1);
                Set("inventorySort", 0);
                Set("inventoryScroll", Vector2.zero);
                Set("selectedItem", null);
                Rebuild();
            }

            private static ItemData Item(string id, ItemSlot slot, int level, Rarity rarity, int attack, int defense = 0, int health = 0)
            {
                return new ItemData { id = id, name = id, slot = slot, level = level, rarity = rarity, attack = attack, defense = defense, health = health };
            }
            private static ItemData Clone(ItemData item) { return JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item)); }
            private object Get(string name) { return Field(typeof(GameUI), name).GetValue(ui); }
            private void Set(string name, object value) { Field(typeof(GameUI), name).SetValue(ui, value); }
            private void Rebuild() { Call(ui, "RebuildBagItems"); }
            private ItemData Resolve() { return (ItemData)Call(ui, "ResolveSelectedItem"); }
            private void Order(string label, params string[] expected)
            {
                var actual = new string[Bag.Count];
                for (int i = 0; i < Bag.Count; i++) actual[i] = Bag[i].id;
                Check(string.Join(",", actual) == string.Join(",", expected), label + " | actual: " + string.Join(",", actual));
            }
            private void Check(bool condition, string label)
            {
                if (!condition) throw new InvalidOperationException("Inventory UI validation failed: " + label);
                Assertions++;
            }

            public void Restore()
            {
                SetProperty(progression, "Profile", originalProfile);
                SetProperty(progression, "LastError", originalError);
                foreach (KeyValuePair<string, object> pair in originalFields) Set(pair.Key, pair.Value);
                Bag.Clear();
                Bag.AddRange(originalBag);
                Field(typeof(GameSession), "notification").SetValue(game, originalNotification);
                Field(typeof(GameSession), "notificationUntil").SetValue(game, originalNotificationUntil);
                foreach (SavedFile file in files) file.Restore();
            }
        }
    }
}
