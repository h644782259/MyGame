using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Owns the character's persistent progression. It has no scene dependencies.</summary>
    public class ProgressionService
    {
        public const int MaximumLevel = 100;
        public const int InventoryCapacity = 72;
        public const int MaximumUpgrade = 10;
        public const int PotionPrice = 20;
        private const int MaximumGold = 999999999;
        private const int MaximumEquipmentStat = 10000;
        private const int MaximumEquipmentHealth = 100000;
        private const string SaveFormat = "emberfall-character";
        private readonly string savePath;
        private readonly string backupPath;
        private readonly string temporaryPath;
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
        public event Action Changed;
        public event Action<int> LeveledUp;
        public string LastError { get; private set; }
        public string SaveDirectory { get { return Path.GetDirectoryName(savePath); } }
        public string SaveFilePath { get { return savePath; } }
        public bool HasSave { get { return File.Exists(savePath) || File.Exists(backupPath); } }

        public ProgressionService(string saveDirectory = null)
        {
            string directory = string.IsNullOrWhiteSpace(saveDirectory) ? Application.persistentDataPath : saveDirectory;
            savePath = Path.Combine(directory, "emberfall-save.json");
            backupPath = savePath + ".bak";
            temporaryPath = savePath + ".tmp";
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
            GameProfile loaded;
            string failure;
            if (TryReadProfile(savePath, out loaded, out failure))
            {
                Profile = loaded;
                LastError = failure;
                RaiseChanged();
                return true;
            }
            string backupFailure;
            if (TryReadProfile(backupPath, out loaded, out backupFailure))
            {
                Profile = loaded;
                LastError = "主存档无法读取，已恢复上一次备份。" + (string.IsNullOrEmpty(backupFailure) ? "" : " " + backupFailure);
                RaiseChanged();
                return true;
            }
            LastError = HasSave ? "存档及备份均无法读取，可创建新角色。" : "尚无可继续的存档。";
            return false;
        }

        public void Save()
        {
            try
            {
                ValidateProfile(Profile);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                var save = new SaveFile { format = SaveFormat, version = 1, profile = Profile };
                string json = JsonUtility.ToJson(save, true);
                // Flush the complete new document before atomically replacing the old one.
                using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(savePath))
                {
                    GameProfile previous;
                    string error;
                    // A corrupt primary must never replace a usable recovery backup.
                    bool validPrevious = TryReadProfile(savePath, out previous, out error);
                    File.Replace(temporaryPath, savePath, validPrevious ? backupPath : null, true);
                }
                else
                {
                    File.Move(temporaryPath, savePath);
                    if (!File.Exists(backupPath)) File.Copy(savePath, backupPath);
                }
                LastError = string.Empty;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                LastError = "保存失败：" + exception.Message;
                Debug.LogWarning("Emberfall: " + LastError);
            }
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
            return stats;
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
            float multiplier = new[] { 1f, 1.35f, 1.8f, 2.5f }[(int)rarity];
            var item = new ItemData
            {
                id = Guid.NewGuid().ToString("N"),
                slot = slot,
                rarity = rarity,
                level = level,
                name = new[] { "旅者", "苍蓝", "星辉", "烬王" }[(int)rarity] + ItemBaseName(slot, Profile.heroClass)
            };
            if (slot == ItemSlot.Weapon) item.attack = Round((5 + level * 2.5f) * multiplier);
            else if (slot == ItemSlot.Armor)
            {
                item.defense = Round((3 + level * 1.2f) * multiplier);
                item.health = Round((10 + level * 4) * multiplier);
            }
            else
            {
                item.attack = Round((2 + level) * multiplier);
                item.health = Round((6 + level * 3) * multiplier);
            }
            EnsureUpgradeBasis(item);
            return item;
        }

        /// <summary>Collect an existing drop once, preserving the full-bag gold fallback.</summary>
        public bool CollectLoot(ItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity))
                return Fail("掉落装备无效。");
            if (collectedLootIds.Contains(item.id) || FindItem(item.id) != null)
                return Fail("这件装备已经拾取。");
            bool overflow = Profile.inventory.Count >= InventoryCapacity;
            if (overflow) Profile.gold = (int)Math.Min(MaximumGold, (long)Profile.gold + SellValue(item));
            else Profile.inventory.Add(item);
            collectedLootIds.Add(item.id);
            Commit();
            if (overflow && string.IsNullOrEmpty(LastError))
                LastError = "背包已满，" + item.name + "已自动出售，获得 " + SellValue(item) + " 金币。";
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
            SetEquipped(Profile, item);
            Commit();
            return true;
        }

        public bool Sell(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (IsEquipped(Profile, item.id)) return Fail("请先替换身上的装备，再出售。");
            Profile.gold = (int)Math.Min(MaximumGold, (long)Profile.gold + SellValue(item));
            Profile.inventory.Remove(item);
            Commit();
            return true;
        }

        public bool Upgrade(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (item.upgradeLevel >= MaximumUpgrade) return Fail("装备已达到强化上限 +10。");
            int cost = UpgradeCost(item);
            if (Profile.gold < cost) return Fail("金币不足，强化需要 " + cost + " 金币。");
            EnsureUpgradeBasis(item);
            Profile.gold -= cost;
            ApplyUpgradeRank(item, item.upgradeLevel + 1);
            Commit();
            return true;
        }

        /// <summary>
        /// Moves the source's rank onto another item of the same slot for free.
        /// An upgraded destination returns its old rank to the source: no ranks are
        /// added, copied or destroyed, and each item's own base attributes stay put.
        /// </summary>
        public bool TransferUpgrade(string sourceId, string targetId)
        {
            ItemData source = FindItem(sourceId);
            ItemData target = FindItem(targetId);
            if (source == null || target == null) return Fail("找不到来源或目标装备，可能已经出售。");
            if (source == target || source.id == target.id) return Fail("请选择两件不同的装备。");
            if (source.slot != target.slot) return Fail("只有相同部位的装备可以转移强化。");
            if (source.upgradeLevel <= 0) return Fail("来源装备没有可转移的强化等级。");
            if (source.upgradeLevel > MaximumUpgrade || target.upgradeLevel < 0 || target.upgradeLevel > MaximumUpgrade)
                return Fail("装备强化等级无效，请重新读取存档。");

            // Validate the entire request before initializing or changing either item.
            EnsureUpgradeBasis(source);
            EnsureUpgradeBasis(target);
            int sourceRank = source.upgradeLevel;
            int targetRank = target.upgradeLevel;
            ApplyUpgradeRank(source, targetRank);
            ApplyUpgradeRank(target, sourceRank);
            Commit();
            return true;
        }

        /// <summary>Returns an independent preview; never mutates items, gold, saves or events.</summary>
        public ItemData PreviewUpgrade(ItemData item, int rank)
        {
            if (item == null || rank < 0 || rank > MaximumUpgrade) return null;
            var preview = new ItemData
            {
                id = item.id, name = item.name, slot = item.slot, rarity = item.rarity, level = item.level,
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
            if (item == null || item.upgradeLevel >= MaximumUpgrade) return 0;
            return (18 + Clamp(item.level, 1, MaximumLevel) * 6) * (Clamp(item.upgradeLevel, 0, MaximumUpgrade) + 1) * ((int)item.rarity + 1);
        }

        public int SellValue(ItemData item)
        {
            if (item == null) return 0;
            return (8 + Clamp(item.level, 1, MaximumLevel) * 4) * (Clamp((int)item.rarity, 0, 3) + 1) + Clamp(item.upgradeLevel, 0, MaximumUpgrade) * 10;
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

        /// <summary>Move or swap learned active skills within the selected hotbar page.</summary>
        public bool MoveHotbarSkill(int sourceSlot, int targetSlot)
        {
            if (sourceSlot < 0 || sourceSlot >= GameBalance.HotbarSize || targetSlot < 0 || targetSlot >= GameBalance.HotbarSize)
                return Fail("无效的快捷栏位置。");
            if (sourceSlot == targetSlot) return Fail("技能已经位于这个快捷栏位置。");
            if (Profile.hotbarPage < 0 || Profile.hotbarPage >= GameBalance.HotbarPages || Profile.equippedSkills == null ||
                Profile.equippedSkills.Length < GameBalance.HotbarPages * GameBalance.HotbarSize || Profile.skillRanks == null || Profile.skillRanks.Length < GameBalance.SkillCount)
                return Fail("快捷栏数据无效。");
            int pageStart = Profile.hotbarPage * GameBalance.HotbarSize;
            int source = pageStart + sourceSlot;
            int target = pageStart + targetSlot;
            int skill = Profile.equippedSkills[source];
            if (skill < 0 || skill >= GameBalance.SkillCount || GameBalance.IsPassive(skill) || Profile.skillRanks[skill] < 1)
                return Fail("只能拖动已经学习的主动技能。");
            int displaced = Profile.equippedSkills[target];
            if (displaced < 0 || displaced >= GameBalance.SkillCount || GameBalance.IsPassive(displaced) || Profile.skillRanks[displaced] < 1)
                displaced = -1;
            if (skill == displaced) return Fail("这个技能已经位于目标位置。");
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
            // All points originate from levels; reconstruct unspent points after repairing ranks.
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
                item.level = Clamp(item.level, 1, MaximumLevel);
                item.attack = Clamp(item.attack, 0, 10000);
                item.defense = Clamp(item.defense, 0, 10000);
                item.health = Clamp(item.health, 0, 100000);
                item.upgradeLevel = Clamp(item.upgradeLevel, 0, MaximumUpgrade);
                EnsureUpgradeBasis(item);
                if (string.IsNullOrWhiteSpace(item.name)) item.name = "无名" + ItemBaseName(item.slot, profile.heroClass);
                if (item.name.Length > 60) item.name = item.name.Substring(0, 60);
                items.Add(item);
            }
            profile.inventory = items;
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
            for (int index = items.Count - 1; items.Count > InventoryCapacity && index >= 0; index--)
            {
                if (IsEquipped(profile, items[index].id)) continue;
                items.RemoveAt(index);
            }
            return refundedRanks;
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
                    if (skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill) && used.Add(skill)) loadout[index] = skill;
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
