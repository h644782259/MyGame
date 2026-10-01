using UnityEngine;

namespace Emberfall
{
    internal sealed class SummonerSpell : MonoBehaviour
    {
        private PlayerController owner;
        private GameSession session;
        private int rank, epoch;
        private float damage, age, nextTick;
        private float Radius { get { return 4.4f * GameBalance.SkillRangeMultiplier(rank); } }
        public static void Cast(PlayerController player, GameSession game, int skill, int rank, Vector3 target, float damage)
        {
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass.Summoner);
            if (skill == 2 || skill == 4 || skill == 9)
            {
                var form = skill == 2 ? SummonedCompanion.Kind.Wolf : skill == 4 ? SummonedCompanion.Kind.Spirit : SummonedCompanion.Kind.Treant;
                Vector3 position = skill == 9 ? target : player.transform.position + player.transform.forward * 2 + player.transform.right * (skill == 2 ? -1 : 1);
                SummonedCompanion.Summon(player, game, form, rank, position, damage);
            }
            else if (skill == 0)
            {
                CombatFx.Slash(player.transform.position, player.transform.forward, 5f * range, color);
                foreach (var enemy in game.Enemies.ToArray())
                {
                    if (enemy == null || enemy.IsDead) continue;
                    Vector3 delta = CombatFx.Flat(enemy.transform.position - player.transform.position);
                    if (delta.magnitude <= 5f * range && (delta.sqrMagnitude < .1f || Vector3.Angle(player.transform.forward, delta) < 55))
                        enemy.TakeDamage(damage * 1.8f, delta.normalized, 1.25f + rank * .2f, .2f);
                }
            }
            else if (skill == 1)
                CombatArea.Spawn(player, game, target, 3.2f * range, damage * .55f, 0, .25f, 3 + rank, .7f, color, statusSkill: 1, statusRank: rank);
            else if (skill == 6)
                AdvancedSkillSequence.Spawn(player, game, skill, rank, target, player.transform.forward, damage, color);
            else if (skill == 7)
            {
                var obj = new GameObject("引力印记"); obj.transform.position = target;
                var spell = obj.AddComponent<SummonerSpell>();
                spell.owner = player; spell.session = game; spell.rank = rank; spell.damage = damage; spell.epoch = player.CombatEpoch;
                AdvancedSkillVfx.Rune(player, target, 4.4f * range, color, 3.3f, rank + 1);
            }
        }
        private void Update()
        {
            if (owner == null || owner.IsDead || session == null || session.Player != owner || owner.CombatEpoch != epoch || !session.HasStarted) { Destroy(gameObject); return; }
            if (session.InputBlocked) return;
            age += Time.deltaTime;
            foreach (var enemy in session.Enemies.ToArray())
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(transform.position - enemy.transform.position);
                if (delta.magnitude > Radius) continue;
                enemy.Provoke();
                if (delta.magnitude > .6f)
                    enemy.transform.position = WorldTraversal.Move(enemy.transform.position,
                        delta.normalized * Mathf.Min(delta.magnitude - .6f, Time.deltaTime * (3.5f + rank) * (enemy.IsBoss ? .25f : 1f)), enemy.NavigationRadius);
                if (age >= 2.6f)
                {
                    enemy.TakeDamage(damage * 3.2f, -delta.normalized, .3f, .25f);
                    if (!enemy.IsDead) enemy.StatusEffects.Knockup(.7f + rank * .1f, 1.2f + rank * .2f);
                }
                else if (age >= nextTick) enemy.TakeDamage(damage * .45f, Vector3.zero, impact: false);
            }
            if (age >= nextTick) nextTick = age + .5f;
            if (age >= 2.6f) { CombatFx.Ring(transform.position, Radius, GameBalance.ClassColor(HeroClass.Summoner), .5f, .22f); Destroy(gameObject); }
        }
    }
}
