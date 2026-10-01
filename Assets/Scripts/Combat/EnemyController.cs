using UnityEngine;

namespace Emberfall
{
    public sealed class EnemyController : MonoBehaviour
    {
        public enum ThreatTier { Normal, Elite, Boss }
        public ThreatTier Tier { get; private set; }
        public bool IsAggro { get { return aggro; } }
        public bool IsStunned { get { return stunTime > 0; } }
        public EnemyStatusEffects StatusEffects { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get { return Health <= 0; } }
        public EnemyKind Kind { get; private set; }
        public bool IsBoss { get; private set; }
        public float NavigationRadius { get { return IsBoss ? .9f : Kind == EnemyKind.Guardian ? .6f : .45f; } }
        public string DisplayName { get; private set; }
        public string TraitDescription { get { return IsBoss ? "首领：扇形弹幕、范围震地与直线冲锋轮换；抵抗控制。" : Kind == EnemyKind.Slime ? "跳扑近身，黏液命中使你暂时减速。" : Kind == EnemyKind.Goblin ? "绕侧接近，近身后快速出刀并侧移。" : Kind == EnemyKind.Wisp ? "保持远距离游走，发射双重灵弹。" : "正面石甲减伤35%；重击蓄力时护甲失效。"; } }

        private enum AttackType { Melee, Bolt, Slam, Charge, Fan }
        private GameSession session;
        private CombatModel model;
        private float speed, damage, attackCooldown, windup, stunTime, hurtTime, attackAnimation, patrolPhase;
        private int attackNumber;
        private Vector3 origin, targetPoint, knockVelocity, chargeDirection;
        private float chargeTime;
        private float flinchUntil, nextImpactTime;
        private SummonedCompanion companionTarget;
        private float sidestepTime;
        private Vector3 sidestepDirection;
        private bool preparing, aggro, deathReported, chargeHit;
        private AttackType attackType;
        private GameObject warning;
        private Transform healthRoot, healthFill;
        private Material healthBackgroundMaterial, healthFillMaterial;
        private readonly WorldTraversal.Route route = new WorldTraversal.Route();

        public void Initialize(GameSession game, EnemyKind kind, int level, bool boss = false)
        {
            session = game;
            Kind = kind;
            IsBoss = boss;
            Tier = boss ? ThreatTier.Boss : game.InDungeon ? ThreatTier.Elite : ThreatTier.Normal;
            level = Mathf.Max(1,level);
            DisplayName = (Tier == ThreatTier.Boss ? "首领 · " : Tier == ThreatTier.Elite ? "精英 · " : "普通 · ") +
                (boss ? "星蚀巨像" : new[] { "森林史莱姆", "盗宝哥布林", "幽光魔灵", "遗迹守卫" }[(int)kind]);
            gameObject.name = DisplayName;
            float[] baseHealth = { 32, 46, 35, 100 };
            float[] healthGrowth = { 9, 12, 10, 24 };
            float[] moveSpeed = { 2.05f, 3.1f, 2.5f, 2.1f };
            MaxHealth = boss ? 310 + level * 65 : baseHealth[(int)kind] + level * healthGrowth[(int)kind];
            Health = MaxHealth;
            damage = (boss ? 14f : 6f) + level * (boss ? 2.5f : 1.7f);
            speed = boss ? 2.35f : moveSpeed[(int)kind];
            transform.position = WorldTraversal.NearestWalkable(transform.position, NavigationRadius);
            origin = transform.position;
            patrolPhase = Random.value * Mathf.PI * 2f;
            attackCooldown = Random.Range(.5f,1.2f);
            model = CombatModel.Enemy(transform,kind,boss);
            StatusEffects = gameObject.AddComponent<EnemyStatusEffects>();
            BuildHealthBar();
        }

