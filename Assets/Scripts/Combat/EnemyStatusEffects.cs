using UnityEngine;

namespace Emberfall
{
    /// <summary>Distinct timed status mechanics; all timers obey combat pause.</summary>
    public sealed class EnemyStatusEffects : MonoBehaviour
    {
        public float MoveMultiplier { get { return slowTime > 0 ? 1 - slowStrength : 1; } }
        public float DamageMultiplier { get { return markTime > 0 ? 1 + markStrength : 1; } }
        public int PoisonStacks { get { return poisonTime > 0 ? poisonStacks : 0; } }
        public bool IsFrozen { get { return frozenTime > 0; } }
        public bool HasFrostMark { get { return frozenTime > 0 || frostMarkTime > 0; } }
        public bool IsMarked { get { return markTime > 0; } }
        public bool IsBurning { get { return burnTime > 0; } }
        public bool KnockedDown { get { return downTime > 0; } }
        public bool IsAirborne { get { return airborneTime > 0; } }
        public float AirborneHeight { get { return airborneTime <= 0 ? 0 : Mathf.Sin(Mathf.Clamp01(1f - airborneTime / airborneDuration) * Mathf.PI) * airborneHeight; } }
        public string Summary { get {
            string value = IsAirborne ? "浮空 " : downTime > 0 ? "击倒 " : frozenTime > 0 ? "冻结 " : enemy != null && enemy.IsStunned ? "眩晕 " : "";
            if (frostMarkTime > 0 && frozenTime <= 0) value += "霜痕 ";
            if (burnTime > 0) value += "灼烧 ";
            if (slowTime > 0) value += "减速 ";
            if (PoisonStacks > 0) value += "中毒×" + PoisonStacks + " ";
            if (markTime > 0) value += "锁定易伤 ";
            return value.Trim();
        } }
        private EnemyController enemy;
        private float slowTime, slowStrength, markTime, markStrength, poisonTime, poisonDamage, poisonTick, downTime, frozenTime;
        private float frostMarkTime, burnTime, burnDamage, burnTick;
        private int burnEpoch;
        private PlayerController burnSource;
        private readonly RecentCastGate meteorCasts = new RecentCastGate();
        private readonly RecentCastGate poisonCasts = new RecentCastGate();
        private PlayerController castOwner;
        private int castEpoch;
        private int poisonStacks, sourceEpoch;
        private PlayerController poisonSource;
        private Transform model;
        private bool wasDown;
        private float airborneTime, airborneDuration, airborneHeight, airborneRecovery;

        private void Awake() { enemy = GetComponent<EnemyController>(); }
        public void Slow(float duration, float strength)
        {
            slowTime = Mathf.Max(slowTime, duration);
            slowStrength = Mathf.Max(slowStrength, Mathf.Clamp(strength * (enemy.IsBoss ? .35f : 1), 0, .8f));
            enemy.Provoke();
        }
        public void Freeze(float duration)
        {
            if (enemy == null || enemy.IsDead || duration <= 0) return;
            frozenTime = Mathf.Max(frozenTime, duration * (enemy.IsBoss ? .24f : 1));
            // Boss resistance shortens hard control, never the combo opportunity.
            if (enemy.IsBoss) frostMarkTime = Mathf.Max(frostMarkTime, duration + 2f);
            enemy.ApplyControl(duration);
            Slow(duration + 2, .4f);
        }
        public void Knockdown(float duration)
        {
            downTime = Mathf.Max(downTime, duration * (enemy.IsBoss ? .24f : 1));
            enemy.ApplyControl(duration);
        }
        public void Knockup(float duration, float height)
        {
            if (enemy == null || enemy.IsDead || duration <= 0 || IsAirborne || airborneRecovery > 0) return;
            airborneDuration = Mathf.Clamp(duration, .3f, 1.2f) * (enemy.IsBoss ? .24f : 1f);
            airborneTime = airborneDuration;
            airborneHeight = Mathf.Clamp(height, .25f, 2f) * (enemy.IsBoss ? .18f : 1f);
            airborneRecovery = airborneDuration + (enemy.IsBoss ? 2.5f : .35f);
            enemy.ApplyControl(duration);
            enemy.Provoke();
        }
        public void Mark(float duration, float vulnerability)
        {
            markTime = Mathf.Max(markTime, duration);
            markStrength = Mathf.Max(markStrength, Mathf.Clamp(vulnerability, 0, .3f));
            enemy.Provoke();
        }
        public void Poison(PlayerController source, float duration, float damagePerTick)
        {
            if (source == null || enemy.IsDead) return;
            if (poisonTime <= 0) { poisonStacks = 0; poisonDamage = 0; poisonTick = .75f; }
            poisonTime = Mathf.Max(poisonTime, duration);
            poisonStacks = Mathf.Min(3, poisonStacks + 1);
            poisonDamage = Mathf.Max(poisonDamage, damagePerTick);
            poisonSource = source; sourceEpoch = source.CombatEpoch;
            enemy.Provoke();
            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Poison, duration);
        }
        private void PrepareCastOwner(PlayerController source)
        {
            if (castOwner == source && castEpoch == source.CombatEpoch) return;
            castOwner = source; castEpoch = source.CombatEpoch;
            meteorCasts.Clear(); poisonCasts.Clear();
        }

