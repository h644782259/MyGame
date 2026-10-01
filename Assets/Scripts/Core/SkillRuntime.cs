using System;

namespace Emberfall
{
    /// <summary>Combat resources are keyed by learned skill, never by page or keyboard key.</summary>
    public sealed class SkillRuntime
    {
        public const float MaximumEnergy = 100f;
        public const float EnergyPerSecond = 4f;
        public HeroClass HeroClass { get; }
        public float Energy { get; private set; } = MaximumEnergy;
        private readonly float[] cooldowns = new float[GameBalance.SkillCount];

        public SkillRuntime(HeroClass heroClass)
        {
            if ((int)heroClass < 0 || (int)heroClass >= GameBalance.ClassNames.Length)
                throw new ArgumentOutOfRangeException(nameof(heroClass));
            HeroClass = heroClass;
        }

        public float Remaining(int skill)
        {
            return skill >= 0 && skill < cooldowns.Length ? cooldowns[skill] : 0;
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Math.Max(0, cooldowns[i] - deltaTime);
            RestoreEnergy(deltaTime * EnergyPerSecond);
        }

        public bool TryConsume(int skill, int rank)
        {
            if (skill < 0 || skill >= cooldowns.Length || GameBalance.IsPassive(skill) || rank < 1 || rank > 3 || cooldowns[skill] > 0) return false;
            float cost = GameBalance.SkillEnergyCost(HeroClass, skill);
            if (Energy < cost) return false;
            Energy -= cost;
            cooldowns[skill] = GameBalance.EffectiveCooldown(HeroClass, skill, rank);
            return true;
        }

        public void RestoreEnergy(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Energy = Math.Min(MaximumEnergy, Energy + amount);
        }

        public void ReduceCooldowns(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Math.Max(0, cooldowns[i] - seconds);
        }

        public void FillEnergy() { Energy = MaximumEnergy; }
    }
}
