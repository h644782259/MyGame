using System;
using System.Collections.Generic;

namespace Emberfall
{
    public static class CompanionRules
    {
        public const float CooperationWindow = 1.5f;
        public const float CooperationCooldown = 3f;
        public const float CooperationDamage = .35f;
        public const float AreaDamageTaken = .55f;
        public const float RecallDamageTaken = .5f;
        public const float RecallDuration = 3f;
        public const float RestThreatRadius = 16f; // Covers the boss's full ranged engagement distance.
        public static bool CoordinatedTarget(bool playerFocusMatches, bool livingCommandMatches)
        { return playerFocusMatches || livingCommandMatches; }
        public static bool CanTransfer(bool permanent, float health, bool active)
        { return permanent && active && health > 0 && !float.IsNaN(health) && !float.IsInfinity(health); }
        public static bool ShouldCreatePartner(bool livingPartner) { return !livingPartner; }
        public static bool PermanentPartner(bool foundation, int form, bool packRoute) { return foundation || (form == 1 && !packRoute); }
        public static int PackReinforcements(int currentCount, int rank, bool twinContract)
        { return Math.Max(0, Math.Min(NormalCapacity(twinContract) - Math.Max(0, currentCount), rank >= 3 ? 3 : 2)); }
        public static float PackLifetime(int rank) { return 8f + Math.Max(1, Math.Min(3, rank)) * 2f; }
        public static int NormalCapacity(bool twinContract) { return twinContract ? 2 : 4; }
        public static float DamageMultiplier(bool twinContract) { return twinContract ? 1.6f : 1f; }
        public static float HealthMultiplier(bool twinContract) { return twinContract ? 1.2f : 1f; }
        public static float RankPower(int rank) { return rank <= 0 ? .55f : 1f + (Math.Min(3, rank) - 1) * .3f; }
        public static float CommandMultiplier(int rank) { return 1.25f + Math.Max(0, Math.Min(3, rank)) * .1f; }
        public static float HealthFraction(int form, int rank, bool foundation)
        {
            rank = Math.Max(0, Math.Min(3, rank));
            if (form == 2) return 1.2f + Math.Max(0, rank - 1) * .12f;
            return foundation ? .45f + rank * .1f : .42f + rank * .08f;
        }
        public static float DamageTaken(float amount, bool areaAttack, bool recalling)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return 0;
            return amount * (areaAttack ? AreaDamageTaken : 1f) * (recalling ? RecallDamageTaken : 1f);
        }
    }

    // Only confirmed impacts enter this tracker. A second pet of the same type,
    // a different target, launches, and expired marks cannot trigger cooperation.
    public sealed class CompanionCooperationTracker<T> where T : class
    {
        private sealed class Hits
        {
            public readonly float[] Times = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
            public float Last;
        }
        private readonly Dictionary<T, Hits> targets = new Dictionary<T, Hits>();
        private readonly List<T> expired = new List<T>();
        private float nextProc, latestTime;
        public void Clear() { targets.Clear(); nextProc = latestTime = 0; }
        public bool RegisterHit(T target, int form, float combatTime)
        {
            if (target == null || form < 0 || form > 2 || float.IsNaN(combatTime) || float.IsInfinity(combatTime) || combatTime < latestTime) return false;
            latestTime = combatTime;
            expired.Clear();
            foreach (var pair in targets)
                if (combatTime - pair.Value.Last > CompanionRules.CooperationWindow) expired.Add(pair.Key);
            foreach (T stale in expired) targets.Remove(stale);
            Hits hits;
            if (!targets.TryGetValue(target, out hits)) { hits = new Hits(); targets.Add(target, hits); }
            bool partnerHit = false;
            for (int kind = 0; kind < hits.Times.Length; kind++)
                if (kind != form && combatTime - hits.Times[kind] <= CompanionRules.CooperationWindow) partnerHit = true;
            hits.Times[form] = hits.Last = combatTime;
            if (!partnerHit || combatTime < nextProc) return false;
            targets.Remove(target);
            nextProc = combatTime + CompanionRules.CooperationCooldown;
            return true;
        }
    }
}
