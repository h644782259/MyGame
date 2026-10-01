using System;

namespace Emberfall
{
    /// <summary>Engine-independent encounter pacing. Every follow-up keeps its own readable windup.</summary>
    public static class BossAttackPolicy
    {
        public enum Move { Slam, Charge, Fan }
        public const float CloseRange = 3.8f;
        public const float ChargeRange = 8.5f;
        public const float EngageRange = 16f;
        public const float ChargeSpeed = 11f;
        public const float ChargeHalfWidth = 1.3f;
        public const float SlamRadius = 4.05f;
        public const float ComboGap = .32f;

        public static bool CanEngage(float distance)
        {
            return !float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= 0 && distance <= EngageRange;
        }

        public static bool IsEnraged(float health, float maximumHealth)
        {
            return !float.IsNaN(maximumHealth) && !float.IsInfinity(maximumHealth) && maximumHealth > 0 && health > 0 && health <= maximumHealth * .5f;
        }

        public static Move Select(float distance)
        {
            if (distance <= CloseRange) return Move.Slam;
            if (distance <= ChargeRange) return Move.Charge;
            return Move.Fan;
        }

        public static Move Select(float distance, Move previous, int repeated)
        {
            Move preferred = Select(distance);
            if (repeated < 1 || preferred != previous) return preferred;
            if (preferred == Move.Slam) return distance > 2f ? Move.Charge : Move.Fan;
            if (preferred == Move.Charge) return Move.Fan;
            // A far boss advances after a fan; it never announces a charge that cannot reach.
            return distance <= ChargeRange + 1f ? Move.Charge : Move.Fan;
        }
        public static bool ShouldAdvance(float distance, Move previous, int repeated)
        { return distance > ChargeRange && previous == Move.Fan && repeated > 0; }

        public static Move FollowUp(Move previous, float distance)
        {
            // Ground closes after a charge; a slam opens into a ranged fan.
            // If the player has already moved away, do not start an unreachable slam.
            if (previous == Move.Slam) return Move.Fan;
            return Select(distance, previous, 1);
        }

        public static float Windup(Move move, bool followUp)
        {
            float duration = move == Move.Charge ? 1.05f : move == Move.Fan ? .95f : .9f;
            return followUp ? Math.Max(.65f, duration - .18f) : duration;
        }

        public static float ChargeContactDelay(float along, float lateral, float remainingWindup)
        {
            if (float.IsNaN(along) || float.IsInfinity(along) || float.IsNaN(lateral) || float.IsInfinity(lateral) || float.IsNaN(remainingWindup) || float.IsInfinity(remainingWindup) || Math.Abs(lateral) >= ChargeHalfWidth) return float.PositiveInfinity;
            float contactDistance = along - (float)Math.Sqrt(Math.Max(0, ChargeHalfWidth * ChargeHalfWidth - lateral * lateral));
            return Math.Max(0, remainingWindup) + Math.Max(0, contactDistance) / ChargeSpeed;
        }

        public static bool IsPerfectDodgeTiming(float untilImpact, float window)
        {
            return !float.IsNaN(untilImpact) && !float.IsInfinity(untilImpact) && !float.IsNaN(window) && !float.IsInfinity(window) && untilImpact >= 0 && window > 0 && untilImpact <= Math.Min(.3f, window);
        }

        public static float Recovery(bool enraged) { return enraged ? 1.45f : 1.25f; }
    }
}
