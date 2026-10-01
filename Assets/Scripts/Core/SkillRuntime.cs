using System;

namespace Emberfall
{
    /// <summary>Combat resources are keyed by learned skill, never by page or keyboard key.</summary>
    public sealed class SkillRuntime
    {
        public const float MaximumEnergy = 100f;
        public const float EnergyPerSecond = 4f;
        public float Energy { get; private set; } = MaximumEnergy;
        private readonly float[] cooldowns = new float[GameBalance.SkillCount];

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
            float cost = GameBalance.SkillEnergyCosts[skill];
            if (Energy < cost) return false;
            Energy -= cost;
            cooldowns[skill] = GameBalance.EffectiveCooldown(skill, rank);
            return true;
        }

        public void RestoreEnergy(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            Energy = Math.Min(MaximumEnergy, Energy + amount);
        }

        public void FillEnergy() { Energy = MaximumEnergy; }
    }
}
