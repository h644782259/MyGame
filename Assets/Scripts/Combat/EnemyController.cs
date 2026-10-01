using UnityEngine;

namespace Emberfall
{
    public sealed class EnemyController : MonoBehaviour
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get { return Health <= 0; } }
        public EnemyKind Kind { get; private set; }
        public bool IsBoss { get; private set; }
        public string DisplayName { get; private set; }

        private enum AttackType { Melee, Bolt, Slam, Charge, Fan }
        private GameSession session;
        private CombatModel model;
        private float speed, damage, attackCooldown, windup, stunTime, hurtTime, attackAnimation, patrolPhase;
        private int attackNumber;
        private Vector3 origin, targetPoint, knockVelocity, chargeDirection;
        private float chargeTime;
        private bool preparing, aggro, deathReported, chargeHit;
        private AttackType attackType;
        private GameObject warning;
        private Transform healthRoot, healthFill;
        private Material healthBackgroundMaterial, healthFillMaterial;

        public void Initialize(GameSession game, EnemyKind kind, int level, bool boss = false)
        {
            session = game;
            Kind = kind;
            IsBoss = boss;
            level = Mathf.Max(1,level);
            DisplayName = boss ? "星蚀巨像" : new[] { "森林史莱姆", "盗宝哥布林", "幽光魔灵", "遗迹守卫" }[(int)kind];
            gameObject.name = DisplayName;
            float[] baseHealth = { 32, 46, 35, 100 };
            float[] healthGrowth = { 9, 12, 10, 24 };
            float[] moveSpeed = { 2.05f, 3.1f, 2.5f, 2.1f };
            MaxHealth = boss ? 310 + level * 65 : baseHealth[(int)kind] + level * healthGrowth[(int)kind];
            Health = MaxHealth;
            damage = (boss ? 14f : 6f) + level * (boss ? 2.5f : 1.7f);
            speed = boss ? 2.35f : moveSpeed[(int)kind];
            origin = transform.position;
            patrolPhase = Random.value * Mathf.PI * 2f;
            attackCooldown = Random.Range(.5f,1.2f);
            model = CombatModel.Enemy(transform,kind,boss);
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
            healthFillMaterial.color = IsBoss ? new Color(1f,.43f,.28f) : new Color(.94f,.31f,.41f);
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

        public void TakeDamage(float amount, Vector3 direction, float knockback = 0f, float stun = 0f)
        {
            if (session == null || IsDead || amount <= 0) return;
            Health = Mathf.Max(0,Health-amount);
            aggro = true;
            hurtTime = .15f;
            float resistance = IsBoss ? .24f : 1f;
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

        private void Update()
        {
            if (session == null || session.Player == null || IsDead || !session.HasStarted || session.Paused || session.IsDead) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            hurtTime = Mathf.Max(0,hurtTime-dt);
            attackAnimation = Mathf.Max(0,attackAnimation-dt*3f);
            attackCooldown = Mathf.Max(0,attackCooldown-dt);
            stunTime = Mathf.Max(0,stunTime-dt);
            transform.position += knockVelocity * dt;
            knockVelocity = Vector3.Lerp(knockVelocity,Vector3.zero,Mathf.Min(1,dt*12f));
            Vector3 delta = CombatFx.Flat(session.Player.transform.position-transform.position);
            float distance = delta.magnitude;
            if (session.InDungeon || distance < (IsBoss?15f:9f)) aggro = true;
            if (!session.InDungeon && distance > 17f) aggro = false;
            if (stunTime > 0)
            {
                model.Animate(0,attackAnimation,hurtTime>0);
                ClampPosition();
                return;
            }
            if (chargeTime > 0)
            {
                chargeTime -= dt;
                Vector3 previous = transform.position;
                transform.position += chargeDirection * 11f * dt;
                if (!chargeHit && CombatFx.SegmentDistance(session.Player.transform.position,previous,transform.position) < 1.3f)
                {
                    session.Player.TakeDamage(damage*1.35f);
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
                if (distance <= range && attackCooldown <= 0) BeginAttack();
                else if (distance > range*.82f)
                {
                    Vector3 step = delta.normalized + Separation();
                    transform.position += Vector3.ClampMagnitude(step,1.2f)*speed*dt;
                    model.Animate(1,attackAnimation,hurtTime>0);
                }
                else if (Kind==EnemyKind.Wisp && distance<3.5f)
                {
                    transform.position-=delta.normalized*speed*.7f*dt;
                    model.Animate(.5f,attackAnimation,hurtTime>0);
                }
                else model.Animate(0,attackAnimation,hurtTime>0);
            }
            else
            {
                Vector3 patrol = origin + new Vector3(Mathf.Sin(Time.time*.28f+patrolPhase),0,Mathf.Cos(Time.time*.28f+patrolPhase)) * 1.4f;
                Vector3 toPatrol = CombatFx.Flat(patrol-transform.position);
                transform.position += Vector3.ClampMagnitude(toPatrol,1)*speed*.22f*dt;
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

        private void BeginAttack()
        {
            targetPoint=session.Player.transform.position;
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
                    for(int i=-2;i<=2;i++) CombatProjectile.Hostile(session,transform.position+forward,Quaternion.Euler(0,i*17,0)*forward,damage,7f);
                }
                else CombatProjectile.Hostile(session,transform.position+forward*.7f,forward,damage,7.5f);
            }
            else
            {
                float radius=attackType==AttackType.Slam?(IsBoss?3.7f:2.65f):1.2f;
                if(attackType==AttackType.Melee && Kind==EnemyKind.Slime)
                    transform.position=Vector3.MoveTowards(transform.position,targetPoint,1.25f);
                CombatFx.Ring(targetPoint,radius,new Color(1f,.45f,.25f),.32f,.15f);
                if(CombatFx.Flat(session.Player.transform.position-targetPoint).magnitude<radius+.35f)
                    session.Player.TakeDamage(damage*(attackType==AttackType.Slam?1.4f:1f));
            }
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
            Vector3 point=transform.position;
            float bound=Mathf.Max(1,session.ArenaRadius-(IsBoss?1.1f:.55f));
            point.y=0;
            transform.position=Vector3.ClampMagnitude(point,bound);
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
