using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    internal static class CombatFx
    {
        public static GameObject Ring(Vector3 center, float radius, Color color, float lifetime, float width = .1f, bool expand = true)
        {
            GameObject obj = new GameObject("Combat Ring");
            obj.transform.position = center + Vector3.up * .065f;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
            }
            line.widthMultiplier = width;
            line.sharedMaterial = NewGlow();
            line.startColor = line.endColor = color;
            obj.AddComponent<FadingCombatEffect>().Setup(line, color, radius, lifetime, expand);
            return obj;
        }

        public static void Slash(Vector3 center, Vector3 forward, float radius, Color color)
        {
            GameObject obj = new GameObject("Sword Arc");
            obj.transform.position = center + Vector3.up * .83f;
            obj.transform.rotation = Quaternion.LookRotation(forward);
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float angle = Mathf.Lerp(-75, 75, i / 23f) * Mathf.Deg2Rad;
                line.SetPosition(i, new Vector3(Mathf.Sin(angle), -.1f * Mathf.Cos(angle), Mathf.Cos(angle)));
            }
            line.widthMultiplier = .16f;
            line.sharedMaterial = NewGlow();
            line.startColor = line.endColor = color;
            obj.AddComponent<FadingCombatEffect>().Setup(line, color, radius, .23f, true);
        }

        public static Material NewGlow() { return new Material(Shader.Find("Sprites/Default")); }

        public static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
        {
            point.y = a.y = b.y = 0;
            Vector3 direction = b - a;
            float fraction = direction.sqrMagnitude < .0001f ? 0 : Mathf.Clamp01(Vector3.Dot(point - a, direction) / direction.sqrMagnitude);
            return Vector3.Distance(point, a + direction * fraction);
        }

        public static Vector3 Flat(Vector3 vector) { vector.y = 0; return vector; }
    }

    internal sealed class FadingCombatEffect : MonoBehaviour
    {
        private LineRenderer line;
        private Color color;
        private float radius, lifetime, age;
        private bool expand;
        public void Setup(LineRenderer renderer, Color value, float size, float duration, bool growing)
        {
            line = renderer; color = value; radius = size; lifetime = Mathf.Max(.05f, duration); expand = growing;
            transform.localScale = Vector3.one * (expand ? radius * .4f : radius);
        }
        private void Update()
        {
            age += Time.deltaTime;
            float fraction = age / lifetime;
            if (fraction >= 1) { Destroy(gameObject); return; }
            if (expand) transform.localScale = Vector3.one * radius * Mathf.Lerp(.4f, 1f, Mathf.Min(1, fraction * 2.5f));
            Color faded = color;
            faded.a *= Mathf.Clamp01((1f - fraction) * 2f);
            line.startColor = line.endColor = faded;
        }
        private void OnDestroy() { if (line != null && line.sharedMaterial != null) Destroy(line.sharedMaterial); }
    }

    internal sealed class CombatProjectile : MonoBehaviour
    {
        private static readonly List<CombatProjectile> hostileProjectiles = new List<CombatProjectile>();
        private Vector3 dodgeOrigin;
        private float dodgeDeadline;
        private int dodgeEpoch;
        private bool pendingDodge;
        private int skillIndex = -1, castId;
        private string damageSource = "敌方弹幕";
        private GameSession session;
        private PlayerController owner;
        private PlayerController playerGeneration;
        private Vector3 direction;
        private float speed, radius, age, lifetime, explosionRadius;
        private CombatDamage damage, explosionDamage;
        private bool hostile, pierce, basicAttack, energyAwarded, arrowShape;
        private EnemyController homingTarget;
        private EnemyController basicAimTarget;
        private bool bodyHeightFlight;
        private float launchHeight, impactHeight, aimedDistance, distanceTravelled;
        private int epoch;
        private readonly HashSet<EnemyController> hitTargets = new HashSet<EnemyController>();
        private Material bodyMaterial, trailMaterial;
        private Color color;

        public static void Friendly(PlayerController player, GameSession game, Vector3 at, Vector3 forward, CombatDamage amount, Color tint, bool piercing = false, bool arrow = false, bool basic = false, float size = 1f, float velocity = 0f, EnemyController tracking = null, CombatDamage blastDamage = default(CombatDamage), float blastRadius = 0f, int skillIndex = -1, int castId = 0)
        {
            CombatProjectile projectile = Make(at, forward, tint, arrow);
            projectile.owner = player;
            projectile.playerGeneration = player;
            projectile.session = game;
            projectile.epoch = player.CombatEpoch;
            projectile.damage = amount;
            projectile.speed = arrow ? 20f : 16f;
            projectile.lifetime = 1.15f;
            projectile.radius = piercing ? .38f : .22f;
            projectile.pierce = piercing;
            projectile.basicAttack = basic;
            projectile.homingTarget = tracking;
            projectile.arrowShape = arrow;
            projectile.explosionDamage = blastDamage;
            projectile.explosionRadius = blastRadius;
            projectile.skillIndex = skillIndex; projectile.castId = castId;
            projectile.transform.localScale *= size;
            projectile.radius *= Mathf.Min(2f,size);
            if (velocity > 0) projectile.speed = velocity;
            if (tracking != null) projectile.lifetime = 2.5f;
        }

        // Ordinary shots keep their original range. A mage locks only the target
        // chosen at cast time; an arrow gets a very short, bounded correction.
        public static void BasicShot(PlayerController player,GameSession game,Vector3 muzzle,Vector3 target,CombatDamage amount,Color tint,bool arrow,EnemyController selected)
        {
            if (!WorldTraversal.HasLineOfSight(player.transform.position, muzzle))
            { CombatFx.Ring(player.transform.position, .4f, tint, .15f); return; }
            Vector3 direction=CombatFx.Flat(target-muzzle);
            if(direction.sqrMagnitude<.0001f) direction=player.transform.forward;
            CombatProjectile projectile=Make(muzzle,direction,tint,arrow);
            projectile.owner=player;
            projectile.playerGeneration=player;
            projectile.session=game;
            projectile.epoch=player.CombatEpoch;
            projectile.damage=amount;
            projectile.speed=arrow?20f:16f;
            projectile.lifetime=1.15f;
            projectile.radius=.22f;
            projectile.basicAttack=true;
            projectile.arrowShape=arrow;
            projectile.basicAimTarget=selected;
            projectile.homingTarget=arrow?null:selected;
            projectile.bodyHeightFlight=true;
            projectile.launchHeight=muzzle.y;
            projectile.impactHeight=target.y;
            projectile.aimedDistance=Mathf.Max(.25f,CombatFx.Flat(target-muzzle).magnitude);
            projectile.transform.position=muzzle;
            projectile.AlignBodyFlight();
        }

        public static void Hostile(GameSession game, Vector3 at, Vector3 forward, float amount, float velocity = 8f, string sourceName = "敌方弹幕")
        {
            CombatProjectile projectile = Make(at, forward, new Color(1f,.31f,.48f), false);
            projectile.session = game;
            projectile.playerGeneration = game.Player;
            projectile.epoch = game.Player.CombatEpoch;
            projectile.hostile = true;
            projectile.damageSource = sourceName;
            projectile.damage = amount;
            projectile.speed = velocity;
            projectile.lifetime = 3f;
            projectile.radius = .32f;
            hostileProjectiles.Add(projectile);
        }

        internal static void RegisterDodge(PlayerController player, Vector3 origin, Vector3 destination)
        {
            foreach (CombatProjectile projectile in hostileProjectiles)
            {
                if (projectile == null || projectile.playerGeneration != player || projectile.epoch != player.CombatEpoch || projectile.speed <= 0) continue;
                Vector3 end = projectile.transform.position + projectile.direction * projectile.speed * .18f;
                float hitRadius = projectile.radius + .46f;
                if (CombatFx.SegmentDistance(origin, projectile.transform.position, end) >= hitRadius ||
                    CombatFx.SegmentDistance(destination, projectile.transform.position, end) <= hitRadius ||
                    !WorldTraversal.HasLineOfSight(projectile.transform.position, origin)) continue;
                projectile.pendingDodge = true;
                projectile.dodgeOrigin = origin;
                projectile.dodgeDeadline = projectile.age + .25f;
                projectile.dodgeEpoch = player.CombatEpoch;
            }
        }

        private static CombatProjectile Make(Vector3 at, Vector3 forward, Color tint, bool arrow)
        {
            GameObject obj = GameObject.CreatePrimitive(arrow ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            obj.name = arrow ? "Spectral Arrow" : "Arcane Bolt";
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            obj.transform.position = new Vector3(at.x, 1f, at.z);
            Vector3 normalized = CombatFx.Flat(forward).normalized;
            if (normalized.sqrMagnitude < .1f) normalized = Vector3.forward;
            obj.transform.rotation = Quaternion.LookRotation(normalized) * (arrow ? Quaternion.Euler(90,0,0) : Quaternion.identity);
            obj.transform.localScale = arrow ? new Vector3(.1f,.43f,.1f) : Vector3.one * .27f;
            CombatProjectile projectile = obj.AddComponent<CombatProjectile>();
            projectile.direction = normalized;
            projectile.color = tint;
            projectile.bodyMaterial = new Material(Shader.Find("Unlit/Color"));
            projectile.bodyMaterial.color = tint;
            obj.GetComponent<Renderer>().sharedMaterial = projectile.bodyMaterial;
            TrailRenderer trail = obj.AddComponent<TrailRenderer>();
            projectile.trailMaterial = CombatFx.NewGlow();
            trail.sharedMaterial = projectile.trailMaterial;
            trail.time = .13f;
            trail.startWidth = arrow ? .11f : .22f;
            trail.endWidth = 0;
            trail.startColor = tint;
            trail.endColor = new Color(tint.r,tint.g,tint.b,0);
            trail.minVertexDistance = .12f;
            return projectile;
        }

        private void Update()
        {
            if (session == null || session.Player == null || session.Player != playerGeneration || !session.HasStarted || session.IsDead || session.Player.CombatEpoch != epoch || (!hostile && owner == null))
            { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            age += dt;
            if (age > lifetime) { Destroy(gameObject); return; }
            if (homingTarget != null && !homingTarget.IsDead && homingTarget.gameObject.activeInHierarchy)
            {
                Vector3 towards = CombatFx.Flat(homingTarget.transform.position-transform.position).normalized;
                if (towards.sqrMagnitude > .01f) direction = Vector3.Slerp(direction,towards,Mathf.Min(1,dt*8f)).normalized;
                transform.rotation = Quaternion.LookRotation(direction) * (arrowShape ? Quaternion.Euler(90,0,0) : Quaternion.identity);
            }
            else if(basicAttack && arrowShape && age<=.18f && basicAimTarget!=null && !basicAimTarget.IsDead && basicAimTarget.gameObject.activeInHierarchy)
            {
                Vector3 towards=CombatFx.Flat(basicAimTarget.transform.position-transform.position).normalized;
                if(towards.sqrMagnitude>.01f && Vector3.Angle(direction,towards)<=18f)
                    direction=Vector3.RotateTowards(direction,towards,70f*Mathf.Deg2Rad*dt,0).normalized;
            }
            Vector3 previous = transform.position;
            Vector3 nextPosition = previous + direction * speed * dt;
            bool terrainHit = !WorldTraversal.HasLineOfSight(previous, nextPosition);
            if (terrainHit)
            {
                float clear = 0, blocked = 1;
                for (int sample = 0; sample < 10; sample++)
                {
                    float fraction = (clear + blocked) * .5f;
                    if (WorldTraversal.HasLineOfSight(previous, Vector3.Lerp(previous, nextPosition, fraction))) clear = fraction;
                    else blocked = fraction;
                }
                nextPosition = Vector3.Lerp(previous, nextPosition, clear);
            }
            transform.position = nextPosition;
            if(bodyHeightFlight)
            {
                if(basicAimTarget!=null && !basicAimTarget.IsDead && basicAimTarget.gameObject.activeInHierarchy) impactHeight=owner.EnemyBodyPoint(basicAimTarget).y;
                distanceTravelled += CombatFx.Flat(nextPosition - previous).magnitude;
                Vector3 point=transform.position;
                point.y=Mathf.Lerp(launchHeight,impactHeight,Mathf.Clamp01(distanceTravelled/aimedDistance));
                transform.position=point;
                AlignBodyFlight();
            }
            if (hostile)
            {
                bool strikesPlayer = CombatFx.SegmentDistance(session.Player.transform.position, previous, transform.position) < radius + .46f && WorldTraversal.HasLineOfSight(previous, session.Player.transform.position);
                Vector3 segment = CombatFx.Flat(transform.position - previous);
                float playerFraction = !strikesPlayer ? 1f : segment.sqrMagnitude < .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(CombatFx.Flat(session.Player.transform.position - previous), segment) / segment.sqrMagnitude);
                if (SummonedCompanion.HitHostileProjectile(previous, transform.position, damage.Amount, playerFraction))
                { CombatFx.Ring(transform.position, .65f, color, .18f); Destroy(gameObject); return; }
                if (pendingDodge)
                {
                    if (age > dodgeDeadline || session.Player.CombatEpoch != dodgeEpoch) pendingDodge = false;
                    else if (!strikesPlayer && CombatFx.SegmentDistance(dodgeOrigin, previous, transform.position) < radius + .46f &&
                        WorldTraversal.HasLineOfSight(previous, dodgeOrigin))
                    { pendingDodge = false; session.Player.NotifyPerfectDodge(); }
                }
                if (strikesPlayer)
                {
                    session.Player.TakeDamageFrom(damage.Amount, damageSource);
                    CombatFx.Ring(transform.position, .65f, color, .18f);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                for (int i = session.Enemies.Count - 1; i >= 0; i--)
                {
                    EnemyController enemy = session.Enemies[i];
                    if (enemy == null || enemy.IsDead || hitTargets.Contains(enemy)) continue;
                    float hitRadius = enemy.IsBoss ? 1.05f : .6f;
                    if (CombatFx.SegmentDistance(enemy.transform.position, previous, transform.position) > hitRadius + radius) continue;
                    if (!WorldTraversal.HasLineOfSight(previous, enemy.transform.position)) continue;
                    hitTargets.Add(enemy);
                    Vector3 hitPosition = enemy.transform.position;
                    enemy.TakeDamage(owner.ResolveSkillImpact(enemy, skillIndex, castId, damage.Amount), direction, .18f, critical:damage.IsCritical);
                    CombatFx.Ring(hitPosition, .7f, color, .2f);
                    if (basicAttack && !energyAwarded)
                    {
                        energyAwarded = true;
                        owner.OnBasicAttackHitTarget(hitPosition, enemy, true);
                    }
                    if(explosionDamage.Amount>0)
                    {
                        owner.HitArea(hitPosition,explosionRadius,explosionDamage,.15f,.15f);
                        CombatFx.Ring(hitPosition,explosionRadius,color,.3f,.11f);
                    }
                    if (!pierce) { Destroy(gameObject); return; }
                    i=Mathf.Min(i,session.Enemies.Count);
                }
            }
            if (terrainHit) { CombatFx.Ring(transform.position, .4f, color, .15f); Destroy(gameObject); return; }
            float bound = session.ArenaRadius + 3f;
            if (Mathf.Abs(transform.position.x) > bound || Mathf.Abs(transform.position.z) > bound) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            hostileProjectiles.Remove(this);
            if (bodyMaterial != null) Destroy(bodyMaterial);
            if (trailMaterial != null) Destroy(trailMaterial);
        }

        private void AlignBodyFlight()
        {
            Vector3 visibleDirection=direction;
            if(distanceTravelled<aimedDistance) visibleDirection.y=(impactHeight-launchHeight)/aimedDistance;
            transform.rotation=Quaternion.LookRotation(visibleDirection.normalized)*(arrowShape?Quaternion.Euler(90,0,0):Quaternion.identity);
        }
    }

    internal sealed class CombatArea : MonoBehaviour
    {
        private PlayerController owner;
        private GameSession session;
        private float radius, stun, delay, duration, interval, age, nextTick, pullStrength;
        private CombatDamage damage, finalDamage;
        private bool follow, meteor, finished;
        private int statusSkill = -1, statusRank = 1, castId;
        private int epoch;
        private Color color;
        private GameObject marker, fallingOrb;
        private Material orbMaterial;
        private bool fireVisual, poisonVisual, lightningVisual;

        public static void Spawn(PlayerController player, GameSession game, Vector3 at, float size, CombatDamage amount, float disable,
            float startup, float activeTime, float tickInterval, Color tint, bool followPlayer = false, bool fallingMeteor = false, float pulling = 0f, CombatDamage finisher = default(CombatDamage), int statusSkill = -1, int statusRank = 1, int castId = 0)
        {
            GameObject obj = new GameObject("Skill Area");
            obj.transform.position = new Vector3(at.x,0,at.z);
            CombatArea area = obj.AddComponent<CombatArea>();
            area.owner = player; area.session = game; area.epoch = player.CombatEpoch;
            area.radius = size; area.damage = amount; area.stun = disable;
            area.delay = startup; area.duration = activeTime; area.interval = Mathf.Max(.1f,tickInterval);
            area.color = tint; area.follow = followPlayer; area.meteor = fallingMeteor;
            area.pullStrength = pulling; area.finalDamage = finisher;
            area.statusSkill = statusSkill; area.statusRank = statusRank; area.castId = castId;
            area.nextTick = startup;
            area.marker = CombatFx.Ring(at, size, tint, startup + activeTime + .2f, .075f, false);
            area.fireVisual = fallingMeteor || (player.HeroClass == HeroClass.Arcanist && tint.r > .8f && tint.g < .7f);
            area.poisonVisual = !area.fireVisual &&
                ((tint.g > .9f && tint.r < .8f && tint.b < .8f) ||
                 (statusSkill == 5 && player.HeroClass == HeroClass.Ranger) ||
                 (statusSkill == 1 && player.HeroClass == HeroClass.Summoner));
            area.lightningVisual = player.HeroClass == HeroClass.Arcanist && tint.b > .9f && tint.r > .55f && tint.g < .7f;
            if (area.fireVisual || area.poisonVisual || area.lightningVisual)
                ElementalCombatVfx.Area(obj.transform, size,
                    area.fireVisual ? ElementalCombatVfx.Element.Fire :
                    area.poisonVisual ? ElementalCombatVfx.Element.Poison : ElementalCombatVfx.Element.Lightning);
            if (fallingMeteor)
            {
                area.fallingOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                area.fallingOrb.name = "Falling Meteor";
                Destroy(area.fallingOrb.GetComponent<Collider>());
                area.fallingOrb.transform.SetParent(obj.transform, false);
                area.fallingOrb.transform.localPosition = Vector3.up * 9f;
                area.fallingOrb.transform.localScale = Vector3.one * 1.1f;
                area.orbMaterial = new Material(Shader.Find("Unlit/Color"));
                area.orbMaterial.color = new Color(1f,.6f,.26f);
                area.fallingOrb.GetComponent<Renderer>().sharedMaterial = area.orbMaterial;
            }
        }

        private void Update()
        {
            if (owner == null || session == null || session.Player != owner || owner.IsDead || !session.HasStarted || owner.CombatEpoch != epoch)
            { Destroy(gameObject); return; }
            if (Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (follow) transform.position = owner.transform.position;
            if (marker != null) marker.transform.position = transform.position + Vector3.up * .065f;
            if(pullStrength>0)
            {
                for(int i=0;i<session.Enemies.Count;i++)
                {
                    EnemyController enemy=session.Enemies[i];
                    if(enemy==null || enemy.IsDead) continue;
                    Vector3 delta=CombatFx.Flat(transform.position-enemy.transform.position);
                    if(delta.magnitude<radius+1f && delta.magnitude>.55f)
                        enemy.transform.position = WorldTraversal.Move(enemy.transform.position, delta.normalized * Mathf.Min(delta.magnitude - .55f, pullStrength * Time.deltaTime * (enemy.IsBoss ? .25f : 1f)), enemy.NavigationRadius);
                }
            }
            if (fallingOrb != null)
            {
                fallingOrb.transform.localPosition = Vector3.up * Mathf.Lerp(9f,.5f,Mathf.Clamp01(age/Mathf.Max(.01f,delay)));
                if (age >= delay) { Destroy(fallingOrb); fallingOrb = null; }
            }
            if (age >= nextTick && nextTick <= delay + duration + .01f)
            {
                nextTick += interval;
                CombatFx.Ring(transform.position,radius,color,.42f,.15f);
                if (lightningVisual)
                    for (int bolt = 0; bolt < 3; bolt++)
                    {
                        Vector2 point = Random.insideUnitCircle * radius * .85f;
                        Vector3 end = transform.position + new Vector3(point.x, .15f, point.y);
                        ElementalCombatVfx.Lightning(transform.position + Vector3.up * Random.Range(2.1f, 3.5f), end);
                    }
                if (!meteor && !follow)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        Vector2 offset = Random.insideUnitCircle * radius * .65f;
                        CombatFx.Slash(transform.position + new Vector3(offset.x, .4f, offset.y),Vector3.forward,.4f,color);
                    }
                }
                for (int i = session.Enemies.Count - 1; i >= 0; i--)
                {
                    EnemyController enemy = session.Enemies[i];
                    if (enemy == null || enemy.IsDead) continue;
                    Vector3 delta = CombatFx.Flat(enemy.transform.position - transform.position);
                    if (delta.magnitude <= radius + (enemy.IsBoss ? .85f : .4f))
                    {
                        if (fireVisual)
                            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Fire, 2.5f);
                        if (poisonVisual)
                            ElementalCombatVfx.OnEnemy(enemy, ElementalCombatVfx.Element.Poison, 2f);
                        enemy.TakeDamage(owner.ResolveSkillImpact(enemy, statusSkill, castId, damage.Amount),delta.normalized,.3f,stun,critical:damage.IsCritical);
                        if (lightningVisual)
                            ElementalCombatVfx.Lightning(transform.position + Vector3.up * 2f,
                                enemy.transform.position + Vector3.up * (enemy.IsBoss ? 2f : 1f));
                        if (enemy.StatusEffects != null && statusSkill == 0 && owner.HeroClass == HeroClass.Arcanist)
                            owner.ApplyNovaStatus(enemy, statusRank);
                        if (enemy.StatusEffects != null && ((statusSkill == 5 && owner.HeroClass == HeroClass.Ranger) || (statusSkill == 1 && owner.HeroClass == HeroClass.Summoner)))
                        {
                            enemy.StatusEffects.Slow(3f, .3f + statusRank * .07f);
                            enemy.StatusEffects.Poison(owner, 3.5f + statusRank * .5f, damage.Amount * .2f);
                        }
                    }
                }
            }
            if (!finished && finalDamage.Amount>0 && age>=delay+duration)
            {
                finished=true;
                AdvancedSkillVfx.Rune(owner,transform.position,radius*1.1f,color,.65f,3);
                owner.HitArea(transform.position,radius*1.1f,finalDamage,.7f,.65f);
            }
            if (age > delay + duration + .1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (marker != null) Destroy(marker);
            if (orbMaterial != null) Destroy(orbMaterial);
        }
    }
}
