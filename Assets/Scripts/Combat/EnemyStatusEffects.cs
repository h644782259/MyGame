using UnityEngine;

namespace Emberfall
{
    /// <summary>Distinct timed status mechanics; all timers obey combat pause.</summary>
    public sealed class EnemyStatusEffects : MonoBehaviour
    {
        public float MoveMultiplier { get { return slowTime > 0 ? 1 - slowStrength : 1; } }
        public float DamageMultiplier { get { return markTime > 0 ? 1 + markStrength : 1; } }
        public int PoisonStacks { get { return poisonTime > 0 ? poisonStacks : 0; } }
        public bool KnockedDown { get { return downTime > 0; } }
        public string Summary { get {
            string value = downTime > 0 ? "击倒 " : frozenTime > 0 ? "冻结 " : enemy != null && enemy.IsStunned ? "眩晕 " : "";
            if (slowTime > 0) value += "减速 ";
            if (PoisonStacks > 0) value += "中毒×" + PoisonStacks + " ";
            if (markTime > 0) value += "锁定易伤 ";
            return value.Trim();
        } }
        private EnemyController enemy;
        private float slowTime, slowStrength, markTime, markStrength, poisonTime, poisonDamage, poisonTick, downTime, frozenTime;
        private int poisonStacks, sourceEpoch;
        private PlayerController poisonSource;
        private Transform model;
        private bool wasDown;

        private void Awake() { enemy = GetComponent<EnemyController>(); }
        public void Slow(float duration, float strength)
        {
            slowTime = Mathf.Max(slowTime, duration);
            slowStrength = Mathf.Max(slowStrength, Mathf.Clamp(strength * (enemy.IsBoss ? .35f : 1), 0, .8f));
            enemy.Provoke();
        }
        public void Freeze(float duration)
        {
            frozenTime = Mathf.Max(frozenTime, duration * (enemy.IsBoss ? .24f : 1));
            enemy.ApplyControl(duration);
            Slow(duration + 2, .4f);
        }
        public void Knockdown(float duration)
        {
            downTime = Mathf.Max(downTime, duration * (enemy.IsBoss ? .24f : 1));
            enemy.ApplyControl(duration);
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
            if (slowTime <= 0) slowStrength = 0;
            if (markTime <= 0) markStrength = 0;
            if (poisonTime <= 0) return;
            if (poisonSource == null || poisonSource.CombatEpoch != sourceEpoch || GameSession.Instance == null || GameSession.Instance.Player != poisonSource)
            { poisonTime = 0; poisonDamage = 0; poisonStacks = 0; return; }
            poisonTime = Mathf.Max(0, poisonTime - dt);
            poisonTick -= dt;
            if (poisonTick <= 0)
            {
                poisonTick += .75f;
                enemy.TakeDamage(poisonDamage * poisonStacks, Vector3.zero);
                CombatFx.Ring(enemy.transform.position, .6f, new Color(.55f, 1f, .28f), .35f, .06f);
            }
        }
        private void LateUpdate()
        {
            if (model == null) { CombatModel found = GetComponentInChildren<CombatModel>(); if (found != null) model = found.transform; }
            if (model == null) return;
            if (downTime > 0 && !enemy.IsBoss) { model.localRotation = Quaternion.Euler(0, 0, 72); wasDown = true; }
            else if (wasDown) { model.localRotation = Quaternion.identity; wasDown = false; }
        }
    }
}
