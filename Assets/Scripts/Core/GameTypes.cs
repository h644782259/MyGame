using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public enum HeroClass { Vanguard, Arcanist, Ranger, Summoner }
    public enum ItemSlot { Weapon, Armor, Relic }
    public enum Rarity { Common, Rare, Epic, Legendary }
    public enum FashionSlot { Wings, Weapon }
    public enum ElementalistSpecialization { None, Shatter, Burn }
    public enum EquipmentMechanic { None, FrostEcho, CinderTrail, ReturningBlade, VenomSpread, TwinSummonResonance }
    public enum MasteryType { Offense, Vitality, Guard }

    /// <summary>One source of truth for the build codex, acquisition routes and combat contracts.</summary>
    public static class BuildCatalog
    {
        public static EquipmentMechanic[] MechanicsFor(HeroClass hero)
        {
            switch (hero)
            {
                case HeroClass.Arcanist: return new[] { EquipmentMechanic.FrostEcho, EquipmentMechanic.CinderTrail };
                case HeroClass.Vanguard: return new[] { EquipmentMechanic.ReturningBlade };
                case HeroClass.Ranger: return new[] { EquipmentMechanic.VenomSpread };
                case HeroClass.Summoner: return new[] { EquipmentMechanic.TwinSummonResonance };
                default: return new EquipmentMechanic[0];
            }
        }

        public static HeroClass MechanicClass(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho:
                case EquipmentMechanic.CinderTrail: return HeroClass.Arcanist;
                case EquipmentMechanic.VenomSpread: return HeroClass.Ranger;
                case EquipmentMechanic.TwinSummonResonance: return HeroClass.Summoner;
                default: return HeroClass.Vanguard;
            }
        }

        public static ItemSlot MechanicSlot(EquipmentMechanic mechanic)
        {
            return mechanic == EquipmentMechanic.CinderTrail || mechanic == EquipmentMechanic.ReturningBlade ? ItemSlot.Weapon : ItemSlot.Relic;
        }

        public static string MechanicName(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "霜回护符";
                case EquipmentMechanic.CinderTrail: return "余烬法杖";
                case EquipmentMechanic.ReturningBlade: return "回刃长剑";
                case EquipmentMechanic.VenomSpread: return "蔓毒护符";
                case EquipmentMechanic.TwinSummonResonance: return "双契共鸣";
                default: return "无机制";
            }
        }

        public static string MechanicDescription(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "冰霜新星首击伤害 -20%；0.7秒后回响造成40%基础伤害并再次施加冰霜控制（灼燃专精仍只减速）。";
                case EquipmentMechanic.CinderTrail: return "陨星直接伤害 -20%；落点留下2秒火场，总计40%基础伤害。每次陨星仅触发一次碎冰或灼燃。";
                case EquipmentMechanic.ReturningBlade: return "普攻首击伤害 -15%；每1.5秒回刃弹向4米内另一个目标，造成55%基础伤害。";
                case EquipmentMechanic.VenomSpread: return "毒素引爆加成伤害 -20%；每2秒向附近最多2个目标传播1层毒素。";
                case EquipmentMechanic.TwinSummonResonance: return "普通召唤上限改为2；召唤物伤害 +35%、生命 -15%。两只伙伴存活时每3秒共鸣追加35%基础伤害。";
                default: return "装备机制只在穿戴且职业匹配时生效。";
            }
        }

        public static string MechanicSource(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic)) return "无";
            return "本职业首通自选；遗迹首领25%机制掉落；普通史诗/传说掉落12%机制概率；12枚星烬碎片定向兑换。每次通关获得3枚碎片。";
        }

        public static string SpecializationName(ElementalistSpecialization specialization)
        {
            return specialization == ElementalistSpecialization.Shatter ? "碎冰" : specialization == ElementalistSpecialization.Burn ? "灼燃" : "均衡";
        }

        public static string SpecializationDescription(ElementalistSpecialization specialization)
        {
            if (specialization == ElementalistSpecialization.Shatter) return "陨星直接伤害 -15%；消耗冰霜印记时碎冰追加100%基础伤害。与灼燃互斥，营地免费切换。";
            if (specialization == ElementalistSpecialization.Burn) return "新星只减速、不冻结或施加冰霜印记；陨星直接伤害 -20%，附加3秒灼烧，总计90%基础伤害。营地免费切换。";
            return "保留原有冰霜控制；陨星消耗冰霜印记，碎冰追加50%基础伤害。专精仅改变元素师，营地免费切换。";
        }

        public static string ClassSignatureDescription(HeroClass hero)
        {
            if (hero == HeroClass.Vanguard) return "完美闪避真正避开攻击时回复12能量；2秒内下一次普攻造成175%伤害。";
            if (hero == HeroClass.Arcanist) return "未学习冰霜新星时，对同一目标连续命中3次普攻触发0.35秒霜冻与减速；切换目标重计，触发间隔1.2秒，首领抗性生效。";
            if (hero == HeroClass.Ranger) return "普攻叠加最多3层毒素；扇形箭消耗3层引爆，每个目标每次施法仅触发一次。";
            return "开局拥有持续陪伴的弱灵狼；死亡12秒后回归。普攻命中可指挥近处伙伴集火1.8秒，学习灵狼契约后由正式契约替代。";
        }

        public static string MasteryName(MasteryType mastery)
        {
            return mastery == MasteryType.Offense ? "破敌精通" : mastery == MasteryType.Vitality ? "生命精通" : "坚壁精通";
        }

        public static string MasteryDescription(MasteryType mastery)
        {
            return (mastery == MasteryType.Offense ? "每点攻击 +0.5%" : mastery == MasteryType.Vitality ? "每点生命 +0.75%" : "每点防御 +0.75") + "；上限23点。100级且十个技能全部觉醒后开放；营地可免费重置。";
        }
    }

    /// <summary>Durable chest receipt. Unity serializes the fields; nullable view properties
    /// let the reveal UI distinguish a gold-only reward without unsupported nullable fields.</summary>
    [Serializable]
    public class ChestReward
    {
        public string id;
        public int choice;
        public int gold;
        public int rarityIndex = -1;
        public int slotIndex = -1;
        public string name;
        public bool duplicate;
        public string summary;
        public string Id { get { return id; } }
        public int Gold { get { return gold; } }
        public Rarity? Rarity { get { return rarityIndex < 0 ? (Emberfall.Rarity?)null : (Emberfall.Rarity)rarityIndex; } }
        public FashionSlot? Slot { get { return slotIndex < 0 ? (FashionSlot?)null : (FashionSlot)slotIndex; } }
        public string Name { get { return name; } }
        public bool Duplicate { get { return duplicate; } }
    }

    [Serializable]
    public class FashionData
    {
        public string id;
        public FashionSlot slot;
        public Rarity rarity;
        public string name;
    }
    public enum EnemyKind { Slime, Goblin, Wisp, Guardian }
    public enum ZoneKind { Wilderness, Dungeon }
    public enum SkillCategory { Damage, Control, Mobility, Buff, Defense, Healing }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public ItemSlot slot;
        public Rarity rarity;
        public int level;
        public int attack;
        public int defense;
        public int health;
        public int upgradeLevel;
        public EquipmentMechanic mechanic;
        public bool locked;
        // Persistent origins make changing a rank reversible without repeatedly
        // rounding already-upgraded attributes. Legacy saves initialize these once.
        public bool upgradeBaseInitialized;
        public int baseAttack;
        public int baseDefense;
        public int baseHealth;
        public int upgradeAnchorLevel;
        public int upgradeAnchorAttack;
        public int upgradeAnchorDefense;
        public int upgradeAnchorHealth;
    }

    [Serializable]
    public class GameProfile
    {
        public int version = 1;
        public HeroClass heroClass;
        public int level = 1;
        public int xp;
        public int gold = 60;
        public int potions = 5;
        public int skillPoints;
        public int tutorialMask;
        public ElementalistSpecialization specialization;
        public int[] masteryRanks = new int[3];
        // Slot training is the sole enhancement authority. Item upgradeLevel is
        // only the current equipped-stat cache; legacy anchors preserve item bases.
        public int[] slotUpgradeRanks = new int[3];
        public bool slotUpgradesInitialized;
        public int mechanicMaterials;
        public int materialRewardedClears;
        public bool firstClearRewardClaimed;
        public bool pendingFirstClearReward;
        public List<EquipmentMechanic> discoveredMechanics = new List<EquipmentMechanic>();
        public List<ItemData> pendingLoot = new List<ItemData>();
        public List<ItemData> recoveryLoot = new List<ItemData>();
        public bool autoSellCommon;
        public bool autoSellRare;
        public int[] skillRanks = new int[GameBalance.SkillCount];
        public int[] equippedSkills = GameBalance.DefaultLoadout();
        public int hotbarPage;
        public int[] hotbarKeys = (int[])GameBalance.DefaultHotbarKeys.Clone();
        public int kills;
        public int clearedRuns;
        public int bestFloor;
        public List<ItemData> inventory = new List<ItemData>();
        public string weaponId;
        public string armorId;
        public string relicId;
        public List<FashionData> fashions = new List<FashionData>();
        public string wingsFashionId;
        public string weaponFashionId;
        public bool pendingFashionChest;
        public ChestReward lastChestReward;
        public bool pendingChestReveal;
    }

    public struct StatBlock
    {
        public float MaxHealth;
        public float Damage;
        public float Armor;
        public float MoveSpeed;
        public float CritChance;
    }

    public static class GameBalance
    {
        public const int SkillCount = 10;
        public const int HotbarSize = 10;
        public const int HotbarPotion = -2;
        public const int HotbarPages = 3;
        public static readonly int[] DefaultHotbarKeys = { 122, 120, 99, 118, 98, 49, 50, 51, 52, 53 };
        public static readonly string[] ClassNames = { "剑卫", "元素师", "游侠", "唤灵师" };
        public static readonly string[] ClassDescriptions = {
            "挥剑近战 · 旋风斩击 · 坚韧生存",
            "连击霜印 · 冰霜控制 · 陨星爆发",
            "灵巧射击 · 散射箭雨 · 灵活游走",
            "契约召唤 · 灵狼星灵 · 协同作战"
        };
        public static readonly Color[] ClassColors = {
            new Color(1f, .65f, .26f), new Color(.4f, .7f, 1f), new Color(.38f, .88f, .65f), new Color(.83f, .63f, 1f)
        };
        public static readonly int[] SkillRequiredLevels = { 2, 4, 6, 4, 10, 6, 13, 20, 13, 30 };
        public static readonly int[][] SkillPrerequisites = {
            new int[0], new[] { 0 }, new[] { 1 }, new[] { 0 }, new[] { 2 },
            new[] { 3 }, new[] { 5 }, new[] { 4 }, new[] { 5 }, new[] { 7, 6 }
        };
        public static int SkillTreeRow(int skill) { return new[] { 0, 1, 2, 1, 3, 2, 4, 5, 4, 6 }[skill]; }
        public static float SkillTreeColumn(int skill) { return new[] { 1f, 0f, 0f, 2f, 0f, 2f, 1f, 0f, 2f, 1f }[skill]; }
        public static string PrerequisiteDescription(HeroClass hero, int skill)
        {
            int[] parents = SkillPrerequisites[skill];
            if (parents.Length == 0) return "起始技能 · 无前置";
            string result = "前置：";
            for (int i = 0; i < parents.Length; i++) result += (i == 0 ? "" : " + ") + SkillName(hero, parents[i]);
            return result;
        }
        // Budgets reflect damage coverage, control uptime and protection, rather
        // than position in the skill tree. IDs stay stable for existing saves.
        // Dash and chain attacks cycle faster than persistent fields; shields
        // leave an exposed interval even at rank three. Summons have a hard cap.
        private static readonly float[,] ClassSkillCooldowns = {
            { 5f, 9f, 18f, 0f, 28f, 11f, 32f, 16f, 0f, 42f },
            { 10f, 11f, 20f, 0f, 8f, 26f, 34f, 22f, 0f, 45f },
            { 6f, 12f, 19f, 0f, 12f, 18f, 32f, 16f, 0f, 40f },
            { 7f, 16f, 14f, 0f, 12f, 28f, 36f, 21f, 0f, 46f }
        };
        private static readonly float[,] ClassSkillEnergyCosts = {
            { 12f, 20f, 34f, 0f, 30f, 24f, 28f, 38f, 0f, 64f },
            { 18f, 28f, 38f, 0f, 24f, 28f, 30f, 42f, 0f, 68f },
            { 14f, 20f, 34f, 0f, 22f, 30f, 28f, 42f, 0f, 64f },
            { 16f, 28f, 30f, 0f, 28f, 32f, 36f, 40f, 0f, 70f }
        };
        private static readonly string[,] SkillNames = {
            { "旋风斩", "裂地冲击", "剑刃风暴", "剑术精研", "圣盾反击", "破军突进", "生命战旗", "大地崩裂", "不屈意志", "终焉裁决" },
            { "冰霜新星", "陨星术", "奥术风暴", "奥能亲和", "雷霆锁链", "冰晶护体", "奥术回流", "虚空漩涡", "法力屏障", "天灾终章" },
            { "扇形箭", "震荡陷阱", "天幕箭雨", "弱点洞察", "逐风步", "毒蔓牢笼", "森林祈愿", "幻影连射", "灵风庇佑", "万箭归星" },
            { "灵能冲击", "荆棘牢笼", "灵狼契约", "灵魂共鸣", "星灵契约", "灵魂护盾", "回春共鸣", "引力印记", "灵体庇护", "远古树灵" }
        };
        private static readonly string[,] SkillDescriptions = {
            {
                "旋转斩击周围敌人。低消耗、短冷却，觉醒后牵引收束。", "向前方重击，击退并击倒敌人，打断其攻击。", "连续释放剑气，切割周围的敌人。",
                "被动：永久提高攻击与防御，学习后自动生效。", "展开护盾减轻伤害，并以圣光反击周围敌人。", "向前突进并连续斩击，撕开敌阵。",
                "树立生命战旗，持续恢复生命；升阶获得防护与回复能量。", "沿前方逐段引爆地脉，将命中的敌人击飞浮空；首领控制时间缩短。", "被动：濒危时自动触发减伤防护；触发后有独立内置冷却。",
                "巨剑裁决与多段剑阵爆发，终结大范围敌群。集中消耗战意，适合聚怪后的爆发。"
            },
            {
                "冻结身边敌人，解冻后仍暂时减速40%；首领控制持续时间缩短。", "在瞄准地点降下陨星，造成范围爆发。", "在瞄准地点制造持续的奥术风暴。",
                "被动：永久提高法术攻击与生命，学习后自动生效。", "雷霆在敌人间跳跃，逐个造成伤害并短暂眩晕。", "以冰晶护盾保护自身，减轻伤害并释放寒冰脉冲。",
                "回收奥术之力，持续恢复生命；升阶获得防护与回复能量。", "创造虚空漩涡，将敌人吸向中心并反复撕裂。", "被动：受伤时自动生成法力屏障并回复少量能量，具有内置冷却。",
                "多重星环汇聚，陨星与雷霆引爆整片战场。高奥能消耗，兼顾范围伤害与控制。"
            },
            {
                "向前方发射多支穿透箭矢。短冷却，适合清理敌群。", "在瞄准地点引爆陷阱，伤害并眩晕敌人。", "向瞄准地点持续倾泻箭雨。",
                "被动：永久提高暴击几率和移动速度，学习后自动生效。", "后撤脱离危险，同时获得机动增益并释放追击箭矢。", "毒蔓使敌人减速并叠加中毒，最多3层；离开毒区后毒伤仍会持续。",
                "召唤自然之力持续恢复生命；升阶获得防护与回复能量。", "锁定选区内的一名敌人持续追射，并使其受到的伤害提高12%至20%。目标死亡后不自动转锁。", "被动：受伤时自动短暂无敌并提高移动速度，具有内置冷却。",
                "星弓展开，巨量光羽与箭雨汇聚于目标。集中消耗专注，适合敌群密集时使用。"
            },
            {
                "向前方释放灵能冲击，造成扇形伤害并将敌人轰开。", "在目标地点生长荆棘，使敌人减速并持续中毒；首领减速效果降低。", "召唤一只灵狼近战攻击敌人，持续18秒；普通召唤物总上限4只。",
                "被动：提高自身攻击，召唤物继承强化后的攻击。学习后永久生效。", "召唤一只星灵远程攻击敌人，持续18秒；普通召唤物总上限4只。", "灵魂护盾持续6秒，受到的伤害减少35%；升阶延长保护并增强减伤。",
                "5秒内治疗自身与召唤物，升阶获得防护与回复能量。", "在目标地点施加引力印记，持续聚拢敌人，结束时将敌人击飞；首领控制效果降低。", "被动：受伤时触发灵体庇护，短暂减伤并回复能量，具有独立内置冷却。",
                "召唤一只远古树灵守卫，持续12秒，使用范围重击并击倒敌人；最多存在1只树灵。"
            }
        };
        private static readonly string[,] ReinforcedEffects = {
            { "追加一段旋斩；伤害+30%，范围+15%。", "追加震荡，延长眩晕；伤害+30%，范围+15%。", "剑刃风暴持续3秒；伤害+30%，范围+15%。", "", "反击护盾持续8秒，减伤65%，反击范围扩大。", "冲锋末端追加爆发；伤害+30%，路径扩大。", "", "裂地增加至7段，逐段击飞；伤害+30%，范围+15%。", "", "裁决增加至7段；伤害+30%，范围+15%。" },
            { "释放双重新星；伤害+30%，范围+15%。", "双陨星连续落下；伤害+30%，范围+15%。", "风暴脉冲加快；伤害+30%，范围+15%。", "", "连锁最多8个目标；伤害+30%，范围+15%。", "护体持续8秒，减伤45%，冰环每1.2秒触发。", "", "漩涡攻击增加至11次；伤害+30%，范围+15%。", "", "天灾增加至7段；伤害+30%，范围+15%。" },
            { "增加至7支箭矢；伤害+30%，作用范围+15%。", "陷阱追加余震；伤害+30%，范围+15%。", "箭雨持续5秒；伤害+30%，范围+15%。", "", "后撤射出4支穿透箭，获得6秒机动与普攻增益。", "毒区持续更久；伤害+30%，范围+15%。", "", "连续射出17支穿透箭；伤害+30%，射程扩大。", "", "箭幕增加至10段；伤害+30%，范围+15%。" },
            { "灵能冲击伤害+30%，范围+15%，击退敌人。", "荆棘伤害+30%，范围+15%，延长减速与中毒覆盖。", "灵狼持续21秒，继承强化攻击，伤害+30%。", "", "星灵持续21秒，继承强化攻击，伤害+30%。", "护盾持续8秒，减伤45%。", "", "引力印记伤害+30%，范围+15%，结束时击飞。", "", "树灵持续14秒，重击伤害+30%，范围扩大。" }
        };
        private static readonly string[,] AwakenedEffects = {
            { "三段旋斩并牵引收束；伤害+60%，范围+35%。", "追加终结冲击与强化眩晕；伤害+60%，范围+35%。", "持续3.6秒，牵引敌人并追加收尾爆发。", "", "反击护盾持续10秒，减伤70%，多层圣光反击。", "冲锋终点爆发，起点追加残影冲击。", "", "8段裂地逐段击飞，并追加末端爆破；伤害+60%，范围+35%。", "", "8段裁决，终击留下2秒剑气领域。" },
            { "双重新星后追加8枚冰片，扩大范围并强化冻结。", "双陨星后留下2秒灼烧领域。", "高速脉冲聚拢敌人，并追加收束爆发。", "", "连锁最多10个目标，每个节点追加溅射。", "护体持续10秒，减伤55%，每秒释放控制冰环。", "", "13次漩涡攻击，更强吸附并追加终结爆发。", "", "8段天灾，终击留下聚怪元素领域。" },
            { "9支穿透箭，命中追加爆裂效果。", "引爆前吸附敌人，随后追加余震。", "持续6秒箭雨，并在结束时追加爆发。", "", "5支追踪箭与残影陷阱，获得7秒机动与普攻增益。", "毒区吸附敌人，并在结束时追加爆裂。", "", "连续射出20支追踪穿透箭，强化覆盖能力。", "", "11段箭幕，终爆追加12支放射箭。" },
            { "灵能冲击伤害+60%，范围+35%，将敌群轰开。", "荆棘伤害+60%，范围+35%，强化毒伤与区域控制。", "灵狼持续24秒，继承觉醒攻击，伤害+60%。", "", "星灵持续24秒，继承觉醒攻击，伤害+60%。", "护盾持续10秒，减伤55%。", "", "引力印记伤害+60%，范围+35%，聚怪后以击飞结束。", "", "树灵持续16秒，重击伤害+60%，范围扩大。" }
        };
        public static string ClassName(HeroClass value) { return ClassNames[(int)value]; }
        public static Color ClassColor(HeroClass value) { return ClassColors[(int)value]; }
        public static string SkillName(HeroClass value, int slot) { return SkillNames[(int)value, slot]; }
        public static string SkillDescription(HeroClass value, int slot) { return SkillDescriptions[(int)value, slot]; }
        public static string EnergyName(HeroClass value) { return new[] { "战意", "奥能", "专注", "灵力" }[(int)value]; }
        public static bool IsPassive(int skill) { return skill == 3 || skill == 8; }
        public static int SkillRankRequiredLevel(int skill, int rank) { return SkillRequiredLevels[skill] + (rank <= 1 ? 0 : rank == 2 ? 8 : 18); }
        public static float SkillRangeMultiplier(int rank) { return rank <= 1 ? 1f : rank == 2 ? 1.15f : 1.35f; }
        public static string SkillRankName(int rank) { return rank <= 0 ? "未习得" : rank == 1 ? "初习" : rank == 2 ? "强化" : "觉醒"; }
        public static SkillCategory GetSkillCategory(HeroClass hero, int skill)
        {
            if (skill == 3) return SkillCategory.Buff;
            if (skill == 8) return SkillCategory.Defense;
            if (skill == 6) return SkillCategory.Healing;
            if (hero == HeroClass.Vanguard)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Defense;
                if (skill == 5) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Arcanist)
            {
                if (skill == 0 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Defense;
            }
            else if (hero == HeroClass.Ranger)
            {
                if (skill == 1 || skill == 5) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Summoner)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Defense;
            }
            return SkillCategory.Damage;
        }
        public static string CategoryName(SkillCategory category) { return new[] { "输出", "控制", "位移", "增益", "防御", "治疗" }[(int)category]; }
        public static string SkillEvolution(HeroClass hero, int skill, int rank)
        {
            int stage = Math.Max(1, Math.Min(3, rank)) - 1;
            if (skill == 3)
            {
                if (hero == HeroClass.Vanguard) return new[] { "攻击 +8%，防御 +2。永久生效。", "攻击 +14%，防御 +4。", "攻击 +22%，防御 +7。" }[stage];
                if (hero == HeroClass.Arcanist) return new[] { "攻击 +6%，最大生命 +4%。", "攻击 +11%，最大生命 +7%。", "攻击 +18%，最大生命 +10%。" }[stage];
                if (hero == HeroClass.Summoner) return new[] { "攻击 +6%，召唤物继承自身攻击。永久生效。", "攻击 +11%，召唤物继承强化后的攻击。", "攻击 +18%，召唤物继承强化后的攻击。" }[stage];
                return new[] { "暴击 +4%，移动速度 +3%。", "暴击 +7%，移动速度 +6%。", "暴击 +12%，移动速度 +10%。" }[stage];
            }
            if (skill == 8)
            {
                if (hero == HeroClass.Vanguard) return new[] { "濒危时减伤30%，持续3秒；内置冷却45秒。", "减伤40%，持续4秒；内置冷却38秒。", "减伤50%/5秒并自动反击；内置冷却30秒。" }[stage];
                if (hero == HeroClass.Arcanist) return new[] { "受伤后减伤40%/3秒，回复4能量；冷却45秒。", "减伤50%/4秒，回复6能量；冷却38秒。", "减伤60%/5秒，回复8能量并释放冰环；冷却30秒。" }[stage];
                if (hero == HeroClass.Summoner) return new[] { "受伤后减伤35%/3秒，回复4灵力；冷却45秒。", "减伤45%/4秒，回复6灵力；冷却38秒。", "减伤55%/5秒，回复8灵力并震慑周围敌人1.5秒；冷却30秒。" }[stage];
                return new[] { "受伤后无敌0.25秒、移速+15%/3秒；冷却45秒。", "无敌0.4秒、移速+20%/4秒；冷却38秒。", "无敌0.55秒、移速+25%/5秒，回复8能量；冷却30秒。" }[stage];
            }
            if (skill == 6)
            {
                if (hero == HeroClass.Summoner) return new[] { "5秒内为自身与召唤物恢复30%最大生命。", "5秒为自身与召唤物恢复42%生命，自身获得18%减伤。", "5秒为自身与召唤物恢复55%生命，自身获得25%减伤并回复8灵力。" }[stage];
                return new[] { "5秒内恢复30%最大生命，不造成伤害。", "5秒恢复42%生命，获得18%减伤。", "5秒恢复55%生命，获得25%减伤并回复8能量。" }[stage];
            }
            if (stage == 0) return SkillDescription(hero, skill);
            return stage == 1 ? ReinforcedEffects[(int)hero,skill] : AwakenedEffects[(int)hero,skill];
        }
        public static float SkillCooldown(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return ClassSkillCooldowns[(int)hero, skill];
        }
        public static float SkillEnergyCost(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return ClassSkillEnergyCosts[(int)hero, skill];
        }
        public static float EffectiveCooldown(HeroClass hero, int skill, int rank)
        {
            return SkillCooldown(hero, skill) * (1f - (Math.Max(1, Math.Min(3, rank)) - 1) * .07f);
        }
        private static void ValidateSkillBudget(HeroClass hero, int skill)
        {
            if ((int)hero < 0 || (int)hero >= ClassSkillCooldowns.GetLength(0)) throw new ArgumentOutOfRangeException(nameof(hero));
            if (skill < 0 || skill >= SkillCount) throw new ArgumentOutOfRangeException(nameof(skill));
        }
        public static int[] DefaultLoadout()
        {
            int[] result = new int[HotbarSize * HotbarPages];
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            int slot = 0;
            for (int skill = 0; skill < SkillCount; skill++) if (!IsPassive(skill)) result[slot++] = skill;
            return result;
        }
        public static bool IsBindableKey(int key)
        {
            if (key == 97 || key == 100 || key == 102 || key == 104 || key == 105 || key == 106 || key == 107 || key == 115 || key == 116 || key == 119) return false;
            return (key >= 97 && key <= 122) || (key >= 48 && key <= 57) || (key >= 282 && key <= 293);
        }
        public static string KeyName(int key)
        {
            if (key >= 97 && key <= 122) return ((char)(key - 32)).ToString();
            if (key >= 48 && key <= 57) return ((char)key).ToString();
            if (key >= 282 && key <= 293) return "F" + (key - 281);
            return "未绑定";
        }
        public static string SlotName(ItemSlot slot) { return new[] { "武器", "护甲", "饰品" }[(int)slot]; }
        public static string RarityName(Rarity rarity) { return new[] { "普通", "稀有", "史诗", "传说" }[(int)rarity]; }
        public static Color RarityColor(Rarity rarity) {
            return new[] { new Color(.76f,.8f,.84f), new Color(.35f,.65f,1f), new Color(.77f,.43f,1f), new Color(1f,.72f,.26f) }[(int)rarity];
        }
        public static int XpToNext(int level) { return 60 + (level - 1) * 30; }
    }
}
