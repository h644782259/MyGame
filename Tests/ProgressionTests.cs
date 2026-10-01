// Standalone checks for actual production progression code. These stubs only replace Unity's
// platform path, JSON transport, and colors; no gameplay behavior is copied into the tests.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Emberfall;

namespace UnityEngine
{
    public static class Application { public static string persistentDataPath; }
    public static class Debug { public static void LogWarning(object message) { Console.WriteLine(message); } }
    public struct Color { public Color(float r, float g, float b, float a = 1) { } }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        public static string ToJson(object value, bool pretty) { return JsonSerializer.Serialize(value, value.GetType(), Options); }
        public static T FromJson<T>(string value)
        {
            try { return JsonSerializer.Deserialize<T>(value, Options); }
            catch (JsonException error) { throw new ArgumentException("Malformed JSON", error); }
        }
    }
}

public static class ProgressionTests
{
    private static string root;
    private static int cases;
    private static int assertions;

    public static string Run(string testDirectory)
    {
        root = testDirectory;
        cases = 0;
        assertions = 0;
        NewCharacterAndPersistence();
        SkillPointsAndLeveling();
        InventoryAndEconomy();
        BackupAndCorruption();
        DamagedFieldsAreRepaired();
        UpperBoundsAndInvalidActions();
        EveryClassSkillUnlocksAtItsGate();
        EveryClassSkillRankRequiresItsLevel();
        LegacyThreeSkillSaveMigratesWithoutLoss();
        MalformedSkillArraysAreRepaired();
        SkillLoadoutAssignmentAndPersistence();
        CustomHotbarKeysAndRepair();
        TenSkillRanksAndPointBudget();
        PassiveStatsAreImmediateAndPersistent();
        SaveTransfersBetweenIndependentDirectories();
        return "PASS: " + assertions + " assertions across " + cases + " isolated progression scenarios.";
    }