        private void BuildHealthBar()
        {
            GameObject root = new GameObject("Enemy Health");
            root.transform.SetParent(transform,false);
            root.transform.localPosition = Vector3.up * (IsBoss ? 4.2f : Kind == EnemyKind.Slime ? 1.45f : 2.6f);
            healthRoot = root.transform;
            float width = IsBoss ? 2.1f : 1.05f;
            healthBackgroundMaterial = new Material(Shader.Find("Unlit/Color"));
            healthBackgroundMaterial.color = new Color(.12f,.12f,.19f);
            healthFillMaterial = new Material(Shader.Find("Unlit/Color"));
            healthFillMaterial.color = ThreatColor();
            Transform background = HealthQuad("Background",healthBackgroundMaterial);
            background.localScale = new Vector3(width+.06f,.14f,1);
            healthFill = HealthQuad("Health",healthFillMaterial);
            healthFill.localPosition = new Vector3(0,0,-.012f);
            healthFill.localScale = new Vector3(width,.095f,1);
        }

        private Transform HealthQuad(string title, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = title;
            Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(healthRoot,false);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj.transform;
        }

        public void TakeDamage(float amount, Vector3 direction, float knockback = 0f, float stun = 0f, bool impact = true)
        {
            if (session == null || IsDead || amount <= 0) return;
            amount *= StatusEffects == null ? 1 : StatusEffects.DamageMultiplier;
            if (Kind == EnemyKind.Guardian && !IsBoss && !preparing && CombatFx.Flat(direction).sqrMagnitude > .01f && Vector3.Dot(transform.forward, -CombatFx.Flat(direction).normalized) > .45f)
                amount *= .65f;
            Health = Mathf.Max(0,Health-amount);
            aggro = true;
            hurtTime = .15f;
            float resistance = IsBoss ? .24f : 1f;
            if (impact && Time.time >= nextImpactTime)
            {
                nextImpactTime = Time.time + .10f;
                Vector3 push = CombatFx.Flat(direction).normalized;
                if (push.sqrMagnitude < .01f) push = -transform.forward;
                float strength = Mathf.Clamp(amount / Mathf.Max(1f, session.Progression.GetStats().Damage), .55f, 2f);
                model.Recoil(push, strength * (IsBoss ? .5f : 1f));
                flinchUntil = Time.time + (IsBoss ? .018f : Mathf.Lerp(.035f, .065f, strength / 2f));
                HitFeedback.Spawn(transform.position + Vector3.up * (IsBoss ? 2f : Kind == EnemyKind.Slime ? .65f : 1.25f), push, strength);
                GameAudio.Play(SoundCue.Hit);
            }
            knockVelocity += CombatFx.Flat(direction).normalized * knockback * 7f * resistance;
            stunTime = Mathf.Max(stunTime,stun*resistance);
            if (stun >= .45f && !IsBoss) CancelAttack();
            session.SpawnFloatingText(transform.position+Vector3.up*(IsBoss?3.6f:1.9f),Mathf.CeilToInt(amount).ToString(),new Color(1f,.86f,.48f));
            if (Health <= 0 && !deathReported)
            {
                deathReported = true;
                CancelAttack();
                CombatFx.Ring(transform.position,IsBoss?2.5f:1.1f,new Color(1f,.77f,.35f),.45f,.13f);
                session.OnEnemyKilled(this);
            }
        }

        internal void ApplyControl(float duration)
        {
            if(IsDead || duration<=0) return;
            aggro=true;
            stunTime=Mathf.Max(stunTime,duration*(IsBoss?.24f:1f));
            if(duration>=.45f && !IsBoss) CancelAttack();
        }

        internal void Provoke() { if (!IsDead) aggro = true; }

