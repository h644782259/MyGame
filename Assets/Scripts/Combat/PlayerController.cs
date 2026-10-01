using UnityEngine;

namespace Emberfall
{
    public sealed class PlayerController : MonoBehaviour
    {
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get { return Health <= 0; } }
        public HeroClass HeroClass { get; private set; }
        public float DodgeCooldown { get { return dodgeCooldown; } }
        public float Energy { get { return skillRuntime.Energy; } }
        public float MaxEnergy { get { return SkillRuntime.MaximumEnergy; } }
        internal int CombatEpoch { get; private set; }

        private GameSession session;
        private StatBlock stats;
        private CombatModel model;
        private readonly SkillRuntime skillRuntime = new SkillRuntime();
        private float attackCooldown, attackAnimation, hurtTimer, dodgeCooldown, dodgeTime, invulnerability, skillFeedbackCooldown;
        private float guardTime, guardPower, guardReduction, guardRadius, guardPulseTimer;
        private int guardRank, mobilityRank;
        private float healingProtectionTime, healingReduction, mobilityTime;
        private float passiveCooldown, passiveTime, passiveReduction, passiveSpeed;
        private Vector3 dodgeDirection, aimPoint;
        private GameObject dodgeHalo;

        public void Initialize(GameSession game, HeroClass heroClass)
        {
            session = game;
            HeroClass = heroClass;
            gameObject.name = "Hero - " + GameBalance.ClassName(heroClass);
            if (model != null) Destroy(model.gameObject);
            model = CombatModel.Hero(transform, heroClass);
            RefreshStats(true);
            aimPoint = transform.position + Vector3.forward * 5;
        }

        public void RefreshStats(bool heal)
        {
            if (session == null) return;
            float previousMaximum = MaxHealth;
            bool wasDead = previousMaximum > 0 && Health <= 0;
            stats = session.Progression.GetStats();
            MaxHealth = Mathf.Max(1f, stats.MaxHealth);
            if (heal) Health = MaxHealth;
            else if (!wasDead) Health = Mathf.Clamp(Health, 1, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0) return;
            float healed = Mathf.Min(amount,MaxHealth - Health);
            Health += healed;
            if (healed > .5f)
            {
                session.SpawnFloatingText(transform.position + Vector3.up * 2.4f,"+" + Mathf.CeilToInt(healed),new Color(.42f,1f,.65f));
                CombatFx.Ring(transform.position,1.2f,new Color(.35f,1f,.6f),.5f);
            }
        }

        public void Teleport(Vector3 position)
        {
            CombatEpoch++;
            position.y = 0;
            transform.position = position;
            dodgeTime = 0;
            guardTime = healingProtectionTime = mobilityTime = passiveTime = 0;
            attackAnimation = 0;
            attackCooldown = .15f;
            invulnerability = .65f;
            if (dodgeHalo != null) Destroy(dodgeHalo);
        }

        public void TakeDamage(float amount)
        {
            if (session == null || IsDead || invulnerability > 0 || !session.HasStarted) return;
            float damage = Mathf.Max(1, amount * (100f / (100f + Mathf.Max(0,stats.Armor) * 4f)));
            if (guardTime > 0)
            {
                damage *= 1f-guardReduction;
                if (HeroClass == HeroClass.Vanguard)
                {
                    AdvancedSkillVfx.Rune(this, transform.position, guardRadius, new Color(1f,.84f,.4f), .55f, guardRank);
                    HitArea(transform.position, guardRadius, stats.Damage * guardPower, .5f, .2f);
                }
            }
            if (healingProtectionTime > 0) damage *= 1f-healingReduction;
            if (passiveTime > 0) damage *= 1f-passiveReduction;
            Health = Mathf.Max(0,Health - damage);
            GameAudio.Play(SoundCue.Hit);
            invulnerability = .2f;
            hurtTimer = .22f;
            session.SpawnFloatingText(transform.position + Vector3.up * 2.5f,"−" + Mathf.CeilToInt(damage),new Color(1f,.4f,.42f));
            CombatFx.Ring(transform.position,.85f,new Color(1f,.27f,.3f),.2f);
            if (Health > 0) TryDefensePassive();
            if (Health <= 0)
            {
                CombatEpoch++;
                dodgeTime = 0;
                model.transform.localRotation = Quaternion.Euler(0,0,75f);
                session.OnPlayerDied();
            }
        }

