using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emberfall
{
    public sealed class SaveSlotInfo
    {
        public string Id, DisplayName;
        public HeroClass HeroClass;
        public int Level;
        public DateTime SavedAtUtc;
        public bool CanLoad, RecoveredFromBackup, IsCurrent;
    }

    /// <summary>Owns the character's persistent progression. It has no scene dependencies.</summary>
    public class ProgressionService
    {
        public const int MaximumLevel = 100;
        public const int InventoryCapacity = 72;
        public const int MaximumUpgrade = 10;
        public const int PotionPrice = 20;
        public const int PendingLootCapacity = 24;
        public const int RecoveryLootCapacity = 256;
        public const int MechanicExchangeCost = 12;
        public const int MaximumMasteryRank = 23;
        private static readonly int[] WingHealthPercents = { 3, 5, 8, 12 };
        private static readonly int[] WingArmorPercents = { 2, 3, 5, 8 };
        private static readonly int[] WeaponPercents = { 2, 4, 6, 9 };
        // Absolute probabilities per opened dungeon chest: 22% common, 12% rare,
        // 5% epic, 1% legendary, and 60% without a fashion drop.
        public static Rarity? RollFashionRarity(int roll)
        {
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException("roll");
            if (roll < 1) return Rarity.Legendary;
            if (roll < 6) return Rarity.Epic;
            if (roll < 18) return Rarity.Rare;
            if (roll < 40) return Rarity.Common;
            return null;
        }

        public static string FashionName(FashionSlot slot, Rarity rarity)
        {
            string prefix = new[] { "流光", "星纹", "苍穹", "烬王" }[(int)rarity];
            return prefix + (slot == FashionSlot.Wings ? "之翼" : "兵装");
        }

        public static string FashionBonus(FashionSlot slot, Rarity rarity)
        {
            int rank = (int)rarity;
            int primary = slot == FashionSlot.Wings ? WingHealthPercents[rank] : WeaponPercents[rank];
            int secondary = slot == FashionSlot.Wings ? WingArmorPercents[rank] : WeaponPercents[rank];
            return slot == FashionSlot.Wings ? "生命 +" + primary + "% · 防御 +" + secondary + "%"
                : "攻击 +" + primary + "% · 暴击几率 ×" + (100 + secondary) + "%";
        }

        public static int WeaponFashionPercent(Rarity rarity) { return WeaponPercents[(int)rarity]; }
        private const int MaximumGold = 999999999;
        private const int MaximumEquipmentStat = 10000;
        private const int MaximumEquipmentHealth = 100000;
        private const string SaveFormat = "emberfall-character";
        private readonly string saveDirectory;
        private string savePath;
        private string backupPath;
        private string temporaryPath;
        private string currentSlotId = "legacy";
        private readonly System.Random random = new System.Random();
        private readonly HashSet<string> collectedLootIds = new HashSet<string>(StringComparer.Ordinal);

        [Serializable]
        private class SaveFile
        {
            public string format;
            public int version;
            public GameProfile profile;
        }

        public GameProfile Profile { get; private set; }
        public ChestReward LastChestReward { get { return Profile.lastChestReward; } }
        public int RecoveryLootCount { get { return Profile.recoveryLoot.Count; } }
        public bool CanEnterDungeon { get { return RecoveryLootCount == 0 && CanReceiveProtectedLoot; } }
        public event Action Changed;
        public event Action<int> LeveledUp;
        public string LastError { get; private set; }
        public string SaveDirectory { get { return saveDirectory; } }
        public string SaveFilePath { get { return savePath; } }
        public bool HasSave { get { return DiscoverSlotIds().Count > 0; } }

        public ProgressionService(string saveDirectory = null)
        {
            string directory = string.IsNullOrWhiteSpace(saveDirectory) ? Application.persistentDataPath : saveDirectory;
            this.saveDirectory = directory;
            SelectSlotPath("legacy");
            Profile = CreateProfile(HeroClass.Vanguard);
            LastError = string.Empty;
        }

        public void NewGame(HeroClass heroClass)
        {
            if (!Enum.IsDefined(typeof(HeroClass), heroClass)) heroClass = HeroClass.Vanguard;
            Profile = CreateProfile(heroClass);
            collectedLootIds.Clear();
            Commit();
        }

        public bool Load()
        {
            // A newly constructed service still addresses the original legacy file.
            // Once explicitly selected, reload remains local to that selected slot.
            return LoadSlot(currentSlotId);
        }

        public bool LoadSlot(string id)
        {
            string normalized;
            if (!TryNormalizeSlotId(id, out normalized)) return Fail("无效的存档编号。");
            string candidatePath = SlotPath(normalized);
            GameProfile loaded;
            string failure;
            bool recovered;
            if (TryReadSlot(candidatePath, out loaded, out failure, out recovered))
            {
                if (currentSlotId != normalized) collectedLootIds.Clear();
                SelectSlotPath(normalized);
                Profile = loaded;
                LastError = failure;
                RaiseChanged();
                return true;
            }
            return Fail(File.Exists(candidatePath) || File.Exists(candidatePath + ".bak")
                ? "这个存档及其备份均无法读取；其他存档仍可选择。" : "找不到这个存档。");
        }

        public List<SaveSlotInfo> GetSaveSlots()
        {
            var slots = new List<SaveSlotInfo>();
            foreach (string id in DiscoverSlotIds())
            {
                string path = SlotPath(id);
                GameProfile loaded;
                string error;
                bool recovered;
                bool readable = TryReadSlot(path, out loaded, out error, out recovered);
                DateTime written = SafeWriteTime(path);
                DateTime backupWritten = SafeWriteTime(path + ".bak");
                if (backupWritten > written) written = backupWritten;
                slots.Add(new SaveSlotInfo
                {
                    Id = id,
                    DisplayName = readable ? GameBalance.ClassName(loaded.heroClass) + " · " + loaded.level + "级" + (id == "legacy" ? " · 旧存档" : "") : (id == "legacy" ? "旧存档 · 无法读取" : "存档 " + id.Substring(0, 6) + " · 无法读取"),
                    HeroClass = readable ? loaded.heroClass : HeroClass.Vanguard,
                    Level = readable ? loaded.level : 0,
                    SavedAtUtc = written,
                    CanLoad = readable,
                    RecoveredFromBackup = recovered,
                    IsCurrent = string.Equals(currentSlotId, id, StringComparison.Ordinal)
                });
            }
            slots.Sort((a, b) => { int time = b.SavedAtUtc.CompareTo(a.SavedAtUtc); return time != 0 ? time : string.CompareOrdinal(a.Id, b.Id); });
            return slots;
        }

        public bool CreateNewSlot(HeroClass hero)
        {
            if (!Enum.IsDefined(typeof(HeroClass), hero)) return Fail("无效的职业。");
            return CreateSlot(CreateProfile(hero), true);
        }

        public bool SaveAsNewSlot()
        {
            try
            {
                // Validate/write a deep snapshot, never mutate the current profile
                // while attempting a save that can still fail.
                GameProfile snapshot = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
                if (snapshot == null) return Fail("无法创建当前角色的存档快照。");
                return CreateSlot(snapshot, false);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is NotSupportedException)
            {
                return Fail("另存失败：" + exception.Message);
            }
        }

        private bool CreateSlot(GameProfile candidate, bool newCharacter)
        {
            string id = Guid.NewGuid().ToString("N");
            string path = SlotPath(id);
            string failure;
            if (!TryWriteProfile(candidate, path, true, out failure)) return Fail(failure);
            // Both primary and backup are now durably written. Only then publish
            // the new active profile/path and notify the UI.
            SelectSlotPath(id);
            Profile = candidate;
            if (newCharacter) collectedLootIds.Clear();
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        private void SelectSlotPath(string id)
        {
            currentSlotId = id;
            savePath = SlotPath(id);
            backupPath = savePath + ".bak";
            temporaryPath = savePath + ".tmp";
        }

        private string SlotPath(string id)
        {
            return Path.Combine(saveDirectory, id == "legacy" ? "emberfall-save.json" : "emberfall-save-" + id + ".json");
        }

        private static bool TryNormalizeSlotId(string id, out string normalized)
        {
            normalized = null;
            if (id == "legacy") { normalized = id; return true; }
            Guid guid;
            if (id == null || id.Length != 32 || !Guid.TryParseExact(id, "N", out guid)) return false;
            normalized = guid.ToString("N");
            return true;
        }

        private List<string> DiscoverSlotIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (!Directory.Exists(saveDirectory)) return new List<string>();
                foreach (string file in Directory.EnumerateFiles(saveDirectory, "emberfall-save*.json*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(file);
                    if (name.EndsWith(".bak", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 4);
                    if (name == "emberfall-save.json") { ids.Add("legacy"); continue; }
                    const string prefix = "emberfall-save-", suffix = ".json";
                    if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal)) continue;
                    string raw = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
                    string id;
                    // Generated filenames are canonical lowercase GuidN. Unknown
                    // files, temp writes and subdirectories are never save slots.
                    if (raw.Length == 32 && TryNormalizeSlotId(raw, out id) && raw == id) ids.Add(id);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) { }
            return new List<string>(ids);
        }

        private static DateTime SafeWriteTime(string path)
        {
            try { return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            { return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc); }
        }

        private static bool TryReadSlot(string primary, out GameProfile profile, out string error, out bool recovered)
        {
            recovered = false;
            if (TryReadProfile(primary, out profile, out error)) return true;
            if (!TryReadProfile(primary + ".bak", out profile, out error)) return false;
            recovered = true;
            error = "主存档无法读取，已恢复上一次备份。" + (string.IsNullOrEmpty(error) ? "" : " " + error);
            return true;
        }

        public void Save()
        {
            string failure;
            if (TryWriteProfile(Profile, savePath, false, out failure)) LastError = string.Empty;
            else { LastError = failure; Debug.LogWarning("Emberfall: " + failure); }
        }

        private static bool TryWriteProfile(GameProfile profile, string primary, bool createOnly, out string failure)
        {
            string backup = primary + ".bak", temporary = primary + ".tmp";
            bool ownsTemporary = false, ownsBackup = false;
            failure = string.Empty;
            try
            {
                ValidateProfile(profile);
                Directory.CreateDirectory(Path.GetDirectoryName(primary));
                if (createOnly && (File.Exists(primary) || File.Exists(backup) || File.Exists(temporary)))
                    throw new IOException("新存档文件名已被占用，请重试。");
                var save = new SaveFile { format = SaveFormat, version = 1, profile = profile };
                string json = JsonUtility.ToJson(save, true);
                // Flush the complete new document before atomically replacing the old one.
                using (var stream = new FileStream(temporary, createOnly ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    ownsTemporary = true;
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        writer.Write(json);
                        writer.Flush();
                        stream.Flush(true);
                    }
                }
                if (createOnly)
                {
                    using (var backupStream = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        ownsBackup = true;
                        using (var source = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read)) source.CopyTo(backupStream);
                        backupStream.Flush(true);
                    }
                    File.Move(temporary, primary);
                }
                else if (File.Exists(primary))
                {
                    GameProfile previous;
                    string error;
                    // A corrupt primary must never replace a usable recovery backup.
                    bool validPrevious = TryReadProfile(primary, out previous, out error);
                    File.Replace(temporary, primary, validPrevious ? backup : null, true);
                }
                else
                {
                    File.Move(temporary, primary);
                    if (!File.Exists(backup)) File.Copy(primary, backup);
                }
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                failure = "保存失败：" + exception.Message;
                if (createOnly)
                {
                    if (ownsTemporary) DeleteFailedSlotFile(temporary);
                    if (ownsBackup) DeleteFailedSlotFile(backup);
                }
                return false;
            }
        }

        private static void DeleteFailedSlotFile(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public StatBlock GetStats()
        {
            int level = Clamp(Profile.level, 1, MaximumLevel);
            float growth = level - 1;
            StatBlock stats;
            switch (Profile.heroClass)
            {
                case HeroClass.Arcanist:
                    stats = new StatBlock { MaxHealth = 125 + 15 * growth, Damage = 24 + 3.5f * growth, Armor = 2 + growth, MoveSpeed = 6.2f, CritChance = .10f };
                    break;
                case HeroClass.Ranger:
                    stats = new StatBlock { MaxHealth = 140 + 17 * growth, Damage = 21 + 3.2f * growth, Armor = 4 + 1.1f * growth, MoveSpeed = 6.6f, CritChance = .14f };
                    break;
                case HeroClass.Summoner:
                    stats = new StatBlock { MaxHealth = 125 + 13 * growth, Damage = 15 + 2.3f * growth, Armor = 3 + .75f * growth, MoveSpeed = 5.5f, CritChance = .10f };
                    break;
                default:
                    stats = new StatBlock { MaxHealth = 170 + 20 * growth, Damage = 20 + 3 * growth, Armor = 7 + 1.4f * growth, MoveSpeed = 6f, CritChance = .08f };
                    break;
            }
            for (int slot = 0; slot < 3; slot++)
            {
                ItemData item = Equipped((ItemSlot)slot);
                if (item == null) continue;
                stats.MaxHealth += item.health;
                stats.Damage += item.attack;
                stats.Armor += item.defense;
            }
            int passiveRank = Profile.skillRanks != null && Profile.skillRanks.Length > 3 ? Clamp(Profile.skillRanks[3], 0, 3) : 0;
            if (passiveRank > 0)
            {
                if (Profile.heroClass == HeroClass.Vanguard)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .08f : passiveRank == 2 ? .14f : .22f);
                    stats.Armor += passiveRank == 1 ? 2f : passiveRank == 2 ? 4f : 7f;
                }
                else if (Profile.heroClass == HeroClass.Arcanist)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .06f : passiveRank == 2 ? .11f : .18f);
                    stats.MaxHealth *= 1f + (passiveRank == 1 ? .04f : passiveRank == 2 ? .07f : .10f);
                }
                else if (Profile.heroClass == HeroClass.Summoner)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .06f : passiveRank == 2 ? .11f : .18f);
                }
                else
                {
                    stats.CritChance = Math.Min(1f, stats.CritChance + (passiveRank == 1 ? .04f : passiveRank == 2 ? .07f : .12f));
                    stats.MoveSpeed *= 1f + (passiveRank == 1 ? .03f : passiveRank == 2 ? .06f : .10f);
                }
            }
            FashionData wings = EquippedFashion(FashionSlot.Wings);
            if (wings != null)
            {
                int rank = (int)wings.rarity;
                stats.MaxHealth *= 1f + WingHealthPercents[rank] / 100f;
                stats.Armor *= 1f + WingArmorPercents[rank] / 100f;
            }
            FashionData weaponFashion = EquippedFashion(FashionSlot.Weapon);
            if (weaponFashion != null)
            {
                float bonus = WeaponPercents[(int)weaponFashion.rarity] / 100f;
                stats.Damage *= 1f + bonus;
                stats.CritChance = Math.Min(1f, stats.CritChance * (1f + bonus));
            }
            if (Profile.masteryRanks != null && Profile.masteryRanks.Length >= 3)
            {
                stats.Damage *= 1f + Clamp(Profile.masteryRanks[0], 0, MaximumMasteryRank) * .005f;
                stats.MaxHealth *= 1f + Clamp(Profile.masteryRanks[1], 0, MaximumMasteryRank) * .0075f;
                stats.Armor += Clamp(Profile.masteryRanks[2], 0, MaximumMasteryRank) * .75f;
            }
            return stats;
        }

        public FashionData EquippedFashion(FashionSlot slot)
        {
            string id = slot == FashionSlot.Wings ? Profile.wingsFashionId : Profile.weaponFashionId;
            return Profile.fashions == null ? null : Profile.fashions.Find(value => value != null && value.id == id && value.slot == slot);
        }

        public bool EquipFashion(string id)
        {
            FashionData fashion = Profile.fashions == null ? null : Profile.fashions.Find(value => value != null && value.id == id);
            if (fashion == null) return Fail("尚未获得这件时装。");
            if (fashion.slot == FashionSlot.Wings) Profile.wingsFashionId = id;
            else Profile.weaponFashionId = id;
            Commit();
            return true;
        }

        public bool UnequipFashion(FashionSlot slot)
        {
            if (slot == FashionSlot.Wings) Profile.wingsFashionId = null;
            else if (slot == FashionSlot.Weapon) Profile.weaponFashionId = null;
            else return Fail("无效的时装部位。");
            Commit();
            return true;
        }

        public bool SetSpecialization(ElementalistSpecialization specialization, bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地免费切换专精。");
            if (Profile.heroClass != HeroClass.Arcanist) return Fail("只有元素师可切换冰火专精。");
            if (!Enum.IsDefined(typeof(ElementalistSpecialization), specialization)) return Fail("无效的专精。");
            Profile.specialization = specialization;
            Commit();
            return true;
        }

        public bool HasMechanic(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic) ||
                BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return false;
            ItemData equipped = Equipped(BuildCatalog.MechanicSlot(mechanic));
            return equipped != null && equipped.mechanic == mechanic;
        }

        public bool HasDiscoveredMechanic(EquipmentMechanic mechanic)
        {
            return Profile.discoveredMechanics != null && Profile.discoveredMechanics.Contains(mechanic);
        }

        public bool SetItemLocked(string id, bool locked)
        {
            ItemData item = FindItem(id);
            if (item == null && Profile.pendingLoot != null) item = Profile.pendingLoot.Find(value => value != null && value.id == id);
            if (item == null && Profile.recoveryLoot != null) item = Profile.recoveryLoot.Find(value => value != null && value.id == id);
            if (item == null) return Fail("找不到这件装备。");
            item.locked = locked;
            Commit();
            return true;
        }

        public bool SetAutoSell(Rarity rarity, bool enabled)
        {
            if (rarity == Rarity.Common) Profile.autoSellCommon = enabled;
            else if (rarity == Rarity.Rare) Profile.autoSellRare = enabled;
            else return Fail("只可自动出售普通或稀有装备；机制、锁定和已强化装备始终受保护。");
            Commit();
            return true;
        }

        public int BulkSellLowQuality()
        {
            int sold = 0;
            for (int index = Profile.inventory.Count - 1; index >= 0; index--)
            {
                ItemData item = Profile.inventory[index];
                if (item == null || IsEquipped(Profile, item.id) || IsProtectedLoot(item) || item.rarity > Rarity.Rare) continue;
                Profile.gold = (int)Math.Min(MaximumGold, (long)Profile.gold + SellValue(item));
                Profile.inventory.RemoveAt(index);
                sold++;
            }
            if (sold > 0) Commit();
            else Fail("没有可批量出售的普通/稀有装备；穿戴、锁定、机制和强化装备受保护。");
            return sold;
        }

        public static bool IsProtectedLoot(ItemData item)
        {
            return item != null && (item.locked || item.mechanic != EquipmentMechanic.None || item.rarity >= Rarity.Epic || item.upgradeLevel > 0);
        }

        public bool CanReceiveProtectedLoot
        {
            get { return Profile.inventory.Count < InventoryCapacity || Profile.pendingLoot.Count < PendingLootCapacity; }
        }

        public bool ClaimPendingLoot(string id)
        {
            ItemData item = Profile.pendingLoot.Find(value => value != null && value.id == id);
            if (item == null) return Fail("找不到待领取的装备。");
            if (Profile.inventory.Count >= InventoryCapacity) return Fail("背包已满，请先腾出位置再领取。");
            // Claims deliberately bypass pickup autosell: this is a player's explicit item claim.
            int pendingIndex = Profile.pendingLoot.IndexOf(item);
            Profile.pendingLoot.Remove(item);
            Profile.inventory.Add(item);
            string failure;
            if (!TryWriteProfile(Profile, savePath, false, out failure))
            {
                Profile.inventory.Remove(item);
                Profile.pendingLoot.Insert(Math.Min(pendingIndex, Profile.pendingLoot.Count), item);
                return Fail(failure);
            }
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public int ClaimAllPendingLoot()
        {
            int claimed = 0;
            var moved = new List<ItemData>();
            while (Profile.pendingLoot.Count > 0 && Profile.inventory.Count < InventoryCapacity)
            {
                ItemData item = Profile.pendingLoot[0];
                Profile.pendingLoot.RemoveAt(0);
                Profile.inventory.Add(item);
                moved.Add(item);
                claimed++;
            }
            if (claimed > 0)
            {
                string failure;
                if (!TryWriteProfile(Profile, savePath, false, out failure))
                {
                    foreach (ItemData item in moved) Profile.inventory.Remove(item);
                    Profile.pendingLoot.InsertRange(0, moved);
                    Fail(failure);
                    return 0;
                }
                LastError = string.Empty;
                RaiseChanged();
            }
            else Fail(Profile.pendingLoot.Count == 0 ? "没有待领取装备。" : "背包已满，请先腾出位置。");
            return claimed;
        }

        /// <summary>Emergency exit/death/quit capture. A single active expedition can
        /// populate this bounded mailbox; entry stays blocked until it is emptied.</summary>
        public bool PreserveGroundLoot(IEnumerable<ItemData> items)
        {
            if (items == null) return Fail("无法读取待保管的地面装备。");
            var known = new HashSet<string>(collectedLootIds, StringComparer.Ordinal);
            foreach (ItemData item in Profile.inventory) known.Add(item.id);
            foreach (ItemData item in Profile.pendingLoot) known.Add(item.id);
            foreach (ItemData item in Profile.recoveryLoot) known.Add(item.id);
            var incoming = new List<ItemData>();
            foreach (ItemData item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80 ||
                    !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity) ||
                    !Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic)) return Fail("地面装备数据无效；尚未收取或删除任何装备。");
                if (known.Add(item.id)) incoming.Add(item);
            }
            if (incoming.Count == 0) { LastError = string.Empty; return true; }
            if (incoming.Count > RecoveryLootCapacity - Profile.recoveryLoot.Count)
                return Fail("临时保管栏已满，尚未删除地面装备；请领取保管装备后再离开。");
            GameProfile candidate = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
            foreach (ItemData item in incoming)
                candidate.recoveryLoot.Add(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item, true)));
            string failure;
            if (!TryWriteProfile(candidate, savePath, false, out failure)) return Fail(failure);
            Profile = candidate;
            foreach (ItemData item in incoming) collectedLootIds.Add(item.id);
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public bool ClaimRecoveryLoot(string id)
        {
            if (!Profile.recoveryLoot.Exists(item => item.id == id)) return Fail("找不到临时保管的装备。");
            if (Profile.inventory.Count >= InventoryCapacity) return Fail("背包已满，请先腾出位置再领取保管装备。");
            GameProfile candidate = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
            ItemData item = candidate.recoveryLoot.Find(value => value.id == id);
            candidate.recoveryLoot.Remove(item);
            candidate.inventory.Add(item);
            string failure;
            if (!TryWriteProfile(candidate, savePath, false, out failure)) return Fail(failure);
            Profile = candidate;
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public int ClaimAllRecoveryLoot()
        {
            int count = Math.Min(Profile.recoveryLoot.Count, Math.Max(0, InventoryCapacity - Profile.inventory.Count));
            if (count == 0)
            {
                Fail(Profile.recoveryLoot.Count == 0 ? "没有临时保管的装备。" : "背包已满，请先腾出位置。");
                return 0;
            }
            GameProfile candidate = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
            candidate.inventory.AddRange(candidate.recoveryLoot.GetRange(0, count));
            candidate.recoveryLoot.RemoveRange(0, count);
            string failure;
            if (!TryWriteProfile(candidate, savePath, false, out failure)) { Fail(failure); return 0; }
            Profile = candidate;
            LastError = string.Empty;
            RaiseChanged();
            return count;
        }

        public ItemData CreateMechanicItem(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic)) return null;
            int level = Clamp(Profile.level, 1, MaximumLevel);
            ItemSlot slot = BuildCatalog.MechanicSlot(mechanic);
            var item = new ItemData { id = Guid.NewGuid().ToString("N"), name = BuildCatalog.MechanicName(mechanic),
                level = level, rarity = Rarity.Epic, slot = slot, mechanic = mechanic, locked = true };
            SetRolledStats(item);
            EnsureUpgradeBasis(item);
            return item;
        }

        public bool ClaimFirstClearReward(EquipmentMechanic mechanic)
        {
            if (!Profile.pendingFirstClearReward || Profile.firstClearRewardClaimed) return Fail("当前没有首通自选奖励。");
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic) ||
                BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return Fail("请选择本职业的机制装备。");
            if (!CanReceiveProtectedLoot) return Fail("背包与待领取栏均已满；首通选择保留，请先腾出位置。");
            Profile.firstClearRewardClaimed = true;
            Profile.pendingFirstClearReward = false;
            if (CollectLoot(CreateMechanicItem(mechanic))) return true;
            Profile.firstClearRewardClaimed = false;
            Profile.pendingFirstClearReward = true;
            return false;
        }

        public bool ExchangeMechanic(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic) ||
                BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return Fail("只能兑换本职业的机制装备。");
            if (Profile.mechanicMaterials < MechanicExchangeCost) return Fail("需要12枚星烬碎片；每次遗迹通关获得3枚。");
            if (!CanReceiveProtectedLoot) return Fail("背包与待领取栏均已满，请先腾出位置；尚未扣除碎片。");
            Profile.mechanicMaterials -= MechanicExchangeCost;
            if (CollectLoot(CreateMechanicItem(mechanic))) return true;
            Profile.mechanicMaterials += MechanicExchangeCost;
            return false;
        }

        public string MasteryLockReason(MasteryType mastery)
        {
            if (!Enum.IsDefined(typeof(MasteryType), mastery)) return "无效的精通。";
            if (Profile.level < MaximumLevel) return "100级开放精通，消耗剩余技能点。";
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                if (Profile.skillRanks[skill] < 3) return "先将十个技能全部觉醒，再分配剩余69点精通。";
            if (Profile.masteryRanks[(int)mastery] >= MaximumMasteryRank) return "该精通已达到23点上限。";
            if (Profile.skillPoints < 1) return "需要1点剩余技能点。";
            return string.Empty;
        }

        public bool LearnMastery(MasteryType mastery)
        {
            string reason = MasteryLockReason(mastery);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            Profile.masteryRanks[(int)mastery]++;
            Profile.skillPoints--;
            Commit();
            return true;
        }

        public bool ResetMastery(bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地免费重置精通。");
            Profile.masteryRanks = new int[3];
            Commit(); // Validation reconstructs exactly the remaining lifetime point budget.
            return true;
        }

        public void PrepareDungeonChest()
        {
            Profile.pendingFashionChest = true;
            if (Profile.clearedRuns > Profile.materialRewardedClears)
            {
                Profile.mechanicMaterials = Clamp(Profile.mechanicMaterials + 3, 0, 999999);
                Profile.materialRewardedClears = Profile.clearedRuns;
            }
            if (!Profile.firstClearRewardClaimed && Profile.clearedRuns > 0) Profile.pendingFirstClearReward = true;
            Commit();
        }

        // Exactly-once reward transaction: roll and grant in a detached snapshot,
        // durably save its receipt, then publish it to the animation/UI. A failed write
        // cannot consume the chest, expose a reward, mutate currency or emit a change.
        public string OpenDungeonChest(int choice)
        {
            if (choice < 0 || choice >= 3 || !Profile.pendingFashionChest)
            {
                Fail("当前没有可开启的通关宝箱。");
                return null;
            }
            if (Profile.pendingChestReveal)
            {
                Fail("请先收起上一次的宝箱奖励展示；该奖励已保存，不会重新抽取。");
                return null;
            }
            GameProfile candidate = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
            candidate.pendingFashionChest = false;
            var receipt = new ChestReward { id = Guid.NewGuid().ToString("N"), choice = choice, gold = 60 + random.Next(41), name = "金币" };
            Rarity? rarity = RollFashionRarity(random.Next(100));
            if (!rarity.HasValue) receipt.summary = "宝箱 " + (choice + 1) + "：获得 " + receipt.gold + " 金币";
            else
            {
                FashionSlot slot = (FashionSlot)random.Next(2);
                string id = "fashion-" + (int)slot + "-" + (int)rarity.Value;
                FashionData owned = candidate.fashions.Find(value => value != null && value.id == id);
                receipt.rarityIndex = (int)rarity.Value;
                receipt.slotIndex = (int)slot;
                receipt.name = FashionName(slot, rarity.Value);
                receipt.duplicate = owned != null;
                if (receipt.duplicate)
                {
                    int duplicateGold = new[] { 40, 100, 250, 800 }[(int)rarity.Value];
                    receipt.summary = "宝箱 " + (choice + 1) + "：" + receipt.name + " 已拥有，转化 " + duplicateGold + " 金币；另得 " + receipt.gold + " 金币";
                    receipt.gold += duplicateGold;
                }
                else
                {
                    candidate.fashions.Add(new FashionData { id = id, slot = slot, rarity = rarity.Value, name = receipt.name });
                    receipt.summary = "宝箱 " + (choice + 1) + "：获得" + GameBalance.RarityName(rarity.Value) + "时装「" + receipt.name + "」及 " + receipt.gold + " 金币";
                }
            }
            candidate.gold = (int)Math.Min(MaximumGold, (long)candidate.gold + receipt.gold);
            candidate.lastChestReward = receipt;
            candidate.pendingChestReveal = true;
            string failure;
            if (!TryWriteProfile(candidate, savePath, false, out failure))
            {
                Fail(failure);
                return null;
            }
            Profile = candidate;
            LastError = string.Empty;
            RaiseChanged();
            return receipt.summary;
        }

        public bool AcknowledgeChestReward()
        {
            if (!Profile.pendingChestReveal) return Fail("当前没有待展示的宝箱奖励。");
            GameProfile candidate = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));
            candidate.pendingChestReveal = false;
            string failure;
            if (!TryWriteProfile(candidate, savePath, false, out failure)) return Fail(failure);
            Profile = candidate;
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public void GrantExperience(int amount)
        {
            if (amount <= 0 || Profile.level >= MaximumLevel) return;
            long experience = (long)Profile.xp + amount;
            var gainedLevels = new List<int>();
            while (Profile.level < MaximumLevel && experience >= GameBalance.XpToNext(Profile.level))
            {
                experience -= GameBalance.XpToNext(Profile.level);
                Profile.level++;
                Profile.skillPoints++;
                gainedLevels.Add(Profile.level);
            }
            Profile.xp = Profile.level == MaximumLevel ? 0 : (int)experience;
            Commit();
            // Subscribers observe the final, fully saved profile even for multiple level-ups.
            foreach (int level in gainedLevels)
                if (LeveledUp != null) LeveledUp(level);
        }

        public void AddGold(int amount)
        {
            Profile.gold = (int)Math.Max(0L, Math.Min(MaximumGold, (long)Profile.gold + amount));
            Commit();
        }

        public ItemData CreateLoot(int level, bool boss)
        {
            ItemData item = RollLoot(level, boss);
            CollectLoot(item);
            return item;
        }

        /// <summary>Generate an identified drop without putting it into the bag or saving.</summary>
        public ItemData RollLoot(int level, bool boss)
        {
            level = Clamp(level, 1, MaximumLevel);
            int roll = random.Next(100);
            Rarity rarity = boss
                ? (roll < 55 ? Rarity.Rare : roll < 92 ? Rarity.Epic : Rarity.Legendary)
                : (roll < 54 ? Rarity.Common : roll < 85 ? Rarity.Rare : roll < 98 ? Rarity.Epic : Rarity.Legendary);
            ItemSlot slot = (ItemSlot)random.Next(3);
            var item = new ItemData
            {
                id = Guid.NewGuid().ToString("N"),
                slot = slot,
                rarity = rarity,
                level = level,
                name = new[] { "旅者", "苍蓝", "星辉", "烬王" }[(int)rarity] + ItemBaseName(slot, Profile.heroClass)
            };
            EquipmentMechanic[] mechanics = BuildCatalog.MechanicsFor(Profile.heroClass);
            if (mechanics.Length > 0 && ((boss && random.Next(100) < 25) || (!boss && rarity >= Rarity.Epic && random.Next(100) < 12)))
            {
                item.mechanic = mechanics[random.Next(mechanics.Length)];
                item.slot = BuildCatalog.MechanicSlot(item.mechanic);
                item.name = BuildCatalog.MechanicName(item.mechanic);
                item.locked = true;
            }
            SetRolledStats(item);
            EnsureUpgradeBasis(item);
            return item;
        }

        private static void SetRolledStats(ItemData item)
        {
            float multiplier = new[] { 1f, 1.35f, 1.8f, 2.5f }[(int)item.rarity];
            int level = item.level;
            item.attack = item.defense = item.health = 0;
            if (item.slot == ItemSlot.Weapon) item.attack = Round((5 + level * 2.5f) * multiplier);
            else if (item.slot == ItemSlot.Armor)
            {
                item.defense = Round((3 + level * 1.2f) * multiplier);
                item.health = Round((10 + level * 4) * multiplier);
            }
            else
            {
                item.attack = Round((2 + level) * multiplier);
                item.health = Round((6 + level * 3) * multiplier);
            }
        }

        /// <summary>Protected overflow is persisted for claiming. A full pending queue rejects
        /// acquisition without consuming the drop; the caller must retain it or block departure.</summary>
        public bool CollectLoot(ItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80 || !Enum.IsDefined(typeof(ItemSlot), item.slot) ||
                !Enum.IsDefined(typeof(Rarity), item.rarity) || !Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic))
                return Fail("掉落装备无效。");
            if (collectedLootIds.Contains(item.id) || FindItem(item.id) != null || Profile.pendingLoot.Exists(value => value != null && value.id == item.id) ||
                Profile.recoveryLoot.Exists(value => value != null && value.id == item.id))
                return Fail("这件装备已经拾取。");
            bool overflow = Profile.inventory.Count >= InventoryCapacity;
            bool protectedLoot = IsProtectedLoot(item);
            if (overflow && protectedLoot && Profile.pendingLoot.Count >= PendingLootCapacity)
                return Fail("背包与待领取栏均已满；珍贵装备仍在地上，请先整理再离开。");
            bool sold = !protectedLoot && (overflow || (item.rarity == Rarity.Common && Profile.autoSellCommon) || (item.rarity == Rarity.Rare && Profile.autoSellRare));
            int previousGold = Profile.gold;
            bool newlyDiscovered = item.mechanic != EquipmentMechanic.None && !Profile.discoveredMechanics.Contains(item.mechanic);
            if (sold) Profile.gold = (int)Math.Min(MaximumGold, (long)Profile.gold + SellValue(item));
            else if (overflow) Profile.pendingLoot.Add(item);
            else Profile.inventory.Add(item);
            if (newlyDiscovered) Profile.discoveredMechanics.Add(item.mechanic);
            string failure;
            if (!TryWriteProfile(Profile, savePath, false, out failure))
            {
                Profile.gold = previousGold;
                Profile.inventory.Remove(item);
                Profile.pendingLoot.Remove(item);
                if (newlyDiscovered) Profile.discoveredMechanics.Remove(item.mechanic);
                return Fail(failure); // The world still owns the item and may retry safely.
            }
            collectedLootIds.Add(item.id);
            LastError = string.Empty;
            RaiseChanged();
            if (sold) LastError = (overflow ? "背包已满，" : "低品质自动出售：") + item.name + "已自动出售，获得 " + SellValue(item) + " 金币。";
            else if (overflow) LastError = "背包已满，" + item.name + "已保护至待领取栏（" + Profile.pendingLoot.Count + "/" + PendingLootCapacity + "）。";
            return true;
        }

        public ItemData Equipped(ItemSlot slot)
        {
            string id = slot == ItemSlot.Weapon ? Profile.weaponId : slot == ItemSlot.Armor ? Profile.armorId : slot == ItemSlot.Relic ? Profile.relicId : null;
            ItemData item = FindItem(id);
            return item != null && item.slot == slot ? item : null;
        }

        public bool Equip(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (item.level > Profile.level) return Fail("需要角色等级 " + item.level + " 才能装备。");
            ItemData previous = Equipped(item.slot);
            ItemData previousState = previous == null ? null : PreviewUpgrade(previous, previous.upgradeLevel);
            ItemData itemState = PreviewUpgrade(item, item.upgradeLevel);
            string previousId = previous == null ? null : previous.id;
            if (previous != null && previous != item) ApplyUpgradeRank(previous, 0);
            EnsureUpgradeBasis(item);
            ApplyUpgradeRank(item, SlotUpgradeRank(item.slot));
            SetEquipped(Profile, item);
            string failure;
            if (!TryWriteProfile(Profile, savePath, false, out failure))
            {
                RestoreUpgradeState(item, itemState);
                if (previous != null) RestoreUpgradeState(previous, previousState);
                if (item.slot == ItemSlot.Weapon) Profile.weaponId = previousId;
                else if (item.slot == ItemSlot.Armor) Profile.armorId = previousId;
                else Profile.relicId = previousId;
                return Fail(failure);
            }
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public int SlotUpgradeRank(ItemSlot slot)
        {
            int index = (int)slot;
            return index < 0 || index > 2 || Profile.slotUpgradeRanks == null || index >= Profile.slotUpgradeRanks.Length
                ? 0 : Clamp(Profile.slotUpgradeRanks[index], 0, MaximumUpgrade);
        }

        /// <summary>Prospective equipped stats at the permanent slot rank. Never
        /// mutates the item, profile or disk, including legacy baseline metadata.</summary>
        public ItemData PreviewEquippedItem(ItemData item)
        {
            return item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) ? null : PreviewUpgrade(item, SlotUpgradeRank(item.slot));
        }

        public bool Sell(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (IsEquipped(Profile, item.id)) return Fail("请先替换身上的装备，再出售。");
            if (item.locked) return Fail("装备已锁定，请先手动解锁再出售。");
            Profile.gold = (int)Math.Min(MaximumGold, (long)Profile.gold + SellValue(item));
            Profile.inventory.Remove(item);
            Commit();
            return true;
        }

        public bool Upgrade(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            int rank = SlotUpgradeRank(item.slot);
            if (rank >= MaximumUpgrade) return Fail("该部位已达到强化上限 +10；换装会自动继承。");
            int cost = UpgradeCost(item);
            if (Profile.gold < cost) return Fail("金币不足，部位强化需要 " + cost + " 金币。");
            ItemData equipped = Equipped(item.slot);
            ItemData oldEquipped = equipped == null ? null : PreviewUpgrade(equipped, equipped.upgradeLevel);
            int oldGold = Profile.gold;
            Profile.gold -= cost;
            Profile.slotUpgradeRanks[(int)item.slot] = rank + 1;
            if (equipped != null) { EnsureUpgradeBasis(equipped); ApplyUpgradeRank(equipped, rank + 1); }
            string failure;
            if (!TryWriteProfile(Profile, savePath, false, out failure))
            {
                Profile.gold = oldGold;
                Profile.slotUpgradeRanks[(int)item.slot] = rank;
                if (equipped != null) RestoreUpgradeState(equipped, oldEquipped);
                return Fail(failure);
            }
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        /// <summary>Compatibility entry point for old UI callers. Training now
        /// belongs to the slot and is applied automatically when equipment changes.</summary>
        public bool TransferUpgrade(string sourceId, string targetId)
        {
            return Fail("强化等级已绑定装备部位；更换装备会自动继承，无需单独转移。");
        }

        private static void RestoreUpgradeState(ItemData item, ItemData state)
        {
            item.attack = state.attack; item.defense = state.defense; item.health = state.health;
            item.upgradeLevel = state.upgradeLevel; item.upgradeBaseInitialized = state.upgradeBaseInitialized;
            item.baseAttack = state.baseAttack; item.baseDefense = state.baseDefense; item.baseHealth = state.baseHealth;
            item.upgradeAnchorLevel = state.upgradeAnchorLevel; item.upgradeAnchorAttack = state.upgradeAnchorAttack;
            item.upgradeAnchorDefense = state.upgradeAnchorDefense; item.upgradeAnchorHealth = state.upgradeAnchorHealth;
        }

        /// <summary>Returns an independent preview; never mutates items, gold, saves or events.</summary>
        public ItemData PreviewUpgrade(ItemData item, int rank)
        {
            if (item == null || rank < 0 || rank > MaximumUpgrade) return null;
            var preview = new ItemData
            {
                id = item.id, name = item.name, slot = item.slot, rarity = item.rarity, level = item.level,
                mechanic = item.mechanic, locked = item.locked,
                attack = item.attack, defense = item.defense, health = item.health, upgradeLevel = item.upgradeLevel,
                upgradeBaseInitialized = item.upgradeBaseInitialized,
                baseAttack = item.baseAttack, baseDefense = item.baseDefense, baseHealth = item.baseHealth,
                upgradeAnchorLevel = item.upgradeAnchorLevel, upgradeAnchorAttack = item.upgradeAnchorAttack,
                upgradeAnchorDefense = item.upgradeAnchorDefense, upgradeAnchorHealth = item.upgradeAnchorHealth
            };
            EnsureUpgradeBasis(preview);
            ApplyUpgradeRank(preview, rank);
            return preview;
        }

        private static void ApplyUpgradeRank(ItemData item, int rank)
        {
            item.upgradeLevel = Clamp(rank, 0, MaximumUpgrade);
            item.attack = UpgradeValue(item.baseAttack, item.upgradeAnchorAttack, item.upgradeAnchorLevel, item.upgradeLevel, 1, MaximumEquipmentStat);
            item.defense = UpgradeValue(item.baseDefense, item.upgradeAnchorDefense, item.upgradeAnchorLevel, item.upgradeLevel, 1, MaximumEquipmentStat);
            item.health = UpgradeValue(item.baseHealth, item.upgradeAnchorHealth, item.upgradeAnchorLevel, item.upgradeLevel, 2, MaximumEquipmentHealth);
        }

        private static int UpgradeValue(int basis, int anchor, int anchorRank, int rank, int minimumIncrease, int cap)
        {
            // A legacy value can be capped or not exactly invertible. Its recorded
            // anchor guarantees returning to the original rank restores it exactly.
            // Normal legacy values are exact on both sides of the same growth curve.
            return rank >= anchorRank ? GrowUpgradeStat(anchor, rank - anchorRank, minimumIncrease, cap)
                : GrowUpgradeStat(basis, rank, minimumIncrease, cap);
        }

        private static int GrowUpgradeStat(int value, int ranks, int minimumIncrease, int cap)
        {
            if (value <= 0) return 0;
            for (int i = 0; i < ranks; i++)
                value = Math.Min(cap, value + Math.Max(minimumIncrease, Round(value * .12f)));
            return value;
        }

        private static int RecoverUpgradeBase(int value, int rank, int minimumIncrease)
        {
            if (rank <= 0 || value <= 0) return value;
            // The uncapped integer growth function is strictly increasing for
            // positive values. Binary search finds the exact old base when it
            // exists; otherwise use the greatest conservative base below it.
            // Never invert the capped function, whose plateau would invent a base.
            int low = 0, high = value, best = 0;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int grown = GrowUpgradeStat(middle, rank, minimumIncrease, int.MaxValue);
                if (grown <= value) { best = middle; low = middle + 1; }
                else high = middle - 1;
            }
            return best;
        }

        private static bool ValidUpgradeBasis(int basis, int anchor, int anchorRank, int minimumIncrease, int cap)
        {
            return basis >= 0 && basis <= cap && anchor >= 0 && anchor <= cap &&
                basis == RecoverUpgradeBase(anchor, anchorRank, minimumIncrease);
        }

        private static void EnsureUpgradeBasis(ItemData item)
        {
            item.upgradeLevel = Clamp(item.upgradeLevel, 0, MaximumUpgrade);
            item.attack = Clamp(item.attack, 0, MaximumEquipmentStat);
            item.defense = Clamp(item.defense, 0, MaximumEquipmentStat);
            item.health = Clamp(item.health, 0, MaximumEquipmentHealth);
            int anchorRank = item.upgradeAnchorLevel;
            bool valid = item.upgradeBaseInitialized && anchorRank >= 0 && anchorRank <= MaximumUpgrade &&
                ValidUpgradeBasis(item.baseAttack, item.upgradeAnchorAttack, anchorRank, 1, MaximumEquipmentStat) &&
                ValidUpgradeBasis(item.baseDefense, item.upgradeAnchorDefense, anchorRank, 1, MaximumEquipmentStat) &&
                ValidUpgradeBasis(item.baseHealth, item.upgradeAnchorHealth, anchorRank, 2, MaximumEquipmentHealth);
            if (valid &&
                item.attack == UpgradeValue(item.baseAttack, item.upgradeAnchorAttack, anchorRank, item.upgradeLevel, 1, MaximumEquipmentStat) &&
                item.defense == UpgradeValue(item.baseDefense, item.upgradeAnchorDefense, anchorRank, item.upgradeLevel, 1, MaximumEquipmentStat) &&
                item.health == UpgradeValue(item.baseHealth, item.upgradeAnchorHealth, anchorRank, item.upgradeLevel, 2, MaximumEquipmentHealth)) return;

            // Initialize old saves (or repair inconsistent metadata) without changing
            // their currently visible attributes. The anchor remains immutable across
            // future upgrades, rank transfers, previews and save/load round trips.
            item.upgradeAnchorLevel = item.upgradeLevel;
            item.upgradeAnchorAttack = item.attack;
            item.upgradeAnchorDefense = item.defense;
            item.upgradeAnchorHealth = item.health;
            item.baseAttack = RecoverUpgradeBase(item.attack, item.upgradeLevel, 1);
            item.baseDefense = RecoverUpgradeBase(item.defense, item.upgradeLevel, 1);
            item.baseHealth = RecoverUpgradeBase(item.health, item.upgradeLevel, 2);
            item.upgradeBaseInitialized = true;
        }

        public int UpgradeCost(ItemData item)
        {
            if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot)) return 0;
            int rank = SlotUpgradeRank(item.slot);
            if (rank >= MaximumUpgrade) return 0;
            // Every item of a slot trains the same permanent slot rank. Price never
            // depends on a donor's level, rarity, or cached item upgradeLevel.
            int slotPrice = item.slot == ItemSlot.Weapon ? 60 : item.slot == ItemSlot.Armor ? 50 : 45;
            return slotPrice * (rank + 1);
        }

        public int SellValue(ItemData item)
        {
            if (item == null) return 0;
            return (8 + Clamp(item.level, 1, MaximumLevel) * 4) * (Clamp((int)item.rarity, 0, 3) + 1);
        }

        public static float EquipmentScore(ItemData item)
        {
            return item == null ? 0 : item.attack * 5f + item.defense * 3f + item.health * .2f;
        }

        public bool LearnSkill(int slot)
        {
            string reason = SkillLockReason(slot);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            Profile.skillRanks[slot]++;
            Profile.skillPoints--;
            Commit();
            return true;
        }

        public string SkillLockReason(int slot)
        {
            if (slot < 0 || slot >= GameBalance.SkillCount) return "无效的技能。";
            if (Profile.skillRanks[slot] >= 3) return "已达到最高等级 3。";
            int nextRank = Profile.skillRanks[slot] + 1;
            int required = GameBalance.SkillRankRequiredLevel(slot, nextRank);
            if (Profile.level < required) return "角色达到 " + required + " 级可学习技能第 " + nextRank + " 阶。";
            if (nextRank == 1 && !PrerequisitesMet(slot)) return "请先点亮前置技能。" + GameBalance.PrerequisiteDescription(Profile.heroClass, slot);
            if (Profile.skillPoints < 1) return "需要 1 点技能点，升级后获得。";
            return string.Empty;
        }

        public bool PrerequisitesMet(int skill)
        {
            if (skill < 0 || skill >= GameBalance.SkillCount) return false;
            foreach (int parent in GameBalance.SkillPrerequisites[skill])
                if (Profile.skillRanks[parent] < 1) return false;
            return true;
        }

        public bool AssignSkill(int hotbarSlot, int skillIndex)
        {
            if (hotbarSlot < 0 || hotbarSlot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (skillIndex < -1 || skillIndex >= GameBalance.SkillCount) return Fail("无效的技能。");
            if (skillIndex >= 0 && GameBalance.IsPassive(skillIndex)) return Fail("被动技能学习后自动生效，无需装备到快捷栏。");
            if (skillIndex >= 0 && Profile.skillRanks[skillIndex] < 1) return Fail("请先学习这个技能，再装备到快捷栏。");
            int pageStart = Profile.hotbarPage * GameBalance.HotbarSize;
            int target = pageStart + hotbarSlot;
            if (skillIndex >= 0)
            {
                for (int slot = pageStart; slot < pageStart + GameBalance.HotbarSize; slot++)
                {
                    if (slot == target || Profile.equippedSkills[slot] != skillIndex) continue;
                    Profile.equippedSkills[slot] = Profile.equippedSkills[target];
                    break;
                }
            }
            Profile.equippedSkills[target] = skillIndex;
            Commit();
            return true;
        }

        public bool AssignConsumable(int hotbarSlot)
        {
            if (hotbarSlot < 0 || hotbarSlot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (!HasValidHotbarData()) return Fail("快捷栏数据无效。");
            int pageStart = Profile.hotbarPage * GameBalance.HotbarSize;
            int target = pageStart + hotbarSlot;
            if (Profile.equippedSkills[target] == GameBalance.HotbarPotion) return Fail("生命药水已经位于这个快捷栏位置。");
            for (int slot = pageStart; slot < pageStart + GameBalance.HotbarSize; slot++)
            {
                if (Profile.equippedSkills[slot] != GameBalance.HotbarPotion) continue;
                int displaced = Profile.equippedSkills[target];
                Profile.equippedSkills[slot] = IsUsableHotbarEntry(displaced) ? displaced : -1;
                break;
            }
            Profile.equippedSkills[target] = GameBalance.HotbarPotion;
            Commit();
            return true;
        }

        private bool HasValidHotbarData()
        {
            return Profile.hotbarPage >= 0 && Profile.hotbarPage < GameBalance.HotbarPages && Profile.equippedSkills != null &&
                Profile.equippedSkills.Length >= GameBalance.HotbarPages * GameBalance.HotbarSize && Profile.skillRanks != null && Profile.skillRanks.Length >= GameBalance.SkillCount;
        }

        private bool IsUsableHotbarEntry(int entry)
        {
            return entry == GameBalance.HotbarPotion ||
                (entry >= 0 && entry < GameBalance.SkillCount && !GameBalance.IsPassive(entry) && Profile.skillRanks[entry] > 0);
        }

        /// <summary>Move or swap learned active skills and consumables within the selected hotbar page.</summary>
        public bool MoveHotbarSkill(int sourceSlot, int targetSlot)
        {
            if (sourceSlot < 0 || sourceSlot >= GameBalance.HotbarSize || targetSlot < 0 || targetSlot >= GameBalance.HotbarSize)
                return Fail("无效的快捷栏位置。");
            if (sourceSlot == targetSlot) return Fail("已经位于这个快捷栏位置。");
            if (!HasValidHotbarData()) return Fail("快捷栏数据无效。");
            int pageStart = Profile.hotbarPage * GameBalance.HotbarSize;
            int source = pageStart + sourceSlot;
            int target = pageStart + targetSlot;
            int skill = Profile.equippedSkills[source];
            if (!IsUsableHotbarEntry(skill)) return Fail("只能拖动已学习的主动技能或可使用物品。");
            int displaced = Profile.equippedSkills[target];
            if (!IsUsableHotbarEntry(displaced)) displaced = -1;
            if (skill == displaced) return Fail("已经位于目标位置。");
            Profile.equippedSkills[target] = skill;
            Profile.equippedSkills[source] = displaced;
            Commit();
            return true;
        }

        public bool SetHotbarPage(int page)
        {
            if (page < 0 || page >= GameBalance.HotbarPages) return Fail("无效的快捷栏页面。");
            Profile.hotbarPage = page;
            Commit();
            return true;
        }

        public bool SetHotbarKey(int slot, int keyCode)
        {
            if (slot < 0 || slot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (!GameBalance.IsBindableKey(keyCode)) return Fail("请选择字母、数字或 F1–F12；移动、药水和面板按键不能绑定。");
            int otherSlot = Array.IndexOf(Profile.hotbarKeys, keyCode);
            if (otherSlot >= 0 && otherSlot != slot) Profile.hotbarKeys[otherSlot] = Profile.hotbarKeys[slot];
            Profile.hotbarKeys[slot] = keyCode;
            Commit();
            return true;
        }

        public bool UsePotion()
        {
            if (Profile.potions <= 0) return Fail("治疗药水已用尽，返回营地购买。");
            Profile.potions--;
            Commit();
            return true;
        }

        public bool BuyPotion()
        {
            if (Profile.potions >= 99) return Fail("药水已达到携带上限 99。");
            if (Profile.gold < PotionPrice) return Fail("购买药水需要 " + PotionPrice + " 金币。");
            Profile.gold -= PotionPrice;
            Profile.potions++;
            Commit();
            return true;
        }

        private void Commit()
        {
            Save();
            RaiseChanged();
        }

        private void RaiseChanged() { if (Changed != null) Changed(); }
        private bool Fail(string message) { LastError = message; return false; }

        private ItemData FindItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Profile.inventory.Find(item => item != null && item.id == id);
        }

        private static GameProfile CreateProfile(HeroClass heroClass)
        {
            var profile = new GameProfile { heroClass = heroClass };
            for (int slot = 0; slot < 3; slot++) AddStarterItem(profile, (ItemSlot)slot);
            return profile;
        }

        private static void AddStarterItem(GameProfile profile, ItemSlot slot)
        {
            var item = new ItemData
            {
                id = Guid.NewGuid().ToString("N"), name = "初行" + ItemBaseName(slot, profile.heroClass),
                slot = slot, rarity = Rarity.Common, level = 1,
                attack = slot == ItemSlot.Weapon ? 6 : slot == ItemSlot.Relic ? 2 : 0,
                defense = slot == ItemSlot.Armor ? 4 : 0,
                health = slot == ItemSlot.Armor ? 20 : slot == ItemSlot.Relic ? 10 : 0
            };
            EnsureUpgradeBasis(item);
            profile.inventory.Add(item);
            SetEquipped(profile, item);
        }

        private static string ItemBaseName(ItemSlot slot, HeroClass heroClass)
        {
            if (slot == ItemSlot.Armor) return "战衣";
            if (slot == ItemSlot.Relic) return "护符";
            return heroClass == HeroClass.Arcanist ? "法杖" : heroClass == HeroClass.Ranger ? "长弓" : heroClass == HeroClass.Summoner ? "法器" : "长剑";
        }

        private static void SetEquipped(GameProfile profile, ItemData item)
        {
            if (item.slot == ItemSlot.Weapon) profile.weaponId = item.id;
            else if (item.slot == ItemSlot.Armor) profile.armorId = item.id;
            else profile.relicId = item.id;
        }

        private static bool IsEquipped(GameProfile profile, string id)
        {
            return id == profile.weaponId || id == profile.armorId || id == profile.relicId;
        }

        private static bool TryReadProfile(string path, out GameProfile profile, out string error)
        {
            profile = null;
            error = string.Empty;
            try
            {
                if (!File.Exists(path)) { error = "missing"; return false; }
                var info = new FileInfo(path);
                if (info.Length == 0 || info.Length > 4 * 1024 * 1024) { error = "invalid size"; return false; }
                SaveFile data = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path, Encoding.UTF8));
                if (data == null || data.format != SaveFormat || data.version != 1 || data.profile == null || data.profile.version != 1)
                { error = "unsupported format"; return false; }
                int refundedRanks = ValidateProfile(data.profile);
                if (refundedRanks > 0) error = "部分技能阶级尚未达到新的解锁等级，已调整并返还技能点；角色与装备进度均已保留。";
                profile = data.profile;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static int ValidateProfile(GameProfile profile)
        {
            int refundedRanks = 0;
            if (!Enum.IsDefined(typeof(HeroClass), profile.heroClass)) profile.heroClass = HeroClass.Vanguard;
            profile.version = 1;
            profile.level = Clamp(profile.level, 1, MaximumLevel);
            profile.xp = profile.level >= MaximumLevel ? 0 : Clamp(profile.xp, 0, GameBalance.XpToNext(profile.level) - 1);
            profile.gold = Clamp(profile.gold, 0, MaximumGold);
            profile.potions = Clamp(profile.potions, 0, 99);
            profile.kills = Clamp(profile.kills, 0, int.MaxValue);
            profile.clearedRuns = Clamp(profile.clearedRuns, 0, 999999);
            profile.bestFloor = Clamp(profile.bestFloor, 0, 999999);
            profile.tutorialMask = Math.Max(0, profile.tutorialMask) & 15;
            if (profile.heroClass != HeroClass.Arcanist || !Enum.IsDefined(typeof(ElementalistSpecialization), profile.specialization))
                profile.specialization = ElementalistSpecialization.None;
            profile.mechanicMaterials = Clamp(profile.mechanicMaterials, 0, 999999);
            profile.materialRewardedClears = Clamp(profile.materialRewardedClears, 0, profile.clearedRuns);
            profile.pendingFirstClearReward = profile.clearedRuns > 0 && !profile.firstClearRewardClaimed;
            ChestReward receipt = profile.lastChestReward;
            if (receipt == null || string.IsNullOrWhiteSpace(receipt.id) || receipt.id.Length > 80 ||
                receipt.gold < 60 || receipt.gold > 900 || receipt.rarityIndex < -1 || receipt.rarityIndex > 3 ||
                (receipt.rarityIndex >= 0 && (receipt.slotIndex < 0 || receipt.slotIndex > 1)))
            {
                profile.lastChestReward = null;
                profile.pendingChestReveal = false;
            }
            else
            {
                receipt.choice = Clamp(receipt.choice, 0, 2);
                if (receipt.rarityIndex < 0) { receipt.slotIndex = -1; receipt.duplicate = false; receipt.name = "金币"; }
                else receipt.name = FashionName((FashionSlot)receipt.slotIndex, (Rarity)receipt.rarityIndex);
                if (string.IsNullOrWhiteSpace(receipt.summary)) receipt.summary = receipt.name + " · " + receipt.gold + " 金币";
                if (receipt.summary.Length > 240) receipt.summary = receipt.summary.Substring(0, 240);
            }
            // Version 1 saves originally held three skills. Preserve their ranks while adding
            // seven unlearned entries; the character, gear, experience and currencies stay intact.
            int[] ranks = new int[GameBalance.SkillCount];
            int remaining = profile.level - 1;
            for (int slot = 0; slot < GameBalance.SkillCount; slot++)
            {
                int rank = profile.skillRanks != null && slot < profile.skillRanks.Length ? Clamp(profile.skillRanks[slot], 0, 3) : 0;
                int previousRank = rank;
                while (rank > 0 && profile.level < GameBalance.SkillRankRequiredLevel(slot, rank)) rank--;
                refundedRanks += previousRank - rank;
                ranks[slot] = Math.Min(rank, remaining);
                remaining -= ranks[slot];
            }
            profile.skillRanks = ranks;
            int[] mastery = new int[3];
            bool allSkillsAwakened = true;
            foreach (int rank in ranks) if (rank != 3) allSkillsAwakened = false;
            if (profile.level == MaximumLevel && allSkillsAwakened)
            {
                for (int track = 0; track < mastery.Length; track++)
                {
                    int oldRank = profile.masteryRanks != null && track < profile.masteryRanks.Length ? profile.masteryRanks[track] : 0;
                    mastery[track] = Math.Min(Clamp(oldRank, 0, MaximumMasteryRank), remaining);
                    remaining -= mastery[track];
                }
            }
            profile.masteryRanks = mastery;
            // No saved free-point counter is trusted. Levels pay for both skills and
            // bounded mastery, so loading/repeated saves can neither mint nor lose points.
            profile.skillPoints = remaining;
            profile.equippedSkills = RepairLoadout(profile.equippedSkills);
            profile.hotbarKeys = RepairHotbarKeys(profile.hotbarKeys);
            if (profile.hotbarPage < 0 || profile.hotbarPage >= GameBalance.HotbarPages) profile.hotbarPage = 0;
            if (profile.inventory == null) profile.inventory = new List<ItemData>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var items = new List<ItemData>();
            foreach (ItemData item in profile.inventory)
            {
                if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80 || !ids.Add(item.id))
                {
                    item.id = Guid.NewGuid().ToString("N");
                    ids.Add(item.id);
                }
                RepairItem(item, profile.heroClass);
                items.Add(item);
            }
            profile.inventory = items;
            var pending = new List<ItemData>();
            if (profile.pendingLoot != null)
            {
                foreach (ItemData item in profile.pendingLoot)
                {
                    if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                    if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80) item.id = Guid.NewGuid().ToString("N");
                    if (!ids.Add(item.id)) continue; // A duplicated receipt never creates another copy.
                    RepairItem(item, profile.heroClass);
                    pending.Add(item);
                }
            }
            if (pending.Count > PendingLootCapacity) throw new ArgumentException("待领取栏超过安全容量；保留原存档，请从备份恢复。");
            profile.pendingLoot = pending;
            var recovery = new List<ItemData>();
            if (profile.recoveryLoot != null)
            {
                foreach (ItemData item in profile.recoveryLoot)
                {
                    if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                    if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80) item.id = Guid.NewGuid().ToString("N");
                    if (!ids.Add(item.id)) continue;
                    RepairItem(item, profile.heroClass);
                    recovery.Add(item);
                }
            }
            if (recovery.Count > RecoveryLootCapacity) throw new ArgumentException("临时保管栏超过安全容量；保留原存档，请从备份恢复。");
            profile.recoveryLoot = recovery;
            var discovered = new List<EquipmentMechanic>();
            if (profile.discoveredMechanics != null)
                foreach (EquipmentMechanic mechanic in profile.discoveredMechanics)
                    if (mechanic != EquipmentMechanic.None && Enum.IsDefined(typeof(EquipmentMechanic), mechanic) && !discovered.Contains(mechanic)) discovered.Add(mechanic);
            foreach (ItemData item in items)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            foreach (ItemData item in pending)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            foreach (ItemData item in recovery)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            profile.discoveredMechanics = discovered;
            if (profile.fashions == null) profile.fashions = new List<FashionData>();
            var validFashions = new List<FashionData>();
            var fashionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (FashionData fashion in profile.fashions)
            {
                if (fashion == null || !Enum.IsDefined(typeof(FashionSlot), fashion.slot) ||
                    !Enum.IsDefined(typeof(Rarity), fashion.rarity)) continue;
                string expectedId = "fashion-" + (int)fashion.slot + "-" + (int)fashion.rarity;
                if (!fashionIds.Add(expectedId)) continue;
                fashion.id = expectedId;
                fashion.name = FashionName(fashion.slot, fashion.rarity);
                validFashions.Add(fashion);
            }
            profile.fashions = validFashions;
            if (!validFashions.Exists(value => value.id == profile.wingsFashionId && value.slot == FashionSlot.Wings)) profile.wingsFashionId = null;
            if (!validFashions.Exists(value => value.id == profile.weaponFashionId && value.slot == FashionSlot.Weapon)) profile.weaponFashionId = null;
            // Prefer preserving existing valid equipped gear if a damaged save exceeds the cap.
            for (int slot = 0; slot < 3; slot++)
            {
                ItemSlot itemSlot = (ItemSlot)slot;
                string equippedId = slot == 0 ? profile.weaponId : slot == 1 ? profile.armorId : profile.relicId;
                ItemData equipped = items.Find(item => item.id == equippedId && item.slot == itemSlot && item.level <= profile.level);
                if (equipped == null) equipped = items.Find(item => item.slot == itemSlot && item.level <= profile.level);
                if (equipped == null) AddStarterItem(profile, itemSlot);
                else SetEquipped(profile, equipped);
            }
            InitializeSlotUpgrades(profile);
            // Repair malformed over-capacity inventories by removing ordinary overflow
            // first. Never silently truncate locked, enhanced, epic or mechanic gear.
            for (int index = items.Count - 1; items.Count > InventoryCapacity && index >= 0; index--)
            {
                if (IsEquipped(profile, items[index].id) || IsProtectedLoot(items[index])) continue;
                items.RemoveAt(index);
            }
            for (int index = items.Count - 1; items.Count > InventoryCapacity && index >= 0; index--)
            {
                if (IsEquipped(profile, items[index].id)) continue;
                if (pending.Count < PendingLootCapacity) pending.Add(items[index]);
                else if (recovery.Count < RecoveryLootCapacity) recovery.Add(items[index]);
                else throw new ArgumentException("珍贵装备超过安全容量；保留原存档，请从备份恢复。");
                items.RemoveAt(index);
            }
            return refundedRanks;
        }

        private static void InitializeSlotUpgrades(GameProfile profile)
        {
            int[] previous = profile.slotUpgradeRanks;
            bool migrate = !profile.slotUpgradesInitialized || previous == null || previous.Length != 3;
            int[] ranks = new int[3];
            for (int slot = 0; slot < ranks.Length; slot++)
                ranks[slot] = previous != null && slot < previous.Length ? Clamp(previous[slot], 0, MaximumUpgrade) : 0;
            var all = new List<ItemData>(profile.inventory);
            all.AddRange(profile.pendingLoot);
            all.AddRange(profile.recoveryLoot);
            if (migrate)
            {
                foreach (ItemData item in all)
                {
                    ranks[(int)item.slot] = Math.Max(ranks[(int)item.slot], Clamp(item.upgradeLevel, 0, MaximumUpgrade));
                    // Retain intentional protection on once-invested legacy gear;
                    // ordinary future drops do not inherit this lock or sale value.
                    if (item.upgradeLevel > 0) item.locked = true;
                }
            }
            profile.slotUpgradeRanks = ranks;
            profile.slotUpgradesInitialized = true;
            foreach (ItemData item in all)
                ApplyUpgradeRank(item, IsEquipped(profile, item.id) ? ranks[(int)item.slot] : 0);
        }

        private static void RepairItem(ItemData item, HeroClass hero)
        {
            item.level = Clamp(item.level, 1, MaximumLevel);
            item.attack = Clamp(item.attack, 0, MaximumEquipmentStat);
            item.defense = Clamp(item.defense, 0, MaximumEquipmentStat);
            item.health = Clamp(item.health, 0, MaximumEquipmentHealth);
            item.upgradeLevel = Clamp(item.upgradeLevel, 0, MaximumUpgrade);
            if (!Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic) ||
                (item.mechanic != EquipmentMechanic.None && BuildCatalog.MechanicSlot(item.mechanic) != item.slot)) item.mechanic = EquipmentMechanic.None;
            EnsureUpgradeBasis(item);
            if (string.IsNullOrWhiteSpace(item.name)) item.name = "无名" + ItemBaseName(item.slot, hero);
            if (item.name.Length > 60) item.name = item.name.Substring(0, 60);
        }

        private static int[] RepairLoadout(int[] previous)
        {
            if (previous == null) return GameBalance.DefaultLoadout();
            int[] loadout = new int[GameBalance.HotbarSize * GameBalance.HotbarPages];
            for (int i = 0; i < loadout.Length; i++) loadout[i] = -1;
            bool legacy = previous.Length == 3;
            int[] defaults = GameBalance.DefaultLoadout();
            for (int page = 0; page < GameBalance.HotbarPages; page++)
            {
                var used = new HashSet<int>();
                for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
                {
                    int index = page * GameBalance.HotbarSize + slot;
                    int skill = index < previous.Length ? previous[index] : -1;
                    if (legacy && page == 0 && slot >= 3)
                    {
                        skill = defaults[slot];
                        if (skill >= 0 && used.Contains(skill))
                        {
                            skill = 0;
                            while (skill < GameBalance.SkillCount && (used.Contains(skill) || GameBalance.IsPassive(skill))) skill++;
                        }
                    }
                    // Locked default skills remain mapped; casting still requires a learned rank.
                    if ((skill == GameBalance.HotbarPotion || (skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill))) && used.Add(skill)) loadout[index] = skill;
                }
            }
            return loadout;
        }

        private static int[] RepairHotbarKeys(int[] previous)
        {
            int[] keys = new int[GameBalance.HotbarSize];
            var used = new HashSet<int>();
            for (int slot = 0; slot < keys.Length; slot++)
            {
                int key = previous != null && slot < previous.Length ? previous[slot] : 0;
                if (GameBalance.IsBindableKey(key) && used.Add(key)) keys[slot] = key;
            }
            for (int slot = 0; slot < keys.Length; slot++)
            {
                if (keys[slot] != 0) continue;
                int key = GameBalance.DefaultHotbarKeys[slot];
                if (used.Contains(key))
                {
                    for (int candidate = 0; candidate < GameBalance.DefaultHotbarKeys.Length; candidate++)
                        if (!used.Contains(GameBalance.DefaultHotbarKeys[candidate])) { key = GameBalance.DefaultHotbarKeys[candidate]; break; }
                }
                keys[slot] = key;
                used.Add(key);
            }
            return keys;
        }

        private static int Clamp(int value, int minimum, int maximum) { return Math.Max(minimum, Math.Min(maximum, value)); }
        private static int Round(float value) { return (int)Math.Round(value, MidpointRounding.AwayFromZero); }
    }
}
