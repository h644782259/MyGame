using System;
using System.Collections.Generic;
using Emberfall;

namespace UnityEngine
{
    public struct Color { public Color(float r, float g, float b, float a = 1) { } }
}

public static class SkillRuntimeTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("FAILED: " + message);
    }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .001f; }

    public static string Run()
    {
        assertions = 0;
        var state = new SkillRuntime();
        Check(Near(state.Energy, 100), "new adventure starts with full energy");
        Check(state.TryConsume(9, 1), "learned ultimate can cast when ready");
        Check(Near(state.Energy, 15) && Near(state.Remaining(9), 90), "ultimate spends 85 and starts 90-second cooldown");
        Check(!state.TryConsume(7, 1), "second high-tier skill blocked by resource budget");
        Check(Near(state.Remaining(7), 0) && Near(state.Energy, 15), "failed cast does not spend or start cooldown");
        Check(state.TryConsume(0, 1) && Near(state.Energy, 7), "short-cooldown skill remains usable after ultimate");
        state.RestoreEnergy(1000);
        Check(Near(state.Energy, 100) && !state.TryConsume(0, 1), "energy restoration does not reset skill cooldown");

        var profile = new GameProfile();
        profile.equippedSkills[0] = 9;
        profile.equippedSkills[GameBalance.HotbarSize] = 9;
        profile.hotbarPage = 1;
        profile.hotbarKeys[0] = 113;
        int mappedSkill = profile.equippedSkills[profile.hotbarPage * GameBalance.HotbarSize];
        Check(!state.TryConsume(mappedSkill, 1), "page changes and key rebinding cannot bypass cooldown by skill identity");
        state.Advance(5);
        Check(Near(state.Remaining(0), 0) && Near(state.Remaining(9), 85), "low-tier becomes ready while ultimate still cools");
        Check(state.TryConsume(0, 1), "low-tier reusable during ultimate downtime");
        state.FillEnergy();
        Check(Near(state.Remaining(9), 85), "filling energy keeps cooldown");
        state.Advance(85);
        Check(state.TryConsume(9, 3), "rank-three ultimate usable after full cooldown");
        Check(Near(state.Remaining(9), 77.4f), "rank upgrades reduce cooldown without removing tradeoff");
        state.Advance(2);
        Check(Near(state.Energy, 23), "passive regeneration is four energy per second");
        state.RestoreEnergy(8);
        Check(Near(state.Energy, 31), "normal attack hit rewards eight energy");

        float energyBefore = state.Energy;
        float cooldownBefore = state.Remaining(9);
        state.Advance(0); state.Advance(-2); state.Advance(float.NaN); state.Advance(float.PositiveInfinity);
        state.RestoreEnergy(-10); state.RestoreEnergy(float.NaN); state.RestoreEnergy(float.PositiveInfinity);
        Check(Near(state.Energy, energyBefore) && Near(state.Remaining(9), cooldownBefore), "paused or invalid deltas cannot alter combat resources");
        Check(!state.TryConsume(-1, 1) && !state.TryConsume(10, 1) && !state.TryConsume(2, 0) && !state.TryConsume(2, 4), "invalid and unlearned casts rejected");
        Check(!state.TryConsume(3, 3) && !state.TryConsume(8, 3), "passives never cast or consume resources");
        Check(Near(state.Remaining(-1), 0), "empty hotbar slot is safe");
        state.Advance(10000);
        Check(Near(state.Energy, 100) && Near(state.Remaining(9), 0), "large legitimate elapsed time clamps both resources");

        for (int hero = 0; hero < 3; hero++)
        {
            var names = new HashSet<string>();
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                string name = GameBalance.SkillName((HeroClass)hero, skill);
                Check(!string.IsNullOrWhiteSpace(name) && names.Add(name), "class " + hero + " unique skill " + skill);
                Check(!string.IsNullOrWhiteSpace(GameBalance.SkillDescription((HeroClass)hero, skill)), "skill has a readable effect description");
            }
        }
        int previousActive = 0;
        for (int skill = 1; skill < GameBalance.SkillCount; skill++)
        {
            Check(GameBalance.SkillRequiredLevels[skill] > GameBalance.SkillRequiredLevels[skill-1], "higher-tier unlock levels increase");
            Check(GameBalance.SkillRankRequiredLevel(skill, 2) == GameBalance.SkillRequiredLevels[skill]+8 && GameBalance.SkillRankRequiredLevel(skill,3) == GameBalance.SkillRequiredLevels[skill]+18, "rank evolution gated by character level");
            if (GameBalance.IsPassive(skill)) continue;
            Check(GameBalance.SkillEnergyCosts[skill] > GameBalance.SkillEnergyCosts[previousActive], "higher-tier active resource cost increases");
            Check(GameBalance.EffectiveCooldown(skill, 3) > GameBalance.SkillCooldowns[previousActive], "even fully upgraded high-tier active retains a longer cooldown");
            previousActive = skill;
        }
        int[] layout = GameBalance.DefaultLoadout();
        Check(layout.Length == 30, "three independent ten-slot pages");
        int[] active = { 0, 1, 2, 4, 5, 6, 7, 9 };
        for (int slot = 0; slot < 8; slot++) Check(layout[slot] == active[slot] && !GameBalance.IsPassive(layout[slot]), "first page maps eight active skills in unlock order");
        for (int slot = 8; slot < 30; slot++) Check(layout[slot] == -1, "remaining slots and extra pages initially empty");
        var keys = new HashSet<int>();
        foreach (int key in GameBalance.DefaultHotbarKeys) Check(GameBalance.IsBindableKey(key) && keys.Add(key), "default key is valid and unique");
        foreach (int key in new[] { 97, 100, 102, 104, 105, 106, 107, 115, 116, 119, 9, 27, 32, 91, 93 })
            Check(!GameBalance.IsBindableKey(key), "navigation and utility controls reserved");
        Check(GameBalance.KeyName(122) == "Z" && GameBalance.KeyName(49) == "1" && GameBalance.KeyName(293) == "F12", "custom key names readable");
        return "PASS: " + assertions + " skill combat, catalog, paging, and binding assertions.";
    }
}