        public float CooldownRemaining(int slot) { return SkillCooldownRemaining(HotbarSkill(slot)); }
        public float SkillCooldownRemaining(int skillIndex) { return skillRuntime.Remaining(skillIndex); }

        private int HotbarSkill(int slot)
        {
            if (session == null || slot < 0 || slot >= GameBalance.HotbarSize) return -1;
            GameProfile profile = session.Progression.Profile;
            int index = profile.hotbarPage * GameBalance.HotbarSize + slot;
            return profile.equippedSkills != null && index >= 0 && index < profile.equippedSkills.Length ? profile.equippedSkills[index] : -1;
        }

        private void Update()
        {
            if (session == null || model == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            attackCooldown = Mathf.Max(0,attackCooldown - dt);
            attackAnimation = Mathf.Max(0,attackAnimation - dt * 4f);
            hurtTimer = Mathf.Max(0,hurtTimer - dt);
            dodgeCooldown = Mathf.Max(0,dodgeCooldown - dt);
            invulnerability = Mathf.Max(0,invulnerability - dt);
            skillFeedbackCooldown = Mathf.Max(0,skillFeedbackCooldown - dt);
            if (IsDead) return;
            skillRuntime.Advance(dt);
            guardTime = Mathf.Max(0, guardTime - dt);
            healingProtectionTime = Mathf.Max(0,healingProtectionTime-dt);
            mobilityTime = Mathf.Max(0,mobilityTime-dt);
            passiveTime = Mathf.Max(0,passiveTime-dt);
            passiveCooldown = Mathf.Max(0,passiveCooldown-dt);
            if (HeroClass == HeroClass.Arcanist && guardTime > 0)
            {
                guardPulseTimer -= dt;
                if (guardPulseTimer <= 0)
                {
                    guardPulseTimer = guardRank==3?1f:guardRank==2?1.2f:1.5f;
                    ControlArea(transform.position,guardRadius,.45f+guardRank*.15f);
                    CombatFx.Ring(transform.position,guardRadius,new Color(.56f,.93f,1f),.4f,.12f);
                }
            }
            model.transform.localRotation = Quaternion.identity;
            if (session.InputBlocked)
            {
                dodgeTime = Mathf.Max(0,dodgeTime - dt);
                model.Animate(0,attackAnimation,hurtTimer > 0);
                return;
            }
            if (!session.PointerOverUI) UpdateAim();
            Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
            movement = Vector3.ClampMagnitude(movement,1);
            if (Input.GetKeyDown(KeyCode.Space) && dodgeCooldown <= 0)
            {
                dodgeDirection = movement.sqrMagnitude > .01f ? movement.normalized : transform.forward;
                GameAudio.Play(SoundCue.Dodge);
                dodgeTime = .27f;
                dodgeCooldown = 2.1f;
                invulnerability = .38f;
                dodgeHalo = CombatFx.Ring(transform.position,1.1f,GameBalance.ClassColor(HeroClass),.36f,.13f);
            }
            if (dodgeTime > 0)
            {
                dodgeTime -= dt;
                transform.position += dodgeDirection * 15f * dt;
            }
            else
            {
                float movementBonus = passiveTime>0?passiveSpeed:0;
                if (mobilityTime>0) movementBonus += .1f+mobilityRank*.05f;
                transform.position += movement * stats.MoveSpeed * (1f+movementBonus) * dt;
                if (!session.PointerOverUI)
                {
                    if ((Input.GetMouseButton(0) || Input.GetKey(KeyCode.J)) && attackCooldown <= 0) BasicAttack();
                    GameProfile profile = session.Progression.Profile;
                    for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
                        if (profile.hotbarKeys != null && slot < profile.hotbarKeys.Length && Input.GetKeyDown((KeyCode)profile.hotbarKeys[slot]))
                        {
                            int skill = HotbarSkill(slot);
                            if (skill >= 0 && skill < GameBalance.SkillCount) CastSkill(skill);
                        }
                }
            }
            Vector3 bounded = transform.position;
            float bound = Mathf.Max(1,session.ArenaRadius - .65f);
            bounded.y = 0;
            transform.position = Vector3.ClampMagnitude(bounded,bound);
            model.Animate(dodgeTime > 0 ? 1 : movement.magnitude,attackAnimation,hurtTimer > 0);
        }

        private void UpdateAim()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                float enter;
                if (new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter)) aimPoint = ray.GetPoint(enter);
            }
            Vector3 forward = CombatFx.Flat(aimPoint - transform.position);
            if (forward.sqrMagnitude > .12f) transform.rotation = Quaternion.LookRotation(forward.normalized);
        }

        private float Damage(float multiplier)
        {
            return stats.Damage * multiplier * (Random.value < stats.CritChance ? 1.65f : 1f);
        }

        private void BasicAttack()
        {
            GameAudio.Play(SoundCue.Attack);
            attackAnimation = 1f;
            Color color = GameBalance.ClassColor(HeroClass);
            if (HeroClass == HeroClass.Vanguard)
            {
                attackCooldown = .46f;
                CombatFx.Slash(transform.position,transform.forward,2.3f,color);
                if (Melee(2.8f,110f,Damage(1f),.3f,.12f)) OnBasicAttackHit(transform.position + transform.forward * 1.8f);
            }
            else
            {
                bool ranger = HeroClass == HeroClass.Ranger;
                attackCooldown = ranger ? .34f : .52f;
                CombatProjectile.Friendly(this,session,transform.position + transform.forward * .7f,transform.forward,Damage((ranger ? .78f : 1.15f)*(mobilityTime>0?1f+.12f*mobilityRank:1f)),color,false,ranger,true);
            }
            if (mobilityTime > 0) attackCooldown *= .8f;
        }

        private bool Melee(float range, float arc, float damage, float knockback, float stun)
        {
            bool hit = false;
            for (int i = session.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position-transform.position);
                if (delta.magnitude <= range + (enemy.IsBoss ? .5f : 0) && (delta.sqrMagnitude < .36f || Vector3.Angle(transform.forward,delta) <= arc*.5f))
                {
                    hit = true;
                    enemy.TakeDamage(damage,delta.normalized,knockback,stun);
                }
            }
            return hit;
        }

        internal void OnBasicAttackHit(Vector3 position)
        {
            if (IsDead) return;
            skillRuntime.RestoreEnergy(8f);
        }

        internal void RestoreSkillEnergy(float amount) { skillRuntime.RestoreEnergy(amount); }

        internal void HealingProtection(int rank)
        {
            if (rank < 2) return;
            healingProtectionTime = 5.2f;
            healingReduction = rank==3?.25f:.18f;
        }

        internal void MobilityBuff(int rank) { mobilityTime=4f+rank; mobilityRank=rank; }

        internal void ControlArea(Vector3 at,float radius,float duration)
        {
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy!=null && !enemy.IsDead && CombatFx.Flat(enemy.transform.position-at).magnitude<radius)
                    enemy.ApplyControl(duration);
            }
        }

        private void TryDefensePassive()
        {
            int rank=session.Progression.Profile.skillRanks[8];
            if(rank<=0 || passiveCooldown>0 || (HeroClass==HeroClass.Vanguard && Health>MaxHealth*.35f)) return;
            passiveCooldown=rank==3?30f:rank==2?38f:45f;
            passiveTime=2f+rank;
            passiveReduction=0;
            passiveSpeed=0;
            float radius=3f*GameBalance.SkillRangeMultiplier(rank);
            Color tint=GameBalance.ClassColor(HeroClass);
            if(HeroClass==HeroClass.Vanguard)
            {
                passiveReduction=.2f+rank*.1f;
                if(rank==3) HitArea(transform.position,radius,stats.Damage*1.6f,.9f,.65f);
            }
            else if(HeroClass==HeroClass.Arcanist)
            {
                passiveReduction=.3f+rank*.1f;
                skillRuntime.RestoreEnergy(2f+rank*2f);
                if(rank==3) ControlArea(transform.position,radius,1.5f);
            }
            else
            {
                invulnerability=Mathf.Max(invulnerability,.1f+rank*.15f);
                passiveSpeed=.1f+rank*.05f;
                if(rank==3) skillRuntime.RestoreEnergy(8f);
            }
            AdvancedSkillVfx.Rune(this,transform.position,radius,tint,passiveTime,rank,true);
            session.SpawnFloatingText(transform.position+Vector3.up*2.7f,GameBalance.SkillName(HeroClass,8),tint);
        }

        internal void HitArea(Vector3 at, float radius, float damage, float knockback = 0, float stun = 0)
        {
            for (int i = session.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position - at);
                if (delta.magnitude <= radius + (enemy.IsBoss ? .85f : .4f)) enemy.TakeDamage(damage,delta.normalized,knockback,stun);
            }
        }

        internal void SkillDash(Vector3 direction, float distance, float protection)
        {
            Vector3 flat = CombatFx.Flat(direction).normalized;
            Vector3 previous = transform.position;
            transform.position = Vector3.ClampMagnitude(previous + flat * distance,session.ArenaRadius-.65f);
            invulnerability = Mathf.Max(invulnerability,protection);
            AdvancedSkillVfx.Beam(this,previous+Vector3.up,transform.position+Vector3.up,GameBalance.ClassColor(HeroClass),.55f,.35f);
        }

        private void CastSkill(int slot)
        {
            if (slot < 0 || slot >= GameBalance.SkillCount || GameBalance.IsPassive(slot)) return;
            int rank = session.Progression.Profile.skillRanks[slot];
            if (rank <= 0)
            {
                if (skillFeedbackCooldown <= 0)
                {
                    session.Notify("按 K 打开技能面板，升级后消耗技能点学习技能。");
                    skillFeedbackCooldown = 2f;
                }
                return;
            }
            if (!skillRuntime.TryConsume(slot, rank))
            {
                if (skillFeedbackCooldown <= 0)
                {
                    float remaining = skillRuntime.Remaining(slot);
                    session.Notify(remaining > 0 ? GameBalance.SkillName(HeroClass,slot) + " 冷却中（" + remaining.ToString("0.0") + " 秒）" : "能量不足：需要 " + GameBalance.SkillEnergyCosts[slot] + " 点；普攻命中回复 8 点，持续回复每秒 4 点。");
                    skillFeedbackCooldown = .8f;
                }
                return;
            }
            GameAudio.Play(SoundCue.Cast);
            attackAnimation = 1;
            float power = 1f + (rank-1)*.3f;
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass);
            Vector3 target = transform.position + Vector3.ClampMagnitude(CombatFx.Flat(aimPoint-transform.position),9f*range);
            target = Vector3.ClampMagnitude(target,session.ArenaRadius);
            if (slot >= 3)
            {
                if (HeroClass == HeroClass.Vanguard && slot == 4)
                {
                    guardTime = 6f+(rank-1)*2f; guardPower = 1.2f * power;
                    guardReduction=.55f+rank*.05f; guardRadius=3.2f*range; guardRank=rank;
                    AdvancedSkillVfx.Rune(this,transform.position,2.1f*range,new Color(1f,.84f,.4f),guardTime,rank,true);
                }
                else if (HeroClass == HeroClass.Arcanist && slot == 5)
                {
                    guardTime=6f+(rank-1)*2f; guardRank=rank; guardReduction=.25f+rank*.1f;
                    guardRadius=2.8f*range; guardPulseTimer=0;
                    AdvancedSkillVfx.Rune(this,transform.position,guardRadius,new Color(.55f,.92f,1f),guardTime,rank+1,true);
                }
                else AdvancedSkillSequence.Spawn(this,session,slot,rank,target,transform.forward,stats.Damage * power,color);
                return;
            }
            if(rank>=2) AdvancedSkillVfx.Rune(this,slot==0?transform.position:target,3.1f*range,color,.8f,rank);
            if (HeroClass == HeroClass.Vanguard)
            {
                if (slot == 0)
                {
                    CombatFx.Ring(transform.position,3.4f*range,color,.45f,.2f);
                    Melee(3.4f*range,360,Damage(1.9f*power),.75f,.3f);
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position,3.4f*range,Damage(.85f*power),.25f,.18f,rank==3?.22f:0,.22f,color,true,false,rank==3?5f:0,rank==3?Damage(power):0);
                }
                else if (slot == 1)
                {
                    CombatFx.Slash(transform.position,transform.forward,4.8f*range,new Color(1f,.85f,.4f));
                    Melee(4.8f*range,90+(rank-1)*10,Damage(2.5f*power),1.9f,1.3f+(rank-1)*.3f);
                    CombatFx.Ring(transform.position+transform.forward*2.5f*range,2.1f*range,color,.4f,.16f);
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position+transform.forward*3f*range,2.3f*range,Damage(.9f*power),.6f,.25f,0,1,color,false,false,0,rank==3?Damage(1.3f*power):0);
                }
                else
                {
                    invulnerability = Mathf.Max(invulnerability,.5f);
                    CombatArea.Spawn(this,session,transform.position,4.1f*range,Damage(.95f*power),.14f,0,2.4f+(rank-1)*.6f,.45f,color,true,false,rank==3?2.5f:0,rank==3?Damage(2f*power):0);
                }
            }
            else if (HeroClass == HeroClass.Arcanist)
            {
                if (slot == 0)
                {
                    CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(1.5f*power),2f,0,0,1f,new Color(.51f,.92f,1f));
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(.75f*power),1.2f,.5f,0,1f,new Color(.51f,.92f,1f));
                    if(rank==3) for(int i=0;i<8;i++)
                    {
                        Vector3 shard=Quaternion.Euler(0,i*45f,0)*Vector3.forward;
                        CombatProjectile.Friendly(this,session,transform.position+shard*.5f,shard,Damage(.6f*power),new Color(.51f,.92f,1f),true,false,false,range,16f*range);
                    }
                }
                else if (slot == 1)
                {
                    CombatArea.Spawn(this,session,target,3f*range,Damage(3.5f*power),.7f,.7f,0,1f,new Color(1f,.59f,.28f),false,true);
                    if(rank>=2) CombatArea.Spawn(this,session,target,3f*range,Damage(1.3f*power),.3f,1.1f,0,1,new Color(1f,.59f,.28f),false,true);
                    if(rank==3) CombatArea.Spawn(this,session,target,3.2f*range,Damage(.55f*power),.1f,1.3f,2f,.5f,new Color(1f,.43f,.22f));
                }
                else CombatArea.Spawn(this,session,target,3.9f*range,Damage(.9f*power),.22f,.2f,4.5f,rank>=2?.5f:.6f,new Color(.65f,.5f,1f),false,false,rank==3?3.5f:0,rank==3?Damage(2f*power):0);
            }
            else
            {
                if (slot == 0)
                {
                    int arrows = 5+(rank-1)*2;
                    for (int i=0;i<arrows;i++)
                    {
                        Vector3 dir=Quaternion.Euler(0,Mathf.Lerp(-25,25,i/(float)(arrows-1)),0)*transform.forward;
                        CombatProjectile.Friendly(this,session,transform.position+dir*.6f,dir,Damage(1.15f*power),color,true,true,false,range,20f*range,null,rank==3?Damage(.4f*power):0,1.25f*range);
                    }
                }
                else if (slot == 1)
                {
                    CombatArea.Spawn(this,session,target,3f*range,Damage(1.7f*power),2.3f+(rank-1)*.4f,.4f,0,1f,color,false,false,rank==3?5f:0);
                    if(rank>=2) CombatArea.Spawn(this,session,target,3f*range,Damage(.75f*power),.5f,.8f,0,1,color);
                }
                else CombatArea.Spawn(this,session,target,4.3f*range,Damage(.7f*power),.08f,.3f,4f+(rank-1),.4f,new Color(.7f,1f,.59f),false,false,0,rank==3?Damage(2.5f*power):0);
            }
        }

        private void OnDestroy()
        {
            if (dodgeHalo != null) Destroy(dodgeHalo);
        }
    }
}
