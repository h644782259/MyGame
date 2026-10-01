using UnityEngine;

namespace Emberfall
{
    /// <summary>A pending cast owns one fixed aim and commits its budget only on release.</summary>
    public sealed class SkillChargeController : MonoBehaviour
    {
        public bool IsCharging { get { return SkillIndex >= 0; } }
        public int SkillIndex { get; private set; } = -1;
        public float Progress { get { return IsCharging && duration > 0 ? Mathf.Clamp01(elapsed / duration) : 0; } }
        public bool CancelledThisFrame { get { return cancelledFrame == Time.frameCount; } }
        public bool ConsumedThisFrame { get { return CancelledThisFrame || completedFrame == Time.frameCount; } }
        public Vector3 TargetPoint { get; private set; }
        internal Vector3 Direction { get; private set; }

        private PlayerController owner;
        private GameSession session;
        private int epoch, cancelledFrame = -1, completedFrame = -1;
        private float duration, elapsed;
        private AdvancedSkillVfx chargeEffect;

        public void Initialize(PlayerController hero, GameSession game) { owner = hero; session = game; }

        public static float Duration(HeroClass hero, int skill)
        {
            if (skill == 9)
            {
                if (hero == HeroClass.Vanguard) return .9f;
                if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner) return 1.1f;
                if (hero == HeroClass.Ranger) return .75f;
            }
            if (hero == HeroClass.Arcanist && skill == 1) return .55f;
            if (hero == HeroClass.Vanguard && skill == 7) return .5f;
            if (hero == HeroClass.Summoner && skill == 4) return .4f;
            return 0;
        }

        public bool Begin(int skill)
        {
            if (owner == null || session == null || IsCharging || ConsumedThisFrame || !owner.CanBeginSkillTargeting(skill)) return false;
            float chargeTime = Duration(owner.HeroClass, skill);
            if (chargeTime <= 0) return false;
            int rank = session.Progression.Profile.skillRanks[skill];
            Vector3 origin = owner.transform.position;
            TargetPoint = Vector3.ClampMagnitude(origin + Vector3.ClampMagnitude(CombatFx.Flat(owner.AimPoint - origin), 9f * GameBalance.SkillRangeMultiplier(rank)), session.ArenaRadius);
            Direction = owner.transform.forward;
            epoch = owner.CombatEpoch;
            duration = chargeTime;
            elapsed = 0;
            SkillIndex = skill;
            chargeEffect = AdvancedSkillVfx.Rune(owner, origin, 1.35f, GameBalance.ClassColor(owner.HeroClass), chargeTime + .2f, 2, true);
            if (chargeEffect != null) chargeEffect.transform.localScale = Vector3.one * .65f;
            return true;
        }

        public void Cancel()
        {
            if (IsCharging) cancelledFrame = Time.frameCount;
            SkillIndex = -1;
            elapsed = duration = 0;
            ClearEffect();
        }

        private void LateUpdate() { Advance(Time.deltaTime); }

        // Kept separate so runtime checks can exercise exact commit boundaries.
        private void Advance(float deltaTime)
        {
            if (!IsCharging) return;
            if (owner == null || owner.IsDead || session == null || session.Player != owner || !session.HasStarted || owner.CombatEpoch != epoch)
            { Cancel(); return; }
            if (session.InputBlocked || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            elapsed += deltaTime;
            if (chargeEffect != null) chargeEffect.transform.localScale = Vector3.one * Mathf.Lerp(.65f, 1.2f, Progress);
            if (elapsed < duration) return;
            int skill = SkillIndex;
            SkillIndex = -1;
            // Revalidate the learned rank, budget and cooldown at the actual release.
            // TargetPoint/Direction remain the original snapshot through this call.
            owner.ExecuteChargedSkill(skill);
            ClearEffect();
            completedFrame = Time.frameCount;
            elapsed = duration = 0;
        }

        private void OnDisable() { Cancel(); }

        private void ClearEffect()
        {
            if (chargeEffect == null) return;
            chargeEffect.gameObject.SetActive(false);
            Destroy(chargeEffect.gameObject);
            chargeEffect = null;
        }
    }
}