        public bool BeginMeteorImpact(PlayerController source, int castId)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead) return false;
            PrepareCastOwner(source);
            return meteorCasts.TryEnter(castId);
        }

        public bool ConsumeFrost()
        {
            if (!HasFrostMark) return false;
            frozenTime = frostMarkTime = 0;
            return true;
        }

        public bool ConsumePoison(PlayerController source, int castId, out float storedDamage)
        {
            storedDamage = 0;
            if (source == null || PoisonStacks < 3 || poisonSource != source || sourceEpoch != source.CombatEpoch) return false;
            PrepareCastOwner(source);
            if (!poisonCasts.TryEnter(castId)) return false;
            storedDamage = poisonDamage * poisonStacks * 3f;
            poisonTime = poisonDamage = 0; poisonStacks = 0;
            return true;
        }

        public void Burn(PlayerController source, float duration, float totalDamage)
        {
            if (source == null || source.IsDead || enemy == null || enemy.IsDead || duration <= 0 || totalDamage <= 0) return;
            burnTime = Mathf.Max(burnTime, duration);
            burnDamage = Mathf.Max(burnDamage, totalDamage / duration);
            if (burnSource == null || burnTick <= 0) burnTick = .5f;
            burnSource = source; burnEpoch = source.CombatEpoch;
            enemy.Provoke();
            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Fire, duration);
        }

        private void Update()
        {
            if (enemy == null || enemy.IsDead) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            slowTime = Mathf.Max(0, slowTime - dt);
            markTime = Mathf.Max(0, markTime - dt);
            downTime = Mathf.Max(0, downTime - dt);
            frozenTime = Mathf.Max(0, frozenTime - dt);
            frostMarkTime = Mathf.Max(0, frostMarkTime - dt);
            if (burnTime > 0)
            {
                if (burnSource == null || burnSource.IsDead || burnSource.CombatEpoch != burnEpoch || GameSession.Instance == null || GameSession.Instance.Player != burnSource)
                { burnTime = burnDamage = 0; burnSource = null; }
                else
                {
                    float elapsed = Mathf.Min(dt, burnTime);
                    burnTime = Mathf.Max(0, burnTime - dt);
                    burnTick -= elapsed;
                    int catchup = 0;
                    while (burnTick <= 0 && !enemy.IsDead && catchup++ < 8)
                    {
                        burnTick += .5f;
                        enemy.TakeDamage(burnDamage * .5f, Vector3.zero, impact: false);
                    }
                    if (burnTime <= 0) { burnDamage = 0; burnSource = null; }
                }
            }
            airborneTime = Mathf.Max(0, airborneTime - dt);
            airborneRecovery = Mathf.Max(0, airborneRecovery - dt);
            if (slowTime <= 0) slowStrength = 0;
            if (markTime <= 0) markStrength = 0;
            if (poisonTime <= 0) return;
            if (poisonSource == null || poisonSource.IsDead || poisonSource.CombatEpoch != sourceEpoch || GameSession.Instance == null || GameSession.Instance.Player != poisonSource)
            { poisonTime = 0; poisonDamage = 0; poisonStacks = 0; return; }
            poisonTime = Mathf.Max(0, poisonTime - dt);
            poisonTick -= dt;
            if (poisonTick <= 0)
            {
                poisonTick += .75f;
                enemy.TakeDamage(poisonDamage * poisonStacks, Vector3.zero, impact: false);
                CombatFx.Ring(enemy.transform.position, .6f, new Color(.55f, 1f, .28f), .35f, .06f);
            }
        }
        private void LateUpdate()
        {
            if (enemy == null || enemy.IsDead) return;
            if (model == null) { CombatModel found = GetComponentInChildren<CombatModel>(); if (found != null) model = found.transform; }
            if (model == null) return;
            if (downTime > 0 && !enemy.IsBoss) { model.localRotation = Quaternion.Euler(0, 0, 72); wasDown = true; }
            else if (wasDown) { model.localRotation = Quaternion.identity; wasDown = false; }
        }
    }
}
