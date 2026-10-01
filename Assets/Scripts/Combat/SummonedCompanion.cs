using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed class SummonedCompanion : MonoBehaviour
    {
        public enum Kind { Wolf, Spirit, Treant }
        private static readonly List<SummonedCompanion> active = new List<SummonedCompanion>();
        public PlayerController Owner { get; private set; }
        public Kind Form { get; private set; }
        public bool IsStarter { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float RemainingLifetime { get; private set; }
        public bool IsAlive { get { return Health > 0 && Owner != null && !Owner.IsDead && Owner.gameObject.activeInHierarchy && gameObject.activeInHierarchy && session != null && session.HasStarted && session.Player == Owner && Owner.CombatEpoch == epoch; } }
        private GameSession session;
        private int epoch, rank;
        private float damage, cooldown, attackPose, baseMaxHealth;
        private CombatModel model;
        private EnemyController target;
        private Transform healthBar;
        private Material healthMaterial;
        private readonly WorldTraversal.Route route = new WorldTraversal.Route();
        private float NavigationRadius => RadiusFor(Form);

        private static float RadiusFor(Kind form) => form == Kind.Treant ? .7f : form == Kind.Wolf ? .4f : .35f;

        public static int Count(PlayerController owner, bool treants = false)
        {
            int count = 0;
            foreach (var pet in active) if (pet != null && pet.IsAlive && pet.Owner == owner && (pet.Form == Kind.Treant) == treants) count++;
            return count;
        }

        public static SummonedCompanion Summon(PlayerController owner, GameSession game, Kind form, int rank, Vector3 at, float strength)
        {
            if (form == Kind.Wolf && rank > 0) DismissStarters(owner);
            int cap = form == Kind.Treant ? 1 : owner.HasMechanic(EquipmentMechanic.TwinSummonResonance) ? 2 : 4;
            while (Count(owner, form == Kind.Treant) >= cap)
            {
                var oldest = active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && (pet.Form == Kind.Treant) == (form == Kind.Treant));
                if (oldest == null) break;
                oldest.Dismiss();
            }
            var obj = new GameObject(form == Kind.Wolf ? "灵狼" : form == Kind.Spirit ? "星灵" : "远古树灵");
            obj.transform.position = WorldTraversal.NearestWalkable(at, RadiusFor(form));
            var companion = obj.AddComponent<SummonedCompanion>();
            companion.Owner = owner; companion.session = game; companion.Form = form;
            companion.rank = rank; companion.epoch = owner.CombatEpoch; companion.damage = strength;
            companion.IsStarter = rank == 0;
            companion.baseMaxHealth = game.Progression.GetStats().MaxHealth * (companion.IsStarter ? .25f : form == Kind.Treant ? 1.2f : .42f + rank * .08f);
            companion.MaxHealth = companion.baseMaxHealth * (owner.HasMechanic(EquipmentMechanic.TwinSummonResonance) ? .85f : 1f);
            companion.Health = companion.MaxHealth;
            companion.RemainingLifetime = companion.IsStarter ? float.PositiveInfinity : form == Kind.Treant ? 10 + rank * 2 : 15 + rank * 3;
            companion.model = CombatModel.Companion(obj.transform, form);
            companion.BuildHealthBar();
            active.Add(companion);
            AdvancedSkillVfx.Rune(owner, obj.transform.position, form == Kind.Treant ? 2.6f : 1.3f, GameBalance.ClassColor(HeroClass.Summoner), .7f, rank);
            return companion;
        }

        public static bool HasStarter(PlayerController owner)
        {
            return active.Exists(pet => pet != null && pet.IsAlive && pet.Owner == owner && pet.IsStarter);
        }

        public static SummonedCompanion SummonStarter(PlayerController owner, GameSession game, float strength)
        {
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner || game.Progression.Profile.skillRanks[2] > 0 || HasStarter(owner)) return null;
            SummonedCompanion companion = Summon(owner, game, Kind.Wolf, 0,
                owner.transform.position - owner.transform.forward * 1.2f, strength * .45f);
            companion.gameObject.name = "幼年灵狼 · 基础伙伴";
            return companion;
        }

        public static void DismissStarters(PlayerController owner)
        {
            foreach (SummonedCompanion pet in active.ToArray())
                if (pet != null && pet.Owner == owner && pet.IsStarter) pet.Dismiss();
        }

        public static void EnforceCapacity(PlayerController owner)
        {
            int cap = owner.HasMechanic(EquipmentMechanic.TwinSummonResonance) ? 2 : 4;
            while (Count(owner) > cap)
            {
                SummonedCompanion oldest = active.Find(pet => pet != null && pet.Owner == owner && pet.IsAlive && pet.Form != Kind.Treant);
                if (oldest == null) break;
                oldest.Dismiss();
            }
        }

        public static void HealAll(PlayerController owner, float fraction)
        {
            foreach (var pet in active)
                if (pet != null && pet.IsAlive && pet.Owner == owner) pet.Health = Mathf.Min(pet.MaxHealth, pet.Health + pet.MaxHealth * fraction);
        }

        public static SummonedCompanion ThreatTarget(EnemyController enemy, Vector3 playerPosition)
        {
            SummonedCompanion selected = null;
            float nearest = Vector3.Distance(enemy.transform.position, playerPosition);
            foreach (var pet in active)
            {
                if (pet == null || !pet.IsAlive || pet.session != GameSession.Instance) continue;
                float distance = Vector3.Distance(pet.transform.position, enemy.transform.position);
                if (distance < 6f && (distance < nearest || pet.Form == Kind.Treant))
                { selected = pet; nearest = distance; if (pet.Form == Kind.Treant) break; }
            }
            return selected;
        }

        public static bool HitHostileProjectile(Vector3 previous, Vector3 current, float damage, float maximumFraction = 1f)
        {
            SummonedCompanion first = null;
            Vector3 segment = CombatFx.Flat(current - previous);
            float earliest = maximumFraction;
            foreach (var pet in active)
            {
                if (pet != null && pet.IsAlive && CombatFx.SegmentDistance(pet.transform.position, previous, current) < (pet.Form == Kind.Treant ? .9f : .48f)
                    && WorldTraversal.HasLineOfSight(previous, pet.transform.position))
                {
                    float fraction = segment.sqrMagnitude < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(CombatFx.Flat(pet.transform.position - previous), segment) / segment.sqrMagnitude);
                    if (fraction <= earliest) { earliest = fraction; first = pet; }
                }
            }
            if (first == null) return false;
            first.TakeDamage(damage); return true;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health <= 0 && IsStarter && Owner != null) Owner.OnStarterCompanionDefeated();
            model.Recoil(-transform.forward, .7f);
            HitFeedback.Spawn(transform.position + Vector3.up, -transform.forward, .6f);
            if (Health <= 0) Dismiss();
        }

        private void BuildHealthBar()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "Companion health";
            Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * (Form == Kind.Treant ? 3.3f : 1.7f);
            healthBar = obj.transform;
            healthMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(.32f, 1f, .75f) };
            obj.GetComponent<Renderer>().sharedMaterial = healthMaterial;
        }

        private EnemyController AcquireTarget()
        {
            EnemyController focused = Owner.FocusTarget;
            if (focused != null) return focused;
            EnemyController chosen = null;
            float nearest = 14f;
            foreach (var enemy in session.Enemies)
            {
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy || (!enemy.IsAggro && !session.InDungeon)) continue;
                float distance = CombatFx.Flat(enemy.transform.position - Owner.transform.position).magnitude;
                if (distance < nearest) { chosen = enemy; nearest = distance; }
            }
            return chosen;
        }

        private void Update()
        {
            if (Owner == null || Owner.IsDead || session == null || session.Player != Owner || !session.HasStarted || Owner.CombatEpoch != epoch) { Dismiss(); return; }
            if (session.InputBlocked) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            float healthMaximum = baseMaxHealth * (Owner.HasMechanic(EquipmentMechanic.TwinSummonResonance) ? .85f : 1f);
            if (Mathf.Abs(healthMaximum - MaxHealth) > .01f)
            {
                Health = healthMaximum * Mathf.Clamp01(Health / Mathf.Max(1f, MaxHealth));
                MaxHealth = healthMaximum;
            }
            RemainingLifetime -= dt;
            if (RemainingLifetime <= 0) { Dismiss(); return; }
            cooldown -= dt;
            attackPose = Mathf.Max(0, attackPose - dt * 3);
            target = AcquireTarget();
            Vector3 followAnchor = WorldTraversal.NearestWalkable(Owner.transform.position - Owner.transform.forward * 1.6f + Owner.transform.right * (Form == Kind.Wolf ? -1.7f : 1.7f), NavigationRadius);
            if (!WorldTraversal.HasGroundPath(Owner.transform.position, followAnchor, .12f))
                followAnchor = WorldTraversal.NearestWalkable(Owner.transform.position, NavigationRadius);
            if (Vector3.Distance(transform.position, Owner.transform.position) > 23
                && WorldTraversal.HasGroundPath(transform.position, Owner.transform.position, NavigationRadius)
                && WorldTraversal.HasGroundPath(transform.position, followAnchor, NavigationRadius)
                && WorldTraversal.HasLineOfSight(transform.position, followAnchor))
            {
                CombatFx.Ring(transform.position, .7f, GameBalance.ClassColor(HeroClass.Summoner), .25f, .15f);
                transform.position = followAnchor;
                CombatFx.Ring(transform.position, .7f, GameBalance.ClassColor(HeroClass.Summoner), .25f, .15f);
            }
            Vector3 destination = target == null ? followAnchor : target.transform.position;
            Vector3 delta = CombatFx.Flat(destination - transform.position);
            float attackRange = Form == Kind.Spirit ? 7.5f : Form == Kind.Treant ? 2.8f : 1.4f;
            bool attackPath = target == null || CanReachTarget(target.transform.position);
            bool moving = delta.magnitude > (target == null ? .5f : attackRange * .85f) || !attackPath;
            Vector3 heading = moving ? route.Direction(transform.position, destination, NavigationRadius) : delta.normalized;
            if (heading.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), 1 - Mathf.Exp(-10 * dt));
            Vector3 previous = transform.position;
            transform.position = WorldTraversal.Move(transform.position, moving ? heading * Mathf.Min(delta.magnitude, dt * (Form == Kind.Treant ? 4.2f : 7f)) : Vector3.zero, NavigationRadius);
            moving = (transform.position - previous).sqrMagnitude > .000001f;
            model.Animate(moving ? 1f : 0, attackPose, false);
            delta = CombatFx.Flat(destination - transform.position);
            if (target != null && delta.magnitude <= attackRange && cooldown <= 0 && CanReachTarget(target.transform.position))
            {
                attackPose = 1;
                float attackDamage = damage * (Owner.HasMechanic(EquipmentMechanic.TwinSummonResonance) ? 1.35f : 1f);
                cooldown = Form == Kind.Treant ? 2.2f : Form == Kind.Spirit ? 1.2f : .85f;
                if (Form == Kind.Spirit)
                    CombatProjectile.Friendly(Owner, session, transform.position, delta.normalized, attackDamage * .72f, GameBalance.ClassColor(HeroClass.Summoner), tracking: target);
                else if (Form == Kind.Treant)
                {
                    CombatFx.Ring(transform.position, 3.3f * GameBalance.SkillRangeMultiplier(rank), new Color(.48f, 1f, .63f), .4f, .2f);
                    foreach (var enemy in session.Enemies.ToArray())
                        if (enemy != null && !enemy.IsDead && CombatFx.Flat(enemy.transform.position - transform.position).magnitude < 3.3f * GameBalance.SkillRangeMultiplier(rank)
                            && WorldTraversal.HasGroundPath(transform.position, enemy.transform.position, .12f))
                        {
                            enemy.TakeDamage(attackDamage * 1.45f, enemy.transform.position - transform.position, .55f, .3f);
                            if (!enemy.IsDead) enemy.StatusEffects.Knockdown(.45f + rank * .12f);
                        }
                }
                else
                {
                    CombatFx.Slash(transform.position, delta.normalized, 1.25f, new Color(.55f, 1f, .87f));
                    target.TakeDamage(attackDamage * .65f, delta.normalized, .13f, .05f);
                }
                Owner.OnCompanionAttack(target, transform.position, damage);
            }
        }

        private bool CanReachTarget(Vector3 position)
        {
            return Form == Kind.Spirit
                ? WorldTraversal.HasLineOfSight(transform.position, position)
                : WorldTraversal.HasGroundPath(transform.position, position, .12f);
        }

        private void LateUpdate()
        {
            if (healthBar == null) return;
            healthBar.localScale = new Vector3(.9f * Mathf.Clamp01(Health / MaxHealth), .05f, .025f);
            if (Camera.main != null) healthBar.rotation = Camera.main.transform.rotation;
        }

        public void Dismiss()
        {
            active.Remove(this);
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        private void OnDestroy() { active.Remove(this); if (healthMaterial != null) Destroy(healthMaterial); }
    }
}
