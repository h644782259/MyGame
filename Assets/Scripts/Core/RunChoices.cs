using System;
using System.Collections.Generic;

namespace Emberfall
{
    public enum RunBlessing { DodgeShock, ChargedWard, InterruptFlow, MarkedPursuit, ExecutionMend, RiskContract }

    /// <summary>Per-expedition choices: never written into the permanent character stats.</summary>
    public sealed class RunChoices
    {
        private readonly HashSet<RunBlessing> active = new HashSet<RunBlessing>();
        private RunBlessing[] offer = new RunBlessing[0];
        public bool AwaitingChoice { get; private set; }
        public int CompletedWave { get; private set; }
        public RunBlessing[] Offer { get { return (RunBlessing[])offer.Clone(); } }
        public IEnumerable<RunBlessing> Active { get { return active; } }
        public bool Has(RunBlessing blessing) { return active.Contains(blessing); }
        public void Reset() { active.Clear(); offer = new RunBlessing[0]; AwaitingChoice = false; CompletedWave = 0; }

        public void Prepare(int completedWave, HeroClass hero, int[] skills, int seed)
        {
            if (completedWave < 1 || completedWave > 2 || AwaitingChoice || completedWave <= CompletedWave) return;
            CompletedWave = completedWave;
            var compatible = new List<RunBlessing>();
            var remaining = new List<RunBlessing>();
            foreach (RunBlessing blessing in Enum.GetValues(typeof(RunBlessing)))
            {
                if (active.Contains(blessing)) continue;
                remaining.Add(blessing);
                if (IsCompatible(blessing, hero, skills)) compatible.Add(blessing);
            }
            var random = new Random(seed);
            var result = new List<RunBlessing>();
            // Universal choices guarantee relevance even on an unlearned/legacy build.
            RunBlessing guaranteed = compatible[random.Next(compatible.Count)];
            result.Add(guaranteed); remaining.Remove(guaranteed);
            while (result.Count < 3 && remaining.Count > 0)
            {
                int index = random.Next(remaining.Count); result.Add(remaining[index]); remaining.RemoveAt(index);
            }
            for (int i = result.Count - 1; i > 0; i--) { int j = random.Next(i + 1); RunBlessing temp = result[i]; result[i] = result[j]; result[j] = temp; }
            offer = result.ToArray(); AwaitingChoice = true;
        }

        public bool Choose(int index)
        {
            if (!AwaitingChoice || index < 0 || index >= offer.Length) return false;
            active.Add(offer[index]); AwaitingChoice = false; offer = new RunBlessing[0]; return true;
        }

        public static bool IsCompatible(RunBlessing blessing, HeroClass hero, int[] ranks)
        {
            if (blessing == RunBlessing.DodgeShock || blessing == RunBlessing.ExecutionMend || blessing == RunBlessing.RiskContract) return true;
            if (ranks == null) return false;
            if (blessing == RunBlessing.MarkedPursuit) return hero == HeroClass.Ranger && ranks.Length > 7 && ranks[7] > 0;
            if (blessing == RunBlessing.ChargedWard) return (hero == HeroClass.Arcanist && ranks.Length > 1 && ranks[1] > 0) ||
                (hero == HeroClass.Vanguard && ranks.Length > 7 && ranks[7] > 0) || (hero == HeroClass.Summoner && ranks.Length > 4 && ranks[4] > 0) || (ranks.Length > 9 && ranks[9] > 0);
            return ranks.Length > 1 && ranks[1] > 0 || hero == HeroClass.Arcanist && ranks.Length > 0 && ranks[0] > 0;
        }

        public static string Name(RunBlessing blessing)
        {
            return new[] { "踏风余震", "蓄星护佑", "断势回流", "猎印追风", "破阵甘霖", "星烬契约" }[(int)blessing];
        }
        public static string Description(RunBlessing blessing)
        {
            return new[] {
                "闪现后2秒内，下次普攻命中释放一次小范围冲击。",
                "完成一次蓄力施法，获得2秒25%减伤。取消施法不触发。",
                "真正打断敌人预警攻击时回复12能量，冷却1秒。首领不可硬打断。",
                "击杀带狩猎标记的目标，获得2.5秒20%移速。",
                "击败每波的守卫或首领，恢复12%最大生命（每波一次）。",
                "本局受到伤害+15%，通关金币+30%；撤离或失败不发额外奖励。"
            }[(int)blessing];
        }
    }
}
