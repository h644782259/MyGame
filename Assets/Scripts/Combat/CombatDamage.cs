namespace Emberfall
{
    // The roll travels with its damage through delayed hits. Plain damage such
    // as poison, burns and pet attacks stays non-critical without cosmetic rolls.
    public readonly struct CombatDamage
    {
        public float Amount { get; }
        public bool IsCritical { get; }
        public CombatDamage(float amount, bool isCritical)
        { Amount = amount; IsCritical = isCritical; }
        public static CombatDamage Roll(float amount, float chance, float roll)
        {
            bool critical = roll < chance;
            return new CombatDamage(amount * (critical ? 1.65f : 1f), critical);
        }
        public CombatDamage WithoutCritical() { return new CombatDamage(IsCritical ? Amount / 1.65f : Amount, false); }
        public static CombatDamage operator *(CombatDamage value, float multiplier) { return new CombatDamage(value.Amount * multiplier, value.IsCritical); }
        public static implicit operator CombatDamage(float amount)
        { return new CombatDamage(amount, false); }
    }
}