    private static ProgressionService Fresh(HeroClass heroClass = HeroClass.Vanguard)
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(root, "case-" + (++cases));
        var result = new ProgressionService();
        Check(!result.HasSave, "constructor does not create a save");
        result.NewGame(heroClass);
        return result;
    }

    private static void NewCharacterAndPersistence()
    {
        var service = Fresh(HeroClass.Ranger);
        Check(service.HasSave, "new game saves");
        Check(service.Profile.level == 1 && service.Profile.gold == 60 && service.Profile.potions == 5, "initial progression");
        Check(service.Profile.inventory.Count == 3, "three starter items");
        Check(service.Equipped(ItemSlot.Weapon) != null && service.Equipped(ItemSlot.Armor) != null && service.Equipped(ItemSlot.Relic) != null, "all starter slots equipped");
        service.AddGold(41);
        service.UsePotion();
        var restored = new ProgressionService();
        Check(restored.Load(), "load saved character");
        Check(restored.Profile.heroClass == HeroClass.Ranger && restored.Profile.gold == 101 && restored.Profile.potions == 4, "class and currencies persist");
        Check(restored.Equipped(ItemSlot.Weapon).id == service.Equipped(ItemSlot.Weapon).id, "equipment IDs persist");
    }

    private static void SkillPointsAndLeveling()
    {
        var service = Fresh();
        int levelEvents = 0;
        int changedEvents = 0;
        service.LeveledUp += level => levelEvents++;
        service.Changed += () => changedEvents++;
        Check(!service.LearnSkill(0), "Q locked at level 1");
        service.GrantExperience(60);
        Check(service.Profile.level == 2 && service.Profile.xp == 0 && service.Profile.skillPoints == 1, "level 2 grants one point");
        Check(levelEvents == 1 && changedEvents == 1, "progression events fire");
        Check(service.LearnSkill(0) && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "learning spends exactly one point");
        Check(!service.LearnSkill(0) && !service.LearnSkill(1), "cannot overspend points or bypass level lock");
        service.GrantExperience(90 + 120 + 150 + 180);
        Check(service.Profile.level == 6 && levelEvents == 5 && service.Profile.skillPoints == 4, "multi-level gain and points");
        Check(service.LearnSkill(1) && service.LearnSkill(2), "E and R unlock at required levels");
        Check(!service.LearnSkill(0) && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 2, "rank two requires its higher character level");
        ReachLevel(service, 10);
        Check(service.LearnSkill(0) && service.Profile.skillRanks[0] == 2 && !service.LearnSkill(0), "rank two unlocks at ten while rank three stays locked");
        ReachLevel(service, 20);
        Check(service.LearnSkill(0) && service.Profile.skillRanks[0] == 3 && !service.LearnSkill(0), "rank three unlocks at twenty and remains the cap");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.skillRanks[0] == 3 && restored.Profile.skillRanks[1] == 1 && restored.Profile.skillRanks[2] == 1, "learned ranks persist");
        Check(!restored.LearnSkill(-1) && !restored.LearnSkill(GameBalance.SkillCount), "invalid skill slots rejected");
    }

    private static void InventoryAndEconomy()
    {
        var service = Fresh();
        ItemData weapon = service.Equipped(ItemSlot.Weapon);
        Check(!service.Sell(weapon.id), "equipped item cannot be sold");
        int cost = service.UpgradeCost(weapon);
        float before = service.GetStats().Damage;
        Check(service.Upgrade(weapon.id), "upgrade with enough gold");
        Check(service.Profile.gold == 60 - cost && weapon.upgradeLevel == 1 && service.GetStats().Damage > before, "upgrade charges and immediately improves stats");
        ItemData highLevel = service.CreateLoot(20, true);
        Check(highLevel.rarity >= Rarity.Rare, "boss loot has at least rare quality");
        Check(!service.Equip(highLevel.id), "high-level loot cannot be equipped early");
        int gold = service.Profile.gold;
        int price = service.SellValue(highLevel);
        Check(service.Sell(highLevel.id) && service.Profile.gold == gold + price, "selling grants exact gold");
        Check(!service.Sell(highLevel.id), "item cannot be sold twice");
        ItemData reward = service.CreateLoot(1, false);
        Check(service.Equip(reward.id) && service.Equipped(reward.slot).id == reward.id, "eligible loot can replace equipment");
        for (int i = service.Profile.inventory.Count; i < ProgressionService.InventoryCapacity; i++) service.CreateLoot(1, false);
        gold = service.Profile.gold;
        ItemData overflow = service.CreateLoot(1, true);
        Check(service.Profile.inventory.Count == 72 && !service.Profile.inventory.Exists(item => item.id == overflow.id), "inventory is capped without losing existing gear");
        Check(service.Profile.gold == gold + service.SellValue(overflow) && service.LastError.Contains("自动出售"), "overflow grants gold and a notification");
        service.AddGold(100);
        gold = service.Profile.gold;
        int potions = service.Profile.potions;
        Check(service.BuyPotion() && service.Profile.potions == potions + 1 && service.Profile.gold == gold - 20, "potion purchase");
        Check(service.UsePotion() && service.Profile.potions == potions, "potion consumption");
    }

    private static void BackupAndCorruption()
    {
        var service = Fresh();
        service.AddGold(15); // backup retains initial 60 gold
        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
        File.WriteAllText(path, "{ damaged file");
        var recovery = new ProgressionService();
        Check(recovery.Load() && recovery.Profile.gold == 60 && recovery.LastError.Contains("备份"), "corrupt primary recovers previous snapshot");
        recovery.AddGold(7);
        var roundTrip = new ProgressionService();
        Check(roundTrip.Load() && roundTrip.Profile.gold == 67, "recovered character saves to repaired primary");
        File.WriteAllText(path, "bad again");
        Check(roundTrip.Load() && roundTrip.Profile.gold == 60, "saving recovery does not replace backup with corruption");
        File.WriteAllText(path + ".bak", "also corrupt");
        Check(!new ProgressionService().Load(), "two corrupt copies return false without throwing");
    }

    private static void DamagedFieldsAreRepaired()
    {
        var service = Fresh();
        string path = Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
        File.WriteAllText(path, "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":{\"version\":1,\"heroClass\":99,\"level\":2,\"xp\":-50,\"gold\":-30,\"potions\":500,\"skillPoints\":500,\"skillRanks\":[99,99,99],\"inventory\":[null,{\"id\":\"x\",\"name\":\"\",\"slot\":99,\"rarity\":0}]}}");
        Check(service.Load(), "valid envelope with damaged fields can be repaired");
        Check(service.Profile.heroClass == HeroClass.Vanguard && service.Profile.gold == 0 && service.Profile.potions == 99 && service.Profile.xp == 0, "invalid enums and currency repaired");
        Check(service.Profile.skillRanks[0] == 1 && service.Profile.skillRanks[1] == 0 && service.Profile.skillRanks[2] == 0 && service.Profile.skillPoints == 0, "skill ranks respect level budget and unlocks");
        Check(service.Profile.inventory.Count == 3 && service.Equipped(ItemSlot.Weapon) != null, "missing inventory repaired with valid starter gear");
        service.Profile.inventory[1].id = service.Profile.inventory[0].id;
        service.Save();
        Check(service.Profile.inventory[0].id != service.Profile.inventory[1].id, "duplicate item IDs repaired");
        Check(service.Equipped(ItemSlot.Weapon).slot == ItemSlot.Weapon && service.Equipped(ItemSlot.Armor).slot == ItemSlot.Armor, "repaired IDs retain slot integrity");
    }

    private static void UpperBoundsAndInvalidActions()
    {
        var service = Fresh(HeroClass.Arcanist);
        service.GrantExperience(int.MaxValue);
        Check(service.Profile.level == 100 && service.Profile.xp == 0 && service.Profile.skillPoints == 99, "large XP input safely reaches level cap");
        service.GrantExperience(int.MaxValue);
        Check(service.Profile.level == 100 && service.Profile.skillPoints == 99, "level cap cannot mint extra points");
        service.AddGold(int.MaxValue);
        Check(service.Profile.gold == 999999999, "gold overflow is clamped");
        service.AddGold(int.MinValue);
        Check(service.Profile.gold == 0, "gold loss clamps at zero");
        Check(!service.Upgrade(service.Equipped(ItemSlot.Weapon).id) && !service.BuyPotion(), "insufficient funds rejected");
        Check(!service.Equip(null) && !service.Sell("unknown") && !service.Upgrade("unknown"), "unknown IDs rejected");
        for (int i = 0; i < 5; i++) Check(service.UsePotion(), "existing potion available");
        Check(!service.UsePotion() && service.Profile.potions == 0, "empty potion count never becomes negative");
        service.AddGold(1000000);
        ItemData weapon = service.Equipped(ItemSlot.Weapon);
        for (int i = 0; i < 10; i++) Check(service.Upgrade(weapon.id), "upgrade within cap succeeds");
        Check(!service.Upgrade(weapon.id) && weapon.upgradeLevel == 10, "upgrade cap enforced");
    }

    private static void EveryClassSkillUnlocksAtItsGate()
    {
        int[] expectedLevels = { 2, 4, 6, 8, 10, 13, 16, 20, 25, 30 };
        Check(GameBalance.SkillCount == 10, "eight active and two passive skills per class");
        Check(GameBalance.SkillCooldowns.Length == 10 && GameBalance.SkillEnergyCosts.Length == 10, "each skill has cooldown and energy balance data");
        for (int hero = 0; hero < 3; hero++)
        {
            var service = Fresh((HeroClass)hero);
            var names = new HashSet<string>();
            Check(service.Profile.skillRanks.Length == 10, "new characters have ten skill ranks");
            for (int skill = 0; skill < 10; skill++)
            {
                int required = expectedLevels[skill];
                Check(GameBalance.SkillRequiredLevels[skill] == required, "expected skill unlock milestone");
                string name = GameBalance.SkillName((HeroClass)hero, skill);
                Check(!string.IsNullOrWhiteSpace(name) && names.Add(name) && !string.IsNullOrWhiteSpace(GameBalance.SkillDescription((HeroClass)hero, skill)), "every class skill has a distinct name and description");
                ReachLevel(service, required - 1);
                int points = service.Profile.skillPoints;
                Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 0 && service.Profile.skillPoints == points, "cannot learn one level before requirement");
                Check(service.SkillLockReason(skill).Contains(required.ToString()), "lock reason communicates required level");
                ReachLevel(service, required);
                points = service.Profile.skillPoints;
                Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 1 && service.Profile.skillPoints == points - 1, "skill unlocks and spends one point exactly at requirement");
                if (GameBalance.IsPassive(skill))
                    Check(GameBalance.SkillCooldowns[skill] == 0 && GameBalance.SkillEnergyCosts[skill] == 0, "passive skills have no active cooldown or energy cost");
                else if (skill > 0)
                {
                    int previous = skill - 1;
                    while (GameBalance.IsPassive(previous)) previous--;
                    Check(GameBalance.SkillCooldowns[skill] > GameBalance.SkillCooldowns[previous] && GameBalance.SkillEnergyCosts[skill] > GameBalance.SkillEnergyCosts[previous], "later active skills trade greater cooldown and energy cost");
                }
            }
            Check(service.Profile.level == 30 && service.Profile.skillPoints == 19, "all ten first ranks consume exactly ten earned points");
        }
    }

    private static void EveryClassSkillRankRequiresItsLevel()
    {
        for (int hero = 0; hero < 3; hero++)
            for (int skill = 0; skill < 10; skill++)
            {
                var service = Fresh((HeroClass)hero);
                string originalName = GameBalance.SkillName((HeroClass)hero, skill);
                int assignedSlot = (skill + 3) % 10;
                for (int rank = 1; rank <= 3; rank++)
                {
                    int required = GameBalance.SkillRequiredLevels[skill] + (rank == 1 ? 0 : rank == 2 ? 8 : 18);
                    Check(GameBalance.SkillRankRequiredLevel(skill, rank) == required, "rank gate uses original skill milestone plus evolution offset");
                    ReachLevel(service, required - 1);
                    int points = service.Profile.skillPoints;
                    Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank - 1 && service.Profile.skillPoints == points, "rank cannot be purchased one level early even with spare points");
                    Check(service.SkillLockReason(skill).Contains(required.ToString()), "next-rank lock reason explains higher level requirement");
                    ReachLevel(service, required);
                    points = service.Profile.skillPoints;
                    Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank && service.Profile.skillPoints == points - 1, "rank evolves at exact required level for one point");
                    if (rank == 1)
                        Check(service.AssignSkill(assignedSlot, skill) == !GameBalance.IsPassive(skill), "only active skills can be placed before evolving");
                    bool placementPreserved = GameBalance.IsPassive(skill) ? Array.IndexOf(service.Profile.equippedSkills, skill) < 0 : service.Profile.equippedSkills[assignedSlot] == skill;
                    Check(placementPreserved && GameBalance.SkillName((HeroClass)hero, skill) == originalName, "evolution preserves skill identity and active or passive placement");
                }
                Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 3, "evolved original skill remains capped at rank three");
                int spent = 0;
                foreach (int learned in service.Profile.skillRanks) spent += learned;
                Check(spent == 3 && service.Profile.skillPoints + spent == service.Profile.level - 1, "evolving one skill neither unlocks others nor creates extra points");
            }
    }

    private static void LegacyThreeSkillSaveMigratesWithoutLoss()
    {
        var service = Fresh(HeroClass.Arcanist);
        ReachLevel(service, 12);
        for (int i = 0; i < 3; i++) service.LearnSkill(0);
        for (int i = 0; i < 2; i++) service.LearnSkill(1);
        service.LearnSkill(2);
        service.GrantExperience(37);
        service.AddGold(174);
        service.UsePotion();
        service.Profile.kills = 44;
        service.Profile.clearedRuns = 2;
        service.Profile.bestFloor = 2;
        service.Save();
        string weaponId = service.Profile.weaponId;
        string armorId = service.Profile.armorId;
        string relicId = service.Profile.relicId;
        string path = SavePath();
        JsonNode legacy = JsonNode.Parse(File.ReadAllText(path));
        legacy["profile"]["skillRanks"] = JsonNode.Parse("[3,2,1]");
        legacy["profile"]["skillPoints"] = 5;
        legacy["profile"]["equippedSkills"] = JsonNode.Parse("[2,0,1]");
        ((JsonObject)legacy["profile"]).Remove("hotbarKeys");
        ((JsonObject)legacy["profile"]).Remove("hotbarPage");
        File.WriteAllText(path, legacy.ToJsonString());
        var migrated = new ProgressionService();
        Check(migrated.Load(), "legacy version-1 three-skill save loads");
        GameProfile profile = migrated.Profile;
        Check(profile.version == 1 && profile.heroClass == HeroClass.Arcanist && profile.level == 12 && profile.xp == 37, "migration retains class level XP and version");
        Check(profile.gold == 234 && profile.potions == 4 && profile.kills == 44 && profile.clearedRuns == 2 && profile.bestFloor == 2, "migration retains currencies and completed progress");
        Check(profile.inventory.Count == 3 && profile.weaponId == weaponId && profile.armorId == armorId && profile.relicId == relicId, "migration retains inventory and equipped item IDs");
        Check(profile.skillRanks.Length == 10 && profile.skillRanks[0] == 2 && profile.skillRanks[1] == 2 && profile.skillRanks[2] == 1 && profile.skillPoints == 6, "newly locked old rank is refunded while eligible learned ranks survive");
        Check(migrated.LastError.Contains("返还"), "legacy refund is explained to the player after loading");
        Check(profile.skillPoints + profile.skillRanks[0] + profile.skillRanks[1] + profile.skillRanks[2] == 11, "migration conserves every level-earned skill point");
        for (int i = 3; i < 10; i++) Check(profile.skillRanks[i] == 0, "new skills remain unlearned after migration");
        Check(profile.equippedSkills.Length == 30 && profile.equippedSkills[0] == 2 && profile.equippedSkills[1] == 0 && profile.equippedSkills[2] == 1, "legacy three-slot order survives migration");
        int[] migratedDefault = { 0, 1, 2, 4, 5, 6, 7, 9, -1, -1 };
        for (int i = 3; i < 10; i++) Check(profile.equippedSkills[i] == migratedDefault[i], "remaining active default skills fill first legacy page");
        for (int i = 10; i < 30; i++) Check(profile.equippedSkills[i] == -1, "additional migrated hotbar pages start empty");
        Check(profile.hotbarPage == 0, "legacy character starts on first hotbar page");
        CheckHotbarKeys(profile.hotbarKeys, "legacy character receives ten valid unique hotkeys");
        migrated.Save();
        var reloaded = new ProgressionService();
        Check(reloaded.Load() && reloaded.Profile.skillRanks.Length == 10 && reloaded.Profile.skillRanks[0] == 2 && reloaded.Profile.skillPoints == 6, "migrated ten-skill profile and refunded points persist");
        Check(string.IsNullOrEmpty(reloaded.LastError), "already migrated save does not repeat a refund warning");
        ReachLevel(reloaded, 20);
        Check(reloaded.LearnSkill(0) && reloaded.Profile.skillRanks[0] == 3 && reloaded.Profile.equippedSkills[1] == 0, "refunded original skill can be evolved at its new gate without changing its identity or loadout");
    }

    private static void MalformedSkillArraysAreRepaired()
    {
        var service = Fresh();
        WriteSkillFixture("[-4,5,2,3,99,1,0,8,3,2,9,9]", "[9,8,7]", 40);
        Check(service.Load(), "oversized malformed skill array loads with repair");
        int[] expected = { 0, 3, 2, 3, 3, 1, 0, 3, 2, 2 };
        Check(service.Profile.skillRanks.Length == 10, "excess skill entries removed");
        for (int i = 0; i < expected.Length; i++) Check(service.Profile.skillRanks[i] == expected[i], "each rank bounded to zero through three");
        Check(service.Profile.skillPoints == 20, "point budget reconstructed after rank and level-gate repair");
        Check(service.Profile.equippedSkills[0] == 9 && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[2] == 7, "valid active assignments survive while a mapped passive is cleared");
        string[] malformedLoadouts = { "null", "[]", "[0]", "[0,1,2,3]", "[0,0,2]", "[-1,1,2]", "[10,1,2]" };
        for (int i = 0; i < malformedLoadouts.Length; i++)
        {
            WriteSkillFixture(i % 2 == 0 ? "null" : "[]", malformedLoadouts[i], 40);
            Check(service.Load() && service.Profile.skillRanks.Length == 10 && service.Profile.skillPoints == 39, "missing ranks safely repaired without dropping earned points");
            CheckLoadout(service.Profile, "malformed loadout repaired into three pages without duplicates");
            if (i == 0) CheckDefaultLoadout(service.Profile, "null loadout gets complete default first page");
            if (i == 1) Check(Array.TrueForAll(service.Profile.equippedSkills, value => value == -1), "empty corrupted loadout stays empty rather than inventing assignments");
            if (i == 4) Check(service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[1] == -1 && service.Profile.equippedSkills[2] == 2, "duplicate same-page assignment clears only the duplicate");
            if (i >= 5) Check(service.Profile.equippedSkills[0] == -1 && service.Profile.equippedSkills[1] == 1, "invalid assignment is cleared while valid neighbors survive");
        }
        WriteSkillFixture("[1]", "[0,1,2]", 2);
        Check(service.Load() && service.Profile.skillRanks.Length == 10 && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "short rank array pads missing entries");
        CheckDefaultLoadout(service.Profile, "default mappings remain valid while later skills are still locked");
        WriteSkillFixture("[3,3,3,3,3,3,3,3,3,3]", "[0,1,2]", 2);
        Check(service.Load() && service.Profile.skillRanks[0] == 1 && service.Profile.skillPoints == 0, "rank spending cannot exceed level-earned point budget");
        for (int i = 1; i < 10; i++) Check(service.Profile.skillRanks[i] == 0, "locked higher skills cannot survive corrupted ranks");
    }

    private static void SkillLoadoutAssignmentAndPersistence()
    {
        var service = Fresh(HeroClass.Ranger);
        CheckDefaultLoadout(service.Profile, "new character has eight default active mappings and two empty pages");
        Check(!service.AssignSkill(0, 0), "cannot assign an unlearned default skill");
        ReachLevel(service, 2);
        service.LearnSkill(0);
        Check(service.AssignSkill(1, 0), "learned skill can move onto a locked default slot");
        Check(service.Profile.equippedSkills[0] == 1 && service.Profile.equippedSkills[1] == 0 && service.Profile.equippedSkills[2] == 2, "occupied skill assignment swaps instead of duplicating");
        Check(service.AssignSkill(1, 0) && service.Profile.equippedSkills[0] == 1, "assigning to same slot is idempotent");
        Check(!service.AssignSkill(-1, 0) && !service.AssignSkill(10, 0) && !service.AssignSkill(0, -2) && !service.AssignSkill(0, 10), "invalid loadout slots and skill IDs rejected");
        Check(!service.AssignSkill(2, 9) && service.Profile.equippedSkills[2] == 2, "unlearned higher skill rejected without changing loadout");
        Check(service.AssignSkill(2, -1) && service.Profile.equippedSkills[2] == -1, "negative-one assignment clears a locked or learned slot");
        ReachLevel(service, 30);
        service.LearnSkill(9);
        service.LearnSkill(7);
        int points = service.Profile.skillPoints;
        int changes = 0;
        service.Changed += () => changes++;
        Check(service.AssignSkill(0, 9) && service.AssignSkill(2, 7), "learned high-tier skills can replace unused defaults");
        Check(service.AssignSkill(1, 9), "equipped high-tier skill can swap slots");
        Check(service.Profile.equippedSkills[0] == 0 && service.Profile.equippedSkills[1] == 9 && service.Profile.equippedSkills[2] == 7, "swap preserves previous target skill and uniqueness");
        Check(service.Profile.skillPoints == points && changes == 3, "loadout changes spend no points and notify observers");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.equippedSkills[0] == 0 && restored.Profile.equippedSkills[1] == 9 && restored.Profile.equippedSkills[2] == 7, "loadout automatically persists to disk");
        Check(service.SetHotbarPage(1) && service.Profile.hotbarPage == 1, "second skill page can be selected");
        Check(service.AssignSkill(0, 9) && service.Profile.equippedSkills[10] == 9 && service.Profile.equippedSkills[1] == 9, "same skill is allowed on different pages");
        Check(service.AssignSkill(9, 9) && service.Profile.equippedSkills[10] == -1 && service.Profile.equippedSkills[19] == 9, "duplicate swap stays within selected page");
        Check(service.SetHotbarPage(2) && service.AssignSkill(0, 7) && service.AssignSkill(9, 0), "third page can hold an independent loadout");
        Check(service.AssignSkill(9, -1) && service.Profile.equippedSkills[29] == -1, "clearing third-page slot leaves first-page assignment intact");
        Check(!service.SetHotbarPage(-1) && !service.SetHotbarPage(3) && service.Profile.hotbarPage == 2, "invalid page requests preserve current page");
        restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills[20] == 7 && restored.Profile.equippedSkills[19] == 9 && restored.Profile.equippedSkills[1] == 9, "all pages and selected page persist across reload");
        CheckLoadout(restored.Profile, "multi-page assignments remain unique within each page");
    }

    private static void CustomHotbarKeysAndRepair()
    {
        var service = Fresh();
        CheckHotbarKeys(service.Profile.hotbarKeys, "new character receives ten distinct bindable keys");
        for (int i = 0; i < 10; i++) Check(service.Profile.hotbarKeys[i] == GameBalance.DefaultHotbarKeys[i], "default key order is preserved");
        Check(service.SetHotbarKey(0, 120) && service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 122, "binding an occupied key swaps the two bindings");
        Check(GameBalance.DefaultHotbarKeys[0] == 122 && GameBalance.DefaultHotbarKeys[1] == 120, "user bindings cannot mutate global defaults");
        Check(service.SetHotbarKey(0, 120) && service.Profile.hotbarKeys[1] == 122, "same-slot key reassignment is idempotent");
        Check(service.SetHotbarKey(9, 113) && service.Profile.hotbarKeys[9] == 113, "unused letter key can be assigned");
        Check(service.SetHotbarKey(3, 49) && service.Profile.hotbarKeys[3] == 49 && service.Profile.hotbarKeys[5] == 118, "digit binding swaps existing hotkey");
        int[] invalid = { 0, 27, 32, 97, 100, 102, 105, 106, 107, 115, 119, 999 };
        foreach (int key in invalid)
            Check(!service.SetHotbarKey(0, key) && service.Profile.hotbarKeys[0] == 120, "reserved and unsupported keys cannot replace a valid binding");
        Check(!service.SetHotbarKey(-1, 113) && !service.SetHotbarKey(10, 113), "hotkey slot bounds enforced");
        if (GameBalance.IsBindableKey(282)) Check(service.SetHotbarKey(8, 282), "F1 can be assigned when enabled by key policy");
        if (GameBalance.IsBindableKey(293)) Check(service.SetHotbarKey(7, 293), "F12 can be assigned when enabled by key policy");
        int[] expected = (int[])service.Profile.hotbarKeys.Clone();
        var restored = new ProgressionService();
        Check(restored.Load(), "custom keyboard layout loads");
        for (int i = 0; i < 10; i++) Check(restored.Profile.hotbarKeys[i] == expected[i], "custom key order persists");
        string[] damaged = { "null", "[]", "[120]", "[120,120,97,0,999,282,113,113,49,49,50]" };
        for (int i = 0; i < damaged.Length; i++)
        {
            WriteSkillFixture("null", "null", 1, damaged[i], i % 2 == 0 ? -1 : 100);
            Check(service.Load() && service.Profile.hotbarPage == 0, "invalid selected page returns to page one");
            CheckHotbarKeys(service.Profile.hotbarKeys, "short or corrupt key array repaired to ten valid unique keys");
            if (i == 2) Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 122, "short key repair preserves chosen key and fills defaults without duplication");
            if (i == 3) Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[6] == 113 && service.Profile.hotbarKeys[8] == 49, "key repair preserves first valid occurrence in each duplicate group");
        }
    }

    private static void TenSkillRanksAndPointBudget()
    {
        var service = Fresh();
        service.GrantExperience(int.MaxValue);
        for (int skill = 0; skill < 10; skill++)
        {
            for (int rank = 1; rank <= 3; rank++)
                Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank, "all ten skills support three learned ranks");
            Check(!service.LearnSkill(skill) && service.Profile.skillRanks[skill] == 3, "every skill rejects a fourth rank");
        }
        Check(service.Profile.skillPoints == 69, "ten max-rank skills spend exactly thirty lifetime points");
        var restored = new ProgressionService();
        Check(restored.Load() && restored.Profile.skillRanks.Length == 10 && restored.Profile.skillPoints == 69, "all-ten-rank cap persists");
        for (int skill = 0; skill < 10; skill++) Check(restored.Profile.skillRanks[skill] == 3, "each maxed skill survives reload");
    }

    private static void PassiveStatsAreImmediateAndPersistent()
    {
        Check(GameBalance.IsPassive(3) && GameBalance.IsPassive(8), "exactly designated passive IDs are marked passive");
        int activeCount = 0;
        for (int i = 0; i < 10; i++) if (!GameBalance.IsPassive(i)) activeCount++;
        Check(activeCount == 8, "each class exposes eight active skills");
        for (int hero = 0; hero < 3; hero++)
        {
            var service = Fresh((HeroClass)hero);
            int updates = 0;
            service.Changed += () => updates++;
            for (int rank = 1; rank <= 3; rank++)
            {
                ReachLevel(service, GameBalance.SkillRankRequiredLevel(3, rank));
                int oldRank = service.Profile.skillRanks[3];
                service.Profile.skillRanks[3] = 0;
                StatBlock baseline = service.GetStats();
                service.Profile.skillRanks[3] = oldRank;
                int beforeUpdates = updates;
                Check(service.LearnSkill(3) && updates == beforeUpdates + 1, "passive learning immediately emits stat refresh notification");
                Check(!service.AssignSkill(9, 3), "learned passive cannot be assigned to an empty active slot");
                StatBlock actual = service.GetStats();
                if (hero == 0)
                {
                    float multiplier = rank == 1 ? 1.08f : rank == 2 ? 1.14f : 1.22f;
                    float armor = rank == 1 ? 2f : rank == 2 ? 4f : 7f;
                    Check(Near(actual.Damage, baseline.Damage * multiplier) && Near(actual.Armor, baseline.Armor + armor), "sword passive multiplies equipped damage and adds armor by rank");
                    Check(Near(actual.MaxHealth, baseline.MaxHealth) && Near(actual.MoveSpeed, baseline.MoveSpeed), "sword passive leaves unrelated stats unchanged");
                }
                else if (hero == 1)
                {
                    float damage = rank == 1 ? 1.06f : rank == 2 ? 1.11f : 1.18f;
                    float health = rank == 1 ? 1.04f : rank == 2 ? 1.07f : 1.10f;
                    Check(Near(actual.Damage, baseline.Damage * damage) && Near(actual.MaxHealth, baseline.MaxHealth * health), "mage passive multiplies equipped damage and health by rank");
                    Check(Near(actual.Armor, baseline.Armor) && Near(actual.CritChance, baseline.CritChance), "mage passive leaves unrelated stats unchanged");
                }
                else
                {
                    float crit = rank == 1 ? .04f : rank == 2 ? .07f : .12f;
                    float speed = rank == 1 ? 1.03f : rank == 2 ? 1.06f : 1.10f;
                    Check(Near(actual.CritChance, baseline.CritChance + crit) && Near(actual.MoveSpeed, baseline.MoveSpeed * speed), "ranger passive grants critical chance and movement speed by rank");
                    Check(Near(actual.Damage, baseline.Damage) && Near(actual.MaxHealth, baseline.MaxHealth), "ranger passive leaves unrelated stats unchanged");
                }
                var restored = new ProgressionService();
                Check(restored.Load() && restored.Profile.skillRanks[3] == rank, "passive rank persists immediately");
                StatBlock savedStats = restored.GetStats();
                Check(Near(savedStats.Damage, actual.Damage) && Near(savedStats.Armor, actual.Armor) && Near(savedStats.MaxHealth, actual.MaxHealth) && Near(savedStats.MoveSpeed, actual.MoveSpeed) && Near(savedStats.CritChance, actual.CritChance), "passive-adjusted stats survive save reload");
            }
            ReachLevel(service, GameBalance.SkillRankRequiredLevel(8, 3));
            StatBlock beforeReactive = service.GetStats();
            for (int rank = 1; rank <= 3; rank++) Check(service.LearnSkill(8), "reactive passive can evolve through three ranks");
            Check(!service.AssignSkill(9, 8), "reactive passive cannot be placed on an active hotbar");
            StatBlock afterReactive = service.GetStats();
            Check(Near(beforeReactive.Damage, afterReactive.Damage) && Near(beforeReactive.Armor, afterReactive.Armor) && Near(beforeReactive.MaxHealth, afterReactive.MaxHealth) && Near(beforeReactive.MoveSpeed, afterReactive.MoveSpeed) && Near(beforeReactive.CritChance, afterReactive.CritChance), "reactive passive is handled in combat without a permanent stat bonus");
            CheckLoadout(service.Profile, "learning passives never inserts them into any active page");
        }
    }

    private static bool Near(float actual, float expected) { return Math.Abs(actual - expected) < .002f; }

    private static void SaveTransfersBetweenIndependentDirectories()
    {
        var source = Fresh(HeroClass.Ranger);
        ReachLevel(source, 50);
        source.GrantExperience(23);
        source.AddGold(987);
        for (int skill = 0; skill < 10; skill++)
            for (int rank = 0; rank < 3; rank++) Check(source.LearnSkill(skill), "portable source learns each skill rank");
        ItemData equipment = source.CreateLoot(1, true);
        Check(source.Equip(equipment.id) && source.Upgrade(equipment.id), "portable source receives and upgrades equipped loot");
        source.CreateLoot(40, true);
        source.UsePotion();
        source.Profile.kills = 123;
        source.Profile.clearedRuns = 5;
        source.Profile.bestFloor = 5;
        Check(source.SetHotbarPage(2) && source.AssignSkill(0, 9) && source.AssignSkill(9, 7), "portable source configures third-page skills");
        Check(source.SetHotbarKey(0, 113) && source.SetHotbarKey(9, 282), "portable source configures custom keys");
        source.Save();
        string sourceJson = File.ReadAllText(source.SaveFilePath);
        string profileJson = UnityEngine.JsonUtility.ToJson(source.Profile, true);
        Check(source.SaveDirectory == Path.GetDirectoryName(source.SaveFilePath) && Path.GetFileName(source.SaveFilePath) == "emberfall-save.json", "save directory and file path are exposed for export instructions");
        string unescaped = sourceJson.Replace("\\\\", "\\").Replace("\\/", "/");
        Check(unescaped.IndexOf(source.SaveDirectory, StringComparison.OrdinalIgnoreCase) < 0 && unescaped.IndexOf("\"saveDirectory\"", StringComparison.OrdinalIgnoreCase) < 0 && unescaped.IndexOf("\"machineId\"", StringComparison.OrdinalIgnoreCase) < 0, "save JSON contains no source path or machine identity");
        string destination = Path.Combine(root, "case-" + (++cases));
        Directory.CreateDirectory(destination);
        File.Copy(source.SaveFilePath, Path.Combine(destination, "emberfall-save.json"));
        Check(Directory.GetFiles(destination).Length == 1, "migration copies only the primary JSON file");
        var imported = new ProgressionService(destination);
        Check(imported.SaveDirectory == destination && imported.HasSave && imported.Load(), "fresh service discovers transferred save in its own directory");
        Check(imported.Profile.heroClass == HeroClass.Ranger && imported.Profile.level == 50 && imported.Profile.xp == 23 && imported.Profile.skillPoints == 19, "transferred class, level, XP and unspent points remain intact");
        Check(imported.Profile.inventory.Count == source.Profile.inventory.Count && imported.Profile.weaponId == source.Profile.weaponId && imported.Profile.armorId == source.Profile.armorId && imported.Profile.relicId == source.Profile.relicId, "transferred inventory and equipped IDs remain intact");
        Check(imported.Profile.hotbarPage == 2 && imported.Profile.equippedSkills[20] == 9 && imported.Profile.equippedSkills[29] == 7 && imported.Profile.hotbarKeys[0] == 113 && imported.Profile.hotbarKeys[9] == 282, "transferred pages and custom key bindings remain intact");
        Check(UnityEngine.JsonUtility.ToJson(imported.Profile, true) == profileJson, "every serialized profile field survives cross-directory migration exactly");
        imported.AddGold(1);
        Check(File.ReadAllText(source.SaveFilePath) == sourceJson, "saving imported character cannot modify source installation save");
        string cleanDirectory = Path.Combine(root, "case-" + (++cases));
        var clean = new ProgressionService(cleanDirectory);
        Check(!clean.HasSave && !Directory.Exists(cleanDirectory), "third untouched installation has no save and constructor creates no files");
        clean.NewGame(HeroClass.Arcanist);
        Check(clean.HasSave && File.Exists(clean.SaveFilePath) && clean.Profile.heroClass == HeroClass.Arcanist && clean.Profile.level == 1 && clean.Profile.skillPoints == 0, "third installation can start a new independent character");
        Check(File.ReadAllText(source.SaveFilePath) == sourceJson && imported.Profile.heroClass == HeroClass.Ranger, "independent new game leaves source and imported characters intact");
    }

    private static void ReachLevel(ProgressionService service, int level)
    {
        int xp = -service.Profile.xp;
        for (int current = service.Profile.level; current < level; current++) xp += GameBalance.XpToNext(current);
        if (xp > 0) service.GrantExperience(xp);
    }

    private static string SavePath()
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, "emberfall-save.json");
    }

    private static void WriteSkillFixture(string ranks, string loadout, int level, string keys = "null", int page = 0)
    {
        File.WriteAllText(SavePath(), "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":{\"version\":1,\"level\":" + level + ",\"skillRanks\":" + ranks + ",\"equippedSkills\":" + loadout + ",\"hotbarKeys\":" + keys + ",\"hotbarPage\":" + page + "}}");
    }

    private static void CheckDefaultLoadout(GameProfile profile, string description)
    {
        int[] expected = { 0, 1, 2, 4, 5, 6, 7, 9, -1, -1 };
        bool valid = profile.equippedSkills != null && profile.equippedSkills.Length == 30;
        if (valid)
            for (int i = 0; i < 30; i++) if (profile.equippedSkills[i] != (i < 10 ? expected[i] : -1)) valid = false;
        Check(valid, description);
    }

    private static void CheckLoadout(GameProfile profile, string description)
    {
        bool valid = profile.equippedSkills != null && profile.equippedSkills.Length == 30;
        if (valid)
            for (int page = 0; page < 3; page++)
            {
                var used = new HashSet<int>();
                for (int slot = 0; slot < 10; slot++)
                {
                    int skill = profile.equippedSkills[page * 10 + slot];
                    if (skill < -1 || skill >= 10 || (skill >= 0 && (GameBalance.IsPassive(skill) || !used.Add(skill)))) valid = false;
                }
            }
        Check(valid, description);
    }

    private static void CheckHotbarKeys(int[] keys, string description)
    {
        bool valid = keys != null && keys.Length == 10;
        if (valid)
        {
            var used = new HashSet<int>();
            foreach (int key in keys) if (!GameBalance.IsBindableKey(key) || !used.Add(key)) valid = false;
        }
        Check(valid, description);
    }

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new Exception("Assertion " + assertions + " failed: " + description);
    }
}