        private void Update()
        {
            if (session == null || session.Player == null || IsDead || !session.HasStarted || session.Paused || session.IsDead) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            hurtTime = Mathf.Max(0,hurtTime-dt);
            attackAnimation = Mathf.Max(0,attackAnimation-dt*3f);
            attackCooldown = Mathf.Max(0,attackCooldown-dt);
            stunTime = Mathf.Max(0,stunTime-dt);
            transform.position = WorldTraversal.Move(transform.position, knockVelocity * dt, NavigationRadius);
            knockVelocity = Vector3.Lerp(knockVelocity,Vector3.zero,Mathf.Min(1,dt*12f));
            companionTarget = aggro || Tier != ThreatTier.Normal ? SummonedCompanion.ThreatTarget(this, session.Player.transform.position) : null;
            Vector3 combatTargetPosition = companionTarget != null ? companionTarget.transform.position : session.Player.transform.position;
            Vector3 delta = CombatFx.Flat(combatTargetPosition-transform.position);
            float distance = delta.magnitude;
            float effectiveSpeed = speed * (StatusEffects == null ? 1 : StatusEffects.MoveMultiplier);
            // Ordinary wildlife stays neutral regardless of proximity. Damage/control
            // explicitly provokes retaliation; only elites and bosses acquire on sight.
            if (Tier != ThreatTier.Normal && (session.InDungeon || distance < (IsBoss?15f:9f))) aggro = true;
            if (!session.InDungeon && distance > 17f) aggro = false;
            healthFillMaterial.color = ThreatColor();
            if (stunTime > 0 || Time.time < flinchUntil)
            {
                model.Animate(0,attackAnimation,hurtTime>0);
                ClampPosition();
                return;
            }
            if (sidestepTime > 0)
            {
                sidestepTime -= dt;
                transform.position = WorldTraversal.Move(transform.position, sidestepDirection * effectiveSpeed * 1.6f * dt, NavigationRadius);
                model.Animate(1, attackAnimation, hurtTime > 0);
                ClampPosition(); return;
            }
            if (chargeTime > 0)
            {
                chargeTime -= dt;
                Vector3 previous = transform.position;
                transform.position = WorldTraversal.Move(previous, chargeDirection * 11f * dt, NavigationRadius);
                if (!chargeHit && CombatFx.SegmentDistance(combatTargetPosition,previous,transform.position) < 1.3f && WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, .12f))
                {
                    DamageTarget(damage*1.35f);
                    chargeHit = true;
                }
                model.Animate(1,.6f,hurtTime>0);
            }
            else if (preparing)
            {
                windup -= dt;
                model.Animate(0,.95f,hurtTime>0);
                if (windup <= 0) ResolveAttack();
            }
            else if (aggro)
            {
                if (delta.sqrMagnitude>.01f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),dt*9f);
                float range = Kind==EnemyKind.Wisp ? 7.5f : IsBoss ? 3.5f : Kind==EnemyKind.Guardian ? 2.5f : 1.8f;
                bool attackPath = Kind == EnemyKind.Wisp ? WorldTraversal.HasLineOfSight(transform.position, combatTargetPosition) : WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, .12f);
                if (distance <= range && attackCooldown <= 0 && attackPath) BeginAttack();
                else if (Kind==EnemyKind.Wisp && distance<4.5f && attackPath)
                {
                    transform.position = WorldTraversal.Move(transform.position, -delta.normalized * effectiveSpeed * dt, NavigationRadius);
                    model.Animate(.7f,attackAnimation,hurtTime>0);
                }
                else if (distance > range*.82f || !attackPath)
                {
                    bool directGround = WorldTraversal.HasGroundPath(transform.position, combatTargetPosition, NavigationRadius);
                    Vector3 step = route.Direction(transform.position, combatTargetPosition, NavigationRadius) + Separation() * (directGround ? 1f : .15f);
                    if (directGround && Kind == EnemyKind.Goblin && distance > 2.5f && distance < 9f)
                        step += Vector3.Cross(Vector3.up, delta.normalized) * Mathf.Sin(patrolPhase + Time.time * .8f) * .8f;
                    transform.position = WorldTraversal.Move(transform.position, Vector3.ClampMagnitude(step, 1.2f) * effectiveSpeed * dt, NavigationRadius);
                    model.Animate(1,attackAnimation,hurtTime>0);
                }
                else model.Animate(0,attackAnimation,hurtTime>0);
            }
            else
            {
                Vector3 patrol = origin + new Vector3(Mathf.Sin(Time.time*.28f+patrolPhase),0,Mathf.Cos(Time.time*.28f+patrolPhase)) * 1.4f;
                Vector3 toPatrol = CombatFx.Flat(patrol-transform.position);
                Vector3 patrolDirection = route.Direction(transform.position, patrol, NavigationRadius);
                transform.position = WorldTraversal.Move(transform.position, patrolDirection * Mathf.Min(1, toPatrol.magnitude) * effectiveSpeed * .22f * dt, NavigationRadius);
                if(toPatrol.sqrMagnitude>.1f) transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toPatrol),dt*2f);
                model.Animate(.2f,0,false);
            }
            ClampPosition();
        }

        private Vector3 Separation()
        {
            Vector3 force=Vector3.zero;
            for (int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy==this || enemy.IsDead) continue;
                Vector3 away=CombatFx.Flat(transform.position-enemy.transform.position);
                float distance=away.magnitude;
                if(distance>.01f && distance<1.35f) force+=away.normalized*(1.35f-distance)*1.3f;
            }
            return Vector3.ClampMagnitude(force,.9f);
        }

        private Color ThreatColor()
        {
            if (Tier == ThreatTier.Boss) return new Color(1f, .3f, .25f);
            if (Tier == ThreatTier.Elite) return new Color(1f, .72f, .25f);
            return aggro ? new Color(1f, .46f, .27f) : new Color(.4f, .87f, .6f);
        }

        private void BeginAttack()
        {
            targetPoint=companionTarget != null ? companionTarget.transform.position : session.Player.transform.position;
            attackNumber++;
            preparing=true;
            Color warningColor=new Color(1f,.24f,.29f,.9f);
            if(IsBoss)
            {
                int pattern=attackNumber%3;
                attackType=pattern==0?AttackType.Fan:pattern==1?AttackType.Slam:AttackType.Charge;
                windup=attackType==AttackType.Charge?1.05f:.95f;
                float radius=attackType==AttackType.Slam?3.7f:attackType==AttackType.Fan?1.2f:1.5f;
                if(attackType==AttackType.Slam) targetPoint=transform.position;
                warning=CombatFx.Ring(targetPoint,radius,warningColor,windup+.1f,.14f,false);
                if(attackType==AttackType.Charge)
                {
                    chargeDirection=CombatFx.Flat(targetPoint-transform.position).normalized;
                    CombatFx.Slash(transform.position,chargeDirection,3.7f,warningColor);
                }
            }
            else if(Kind==EnemyKind.Wisp)
            {
                attackType=AttackType.Bolt;
                windup=.72f;
                warning=CombatFx.Ring(transform.position,1.0f,new Color(.9f,.35f,1f),windup+.1f,.09f,false);
            }
            else
            {
                attackType=Kind==EnemyKind.Guardian?AttackType.Slam:AttackType.Melee;
                windup=Kind==EnemyKind.Guardian?.85f:Kind==EnemyKind.Slime?.6f:.48f;
                if(attackType==AttackType.Slam) targetPoint=transform.position;
                warning=CombatFx.Ring(targetPoint,attackType==AttackType.Slam?2.65f:1.2f,warningColor,windup+.12f,.08f,false);
            }
        }

        private void ResolveAttack()
        {
            preparing=false;
            if(warning!=null) Destroy(warning);
            warning=null;
            attackAnimation=1f;
            attackCooldown=IsBoss?1.15f:Kind==EnemyKind.Wisp?1.55f:1.3f;
            if(attackType==AttackType.Charge)
            {
                chargeHit=false;
                chargeTime=Mathf.Clamp(Vector3.Distance(transform.position,targetPoint)/11f+.1f,.2f,.7f);
            }
            else if(attackType==AttackType.Bolt || attackType==AttackType.Fan)
            {
                Vector3 forward=CombatFx.Flat(targetPoint-transform.position).normalized;
                if(forward.sqrMagnitude<.1f) forward=transform.forward;
                if(attackType==AttackType.Fan)
                {
                    if (!WorldTraversal.HasLineOfSight(transform.position, transform.position + forward)) return;
                    for(int i=-2;i<=2;i++) CombatProjectile.Hostile(session,transform.position+forward,Quaternion.Euler(0,i*17,0)*forward,damage,7f);
                }
                else
                {
                    if (!WorldTraversal.HasLineOfSight(transform.position, transform.position + forward * .7f)) return;
                    CombatProjectile.Hostile(session,transform.position+forward*.7f,Quaternion.Euler(0,-7,0)*forward,damage*.75f,7.5f);
                    CombatProjectile.Hostile(session,transform.position+forward*.7f,Quaternion.Euler(0,7,0)*forward,damage*.75f,7.5f);
                }
            }
            else
            {
                float radius=attackType==AttackType.Slam?(IsBoss?3.7f:2.65f):1.2f;
                if(attackType==AttackType.Melee && Kind==EnemyKind.Slime)
                    transform.position = WorldTraversal.Move(transform.position, Vector3.ClampMagnitude(CombatFx.Flat(targetPoint - transform.position), 1.25f), NavigationRadius);
                CombatFx.Ring(targetPoint,radius,new Color(1f,.45f,.25f),.32f,.15f);
                Vector3 victim = companionTarget != null ? companionTarget.transform.position : session.Player.transform.position;
                if(CombatFx.Flat(victim-targetPoint).magnitude<radius+.35f && WorldTraversal.HasGroundPath(transform.position, victim, .12f))
                {
                    float previousHealth = session.Player.Health;
                    DamageTarget(damage*(attackType==AttackType.Slam?1.4f:1f));
                    if (Kind == EnemyKind.Slime && companionTarget == null && session.Player.Health < previousHealth) session.Player.ApplySlow(1.8f, .35f);
                }
                if (Kind == EnemyKind.Goblin)
                {
                    sidestepTime = .28f;
                    sidestepDirection = Vector3.Cross(Vector3.up, transform.forward) * (attackNumber % 2 == 0 ? 1f : -1f);
                }
            }
        }

        private void DamageTarget(float amount)
        {
            Vector3 victim = companionTarget != null && companionTarget.IsAlive ? companionTarget.transform.position : session.Player.transform.position;
            if (!WorldTraversal.HasGroundPath(transform.position, victim, .12f)) return;
            if (companionTarget != null && companionTarget.IsAlive) companionTarget.TakeDamage(amount);
            else session.Player.TakeDamage(amount);
        }

        private void CancelAttack()
        {
            preparing=false;
            chargeTime=0;
            attackCooldown=Mathf.Max(attackCooldown,.55f);
            if(warning!=null) Destroy(warning);
            warning=null;
        }

        private void ClampPosition()
        {
            transform.position = WorldTraversal.Move(transform.position, Vector3.zero, NavigationRadius);
        }

        private void LateUpdate()
        {
            if(healthRoot==null) return;
            healthRoot.gameObject.SetActive(!IsDead && (aggro || Health<MaxHealth || IsBoss));
            Camera camera=Camera.main;
            if(camera!=null) healthRoot.rotation=camera.transform.rotation;
            float fraction=Mathf.Clamp01(Health/Mathf.Max(1,MaxHealth));
            float width=IsBoss?2.1f:1.05f;
            healthFill.localScale=new Vector3(width*fraction,.095f,1);
            healthFill.localPosition=new Vector3((fraction-1)*width*.5f,0,-.012f);
        }

        private void OnDestroy()
        {
            if(warning!=null) Destroy(warning);
            if(healthBackgroundMaterial!=null) Destroy(healthBackgroundMaterial);
            if(healthFillMaterial!=null) Destroy(healthFillMaterial);
        }
    }
}
