using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Runs production code inside Unity, including Unity's real JsonUtility.</summary>
    public static class ProgressionValidation
    {
        [Serializable]
        private sealed class ValidationReport
        {
            public string status = "RUNNING";
            public string unityVersion;
            public string startedUtc;
            public string completedUtc;
            public string outputDirectory;
            public int assertions;
            public List<string> passedStages = new List<string>();
            public string failure = "";
        }

        [Serializable]
        private sealed class LegacyProfile
        {
            public int version = 1;
            public HeroClass heroClass = HeroClass.Arcanist;
            public int level = 12;
            public int xp = 37;
            public int gold = 321;
            public int potions = 7;
            public int skillPoints = 5;
            public int[] skillRanks = { 3, 2, 1 };
            public int kills = 45;
            public int clearedRuns = 3;
            public int bestFloor = 3;
            public List<ItemData> inventory;
            public string weaponId;
            public string armorId;
            public string relicId;
        }

        private static ValidationReport report;

        [MenuItem("Emberfall/Validate Progression and Skills")]
        public static void Validate()
        {
            string workspace = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath));
            string output = Path.GetFullPath(Path.Combine(workspace, "Tests", "TestResults", "unity-progression-" + Guid.NewGuid().ToString("N")));
            string workspacePrefix = workspace.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!output.StartsWith(workspacePrefix, StringComparison.OrdinalIgnoreCase)) throw new Exception("Validation directory must be inside this Unity project.");
            Directory.CreateDirectory(output);
            report = new ValidationReport
            {
                unityVersion = Application.unityVersion,
                startedUtc = DateTime.UtcNow.ToString("o"),
                outputDirectory = output
            };
            try
            {
                for (int hero = 0; hero < 3; hero++) ValidateClass((HeroClass)hero);
                ValidateLegacyJson();
                ValidateDamagedJson();
                ValidateBackupRecovery();
                ValidateSkillRuntime();
                ValidatePortableSaveTransfer();
                report.status = "PASS";
                Debug.Log("Emberfall Unity validation PASS: " + report.assertions + " assertions. Report: " + Path.Combine(output, "validation-report.json"));
            }
            catch (Exception exception)
            {
                report.status = "FAIL";
                report.failure = exception.ToString();
                Debug.LogError("Emberfall Unity validation failed. Report: " + Path.Combine(output, "validation-report.json") + "\n" + exception);
                throw;
            }
            finally
            {
                report.completedUtc = DateTime.UtcNow.ToString("o");
                File.WriteAllText(Path.Combine(output, "validation-report.json"), JsonUtility.ToJson(report, true), new UTF8Encoding(false));
                var summary = new StringBuilder();
                summary.AppendLine(report.status + ": " + report.assertions + " assertions; Unity " + report.unityVersion);
                foreach (string stage in report.passedStages) summary.AppendLine("PASS " + stage);
                if (!string.IsNullOrEmpty(report.failure)) summary.AppendLine(report.failure);
                File.WriteAllText(Path.Combine(output, "validation-report.txt"), summary.ToString(), new UTF8Encoding(false));
            }
        }

        private static ProgressionService Fresh(string name, HeroClass hero = HeroClass.Vanguard)
        {
            var service = new ProgressionService(CaseDirectory(name));
            Check(!service.HasSave, name + ": injected directory initially has no save");
            service.NewGame(hero);
            Check(string.IsNullOrEmpty(service.LastError) && service.HasSave, name + ": real JsonUtility writes new character");
            return service;
        }

        private static void ValidateClass(HeroClass hero)
        {
            string name = "class-" + hero;
            ProgressionService service = Fresh(name, hero);
            Check(service.Profile.inventory.Count == 3 && service.Equipped(ItemSlot.Weapon) != null, name + ": starter equipment exists");
            Check(!service.LearnSkill(0), name + ": level-one skill learning is gated");
            service.GrantExperience(int.MaxValue);
            Check(service.Profile.level == 100 && service.Profile.skillPoints == 99, name + ": bulk XP reaches level cap safely");
            StatBlock baseline = service.GetStats();
            int changed = 0;
            service.Changed += () => changed++;
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                Check(!string.IsNullOrWhiteSpace(GameBalance.SkillName(hero, skill)), name + ": skill catalog entry " + skill);
                for (int rank = 1; rank <= 3; rank++)
                    Check(service.LearnSkill(skill) && service.Profile.skillRanks[skill] == rank, name + ": learn skill " + skill + " rank " + rank);
                Check(!service.LearnSkill(skill), name + ": fourth rank rejected for skill " + skill);
            }
            Check(changed == 30 && service.Profile.skillPoints == 69, name + ": every rank emits an update and consumes exactly one point");
            Check(!service.AssignSkill(8, 3) && !service.AssignSkill(8, 8), name + ": learned passives cannot enter active bar");
            StatBlock stats = service.GetStats();
            if (hero == HeroClass.Vanguard)
                Check(Near(stats.Damage, baseline.Damage * 1.22f) && Near(stats.Armor, baseline.Armor + 7), name + ": rank-three passive includes equipment damage and armor");
            else if (hero == HeroClass.Arcanist)
                Check(Near(stats.Damage, baseline.Damage * 1.18f) && Near(stats.MaxHealth, baseline.MaxHealth * 1.1f), name + ": rank-three passive includes equipped damage and health");
            else
                Check(Near(stats.CritChance, baseline.CritChance + .12f) && Near(stats.MoveSpeed, baseline.MoveSpeed * 1.1f), name + ": rank-three passive improves critical chance and movement");
            Check(service.AssignSkill(0, 9) && service.Profile.equippedSkills[7] == 0, name + ": active assignment swaps same-page skill");
            Check(service.SetHotbarPage(1) && service.AssignSkill(0, 9) && service.AssignSkill(9, 9), name + ": second page accepts and swaps same skill independently");
            Check(service.Profile.equippedSkills[0] == 9 && service.Profile.equippedSkills[10] == -1 && service.Profile.equippedSkills[19] == 9, name + ": page swap preserves first page");
            Check(service.SetHotbarPage(2) && service.AssignSkill(0, 4) && service.AssignSkill(9, 7) && service.AssignSkill(9, -1), name + ": third page assign and clear");
            Check(!service.SetHotbarPage(3) && service.Profile.hotbarPage == 2, name + ": invalid page is rejected");
            Check(service.SetHotbarKey(0, 113) && service.SetHotbarKey(1, 113), name + ": keyboard remapping supports duplicate swap");
            Check(service.Profile.hotbarKeys[0] == 120 && service.Profile.hotbarKeys[1] == 113 && !service.SetHotbarKey(0, 97), name + ": keyboard swap preserves uniqueness and reserves movement");
            string weaponId = service.Profile.weaponId;
            service.Save();
            Check(string.IsNullOrEmpty(service.LastError) && File.Exists(SavePath(name) + ".bak"), name + ": flushed primary and backup exist");
            string json = File.ReadAllText(SavePath(name));
            Check(json.Contains("emberfall-character") && json.Contains("skillRanks"), name + ": private serializable save envelope produces valid document");
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load(), name + ": private save envelope deserializes with real JsonUtility");
            Check(restored.Profile.heroClass == hero && restored.Profile.level == 100 && restored.Profile.skillPoints == 69 && restored.Profile.weaponId == weaponId, name + ": identity, points and gear persist");
            for (int skill = 0; skill < 10; skill++) Check(restored.Profile.skillRanks[skill] == 3, name + ": saved rank " + skill);
            Check(restored.Profile.hotbarPage == 2 && restored.Profile.equippedSkills.Length == 30 && restored.Profile.equippedSkills[20] == 4 && restored.Profile.equippedSkills[19] == 9 && restored.Profile.equippedSkills[29] == -1, name + ": all hotbar pages persist");
            Check(restored.Profile.hotbarKeys[0] == 120 && restored.Profile.hotbarKeys[1] == 113, name + ": custom keys persist");
            Check(SameStats(stats, restored.GetStats()), name + ": passive stats survive save round trip");
            report.passedStages.Add(name + ": ten skills, ranks, passives, pages, keys, real JSON round trip");
        }

        private static void ValidateLegacyJson()
        {
            const string name = "legacy-json";
            ProgressionService seed = Fresh(name, HeroClass.Arcanist);
            var legacy = new LegacyProfile
            {
                inventory = seed.Profile.inventory,
                weaponId = seed.Profile.weaponId,
                armorId = seed.Profile.armorId,
                relicId = seed.Profile.relicId
            };
            string payload = JsonUtility.ToJson(legacy);
            WriteEnvelope(name, payload);
            var migrated = new ProgressionService(CaseDirectory(name));
            Check(migrated.Load(), "legacy: original three-rank JSON loads without new fields");
            Check(migrated.Profile.level == 12 && migrated.Profile.xp == 37 && migrated.Profile.gold == 321 && migrated.Profile.potions == 7 && migrated.Profile.kills == 45 && migrated.Profile.clearedRuns == 3, "legacy: other character progress remains intact");
            Check(migrated.Profile.inventory.Count == 3 && migrated.Profile.weaponId == legacy.weaponId && migrated.Profile.armorId == legacy.armorId && migrated.Profile.relicId == legacy.relicId, "legacy: all equipment IDs survive");
            Check(migrated.Profile.skillRanks.Length == 10 && migrated.Profile.skillRanks[0] == 2 && migrated.Profile.skillRanks[1] == 2 && migrated.Profile.skillRanks[2] == 1, "legacy: ranks pad to ten and newly gated rank is repaired");
            Check(migrated.Profile.skillPoints == 6 && migrated.LastError.Contains("返还"), "legacy: locked rank refunded with player-facing information");
            Check(migrated.Profile.skillPoints + migrated.Profile.skillRanks[0] + migrated.Profile.skillRanks[1] + migrated.Profile.skillRanks[2] == 11, "legacy: all earned skill points conserved");
            Check(migrated.Profile.equippedSkills.Length == 30 && migrated.Profile.equippedSkills[0] == 0 && migrated.Profile.equippedSkills[3] == 4 && migrated.Profile.equippedSkills[29] == -1, "legacy: missing loadout initializes active-only defaults");
            Check(migrated.Profile.hotbarKeys.Length == 10 && migrated.Profile.hotbarKeys[0] == 122, "legacy: missing key bindings repaired");
            string threeSlotPayload = payload.Substring(0, payload.Length - 1) + ",\"equippedSkills\":[2,0,1]}";
            WriteEnvelope(name, threeSlotPayload);
            Check(migrated.Load() && migrated.Profile.equippedSkills[0] == 2 && migrated.Profile.equippedSkills[1] == 0 && migrated.Profile.equippedSkills[2] == 1 && migrated.Profile.equippedSkills[3] == 4, "legacy: existing three-slot order is migrated without resetting it");
            migrated.Save();
            var reloaded = new ProgressionService(CaseDirectory(name));
            Check(reloaded.Load() && reloaded.Profile.skillPoints == 6 && string.IsNullOrEmpty(reloaded.LastError), "legacy: migration persists and does not refund a second time");
            report.passedStages.Add("legacy JSON migration and conserved rank refunds");
        }

        private static void ValidateDamagedJson()
        {
            const string name = "repair-json";
            Fresh(name);
            var damaged = new GameProfile
            {
                level = 40,
                skillRanks = null,
                equippedSkills = new[] { 3, 0, 0, 99, -99, 8, 4, 5 },
                hotbarKeys = new[] { 120, 120, 97, 999, 113 },
                hotbarPage = 50
            };
            WriteEnvelope(name, JsonUtility.ToJson(damaged));
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load() && restored.Profile.skillRanks.Length == 10 && restored.Profile.skillPoints == 39, "repair: actual JSON null rank array restores earned points");
            Check(restored.Profile.hotbarPage == 0 && restored.Profile.equippedSkills.Length == 30, "repair: invalid page and short loadout repaired");
            Check(restored.Profile.equippedSkills[0] == -1 && restored.Profile.equippedSkills[1] == 0 && restored.Profile.equippedSkills[2] == -1 && restored.Profile.equippedSkills[5] == -1 && restored.Profile.equippedSkills[6] == 4, "repair: passive, duplicate and invalid assignments cleared individually");
            var keys = new HashSet<int>();
            Check(restored.Profile.hotbarKeys.Length == 10, "repair: ten keyboard slots restored");
            foreach (int key in restored.Profile.hotbarKeys) Check(GameBalance.IsBindableKey(key) && keys.Add(key), "repair: restored keyboard key is bindable and unique");
            Check(restored.Profile.hotbarKeys[0] == 120 && restored.Profile.hotbarKeys[4] == 113, "repair: valid user keyboard choices retained");
            report.passedStages.Add("real JSON null, malformed array, loadout and keyboard repair");
        }

        private static void ValidateBackupRecovery()
        {
            const string name = "backup-json";
            ProgressionService service = Fresh(name);
            service.AddGold(20);
            File.WriteAllText(SavePath(name), "{ truncated document");
            var restored = new ProgressionService(CaseDirectory(name));
            Check(restored.Load() && restored.Profile.gold == 60 && restored.LastError.Contains("备份"), "backup: Unity JSON parse failure recovers previous valid copy");
            restored.AddGold(7);
            var repaired = new ProgressionService(CaseDirectory(name));
            Check(repaired.Load() && repaired.Profile.gold == 67, "backup: recovered profile safely replaces damaged primary");
            File.WriteAllText(SavePath(name), "again corrupt");
            Check(repaired.Load() && repaired.Profile.gold == 60, "backup: prior recovery did not replace valid backup with corrupt primary");
            report.passedStages.Add("real JSON corruption, atomic save and preserved backup recovery");
        }

        private static void ValidateSkillRuntime()
        {
            const string name = "runtime";
            ProgressionService service = Fresh(name);
            service.GrantExperience(int.MaxValue);
            for (int rank = 1; rank <= 3; rank++) Check(service.LearnSkill(9), "runtime: ultimate learned through production progression");
            Check(service.SetHotbarPage(0) && service.AssignSkill(0, 9), "runtime: ultimate mapped on first page");
            var runtime = new SkillRuntime();
            Check(runtime.TryConsume(9, service.Profile.skillRanks[9]) && Near(runtime.Energy, 15) && Near(runtime.Remaining(9), 77.4f), "runtime: rank-three ultimate spends 85 energy and starts independent cooldown");
            Check(service.SetHotbarPage(1) && service.AssignSkill(5, 9) && service.SetHotbarKey(5, 113), "runtime: same skill can be mapped and rebound on second page");
            int mapped = service.Profile.equippedSkills[service.Profile.hotbarPage * 10 + 5];
            Check(!runtime.TryConsume(mapped, service.Profile.skillRanks[mapped]) && Near(runtime.Remaining(9), 77.4f), "runtime: changing pages and bindings cannot bypass cooldown");
            Check(!runtime.TryConsume(7, 1) && Near(runtime.Remaining(7), 0) && Near(runtime.Energy, 15), "runtime: insufficient energy changes no resources");
            Check(runtime.TryConsume(0, 1) && Near(runtime.Energy, 7), "runtime: a low-cost skill remains usable after ultimate");
            runtime.Advance(5);
            Check(Near(runtime.Remaining(0), 0) && Near(runtime.Remaining(9), 72.4f) && Near(runtime.Energy, 27), "runtime: energy regeneration and cooldown passage are independent");
            runtime.FillEnergy();
            Check(Near(runtime.Energy, 100) && Near(runtime.Remaining(9), 72.4f), "runtime: resource refill cannot reset cooldown");
            Check(!runtime.TryConsume(3, 3) && !runtime.TryConsume(8, 3), "runtime: both learned passive IDs cannot cast");
            Check(!runtime.TryConsume(-1, 1) && !runtime.TryConsume(10, 1) && !runtime.TryConsume(0, 0) && !runtime.TryConsume(0, 4), "runtime: empty slots and invalid ranks safely rejected");
            float beforeCooldown = runtime.Remaining(9);
            runtime.Advance(float.NaN);
            runtime.Advance(float.PositiveInfinity);
            runtime.Advance(-1);
            runtime.RestoreEnergy(float.NaN);
            Check(Near(runtime.Energy, 100) && Near(runtime.Remaining(9), beforeCooldown), "runtime: non-finite and negative time cannot mutate state");
            runtime.Advance(1000);
            Check(runtime.TryConsume(9, 3), "runtime: ultimate becomes usable after real cooldown expires");
            report.passedStages.Add("skill runtime energy, passive rejection and cooldown integrity across pages and bindings");
        }

        private static void ValidatePortableSaveTransfer()
        {
            const string sourceName = "portable-source";
            ProgressionService source = Fresh(sourceName, HeroClass.Ranger);
            int experience = 23;
            for (int level = 1; level < 50; level++) experience += GameBalance.XpToNext(level);
            source.GrantExperience(experience);
            source.AddGold(987);
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                for (int rank = 1; rank <= 3; rank++) Check(source.LearnSkill(skill), "portable: source learns skill " + skill + " rank " + rank);
            ItemData item = source.CreateLoot(1, true);
            Check(source.Equip(item.id) && source.Upgrade(item.id), "portable: source equips upgraded loot");
            source.CreateLoot(40, true);
            source.UsePotion();
            source.Profile.kills = 123;
            source.Profile.clearedRuns = 5;
            source.Profile.bestFloor = 5;
            Check(source.SetHotbarPage(2) && source.AssignSkill(0, 9) && source.AssignSkill(9, 7), "portable: source configures independent skill page");
            Check(source.SetHotbarKey(0, 113) && source.SetHotbarKey(9, 282), "portable: source configures custom keyboard mapping");
            source.Save();
            Check(source.SaveDirectory == CaseDirectory(sourceName) && source.SaveFilePath == SavePath(sourceName), "portable: exposed save paths point to injected test directory");
            string sourceJson = File.ReadAllText(source.SaveFilePath);
            string expectedProfile = JsonUtility.ToJson(source.Profile, true);
            string unescapedJson = sourceJson.Replace("\\\\", "\\").Replace("\\/", "/");
            Check(unescapedJson.IndexOf(source.SaveDirectory, StringComparison.OrdinalIgnoreCase) < 0 && unescapedJson.IndexOf("\"saveDirectory\"", StringComparison.OrdinalIgnoreCase) < 0 && unescapedJson.IndexOf("\"machineId\"", StringComparison.OrdinalIgnoreCase) < 0, "portable: player JSON carries no absolute source path or machine identity");
            string destination = CaseDirectory("portable-destination");
            Directory.CreateDirectory(destination);
            File.Copy(source.SaveFilePath, Path.Combine(destination, "emberfall-save.json"));
            Check(Directory.GetFiles(destination).Length == 1, "portable: only the primary JSON is copied to new installation");
            var migrated = new ProgressionService(destination);
            Check(migrated.HasSave && migrated.Load(), "portable: fresh service loads copied JSON without source backup or registry state");
            Check(migrated.Profile.heroClass == HeroClass.Ranger && migrated.Profile.level == 50 && migrated.Profile.xp == 23 && migrated.Profile.skillPoints == 19, "portable: copied class, level, XP and point balance survive");
            Check(migrated.Profile.inventory.Count == source.Profile.inventory.Count && migrated.Profile.weaponId == source.Profile.weaponId && migrated.Profile.armorId == source.Profile.armorId && migrated.Profile.relicId == source.Profile.relicId, "portable: copied inventory and equipped item identities survive");
            Check(migrated.Profile.hotbarPage == 2 && migrated.Profile.equippedSkills[20] == 9 && migrated.Profile.equippedSkills[29] == 7 && migrated.Profile.hotbarKeys[0] == 113 && migrated.Profile.hotbarKeys[9] == 282, "portable: skill pages and custom key bindings survive");
            Check(JsonUtility.ToJson(migrated.Profile, true) == expectedProfile, "portable: all profile fields exactly match source after real JsonUtility load");
            migrated.AddGold(1);
            Check(File.ReadAllText(source.SaveFilePath) == sourceJson, "portable: destination writes leave source save untouched");
            string emptyDirectory = CaseDirectory("portable-empty-installation");
            var empty = new ProgressionService(emptyDirectory);
            Check(!empty.HasSave && !Directory.Exists(emptyDirectory), "portable: third installation begins without save or copied state");
            empty.NewGame(HeroClass.Arcanist);
            Check(empty.HasSave && empty.Profile.heroClass == HeroClass.Arcanist && empty.Profile.level == 1 && empty.Profile.skillPoints == 0 && File.Exists(empty.SaveFilePath), "portable: third installation creates independent new character");
            Check(File.ReadAllText(source.SaveFilePath) == sourceJson && migrated.Profile.heroClass == HeroClass.Ranger, "portable: new independent character does not overwrite other installations");
            report.passedStages.Add("portable primary-JSON-only transfer, complete profile equality and independent clean installation");
        }

        private static string CaseDirectory(string name) { return Path.Combine(report.outputDirectory, name); }
        private static string SavePath(string name) { return Path.Combine(CaseDirectory(name), "emberfall-save.json"); }
        private static void WriteEnvelope(string name, string profileJson)
        {
            File.WriteAllText(SavePath(name), "{\"format\":\"emberfall-character\",\"version\":1,\"profile\":" + profileJson + "}", new UTF8Encoding(false));
        }
        private static bool Near(float a, float b) { return Math.Abs(a - b) < .003f; }
        private static bool SameStats(StatBlock a, StatBlock b)
        {
            return Near(a.Damage, b.Damage) && Near(a.Armor, b.Armor) && Near(a.MaxHealth, b.MaxHealth) && Near(a.MoveSpeed, b.MoveSpeed) && Near(a.CritChance, b.CritChance);
        }
        private static void Check(bool condition, string description)
        {
            report.assertions++;
            if (!condition) throw new Exception("Assertion " + report.assertions + " failed: " + description);
        }
    }
}
