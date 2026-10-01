using System.Collections.Generic;
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
        public float BlinkCooldown { get { return dodgeCooldown; } }
        public bool IsJumping { get { return jumping; } }
        public float JumpCooldown { get { return 0f; } }
        public bool TraversalStartedThisFrame { get { return traversalFrame == Time.frameCount; } }
        public float Energy { get { return skillRuntime != null ? skillRuntime.Energy : SkillRuntime.MaximumEnergy; } }
        public float MaxEnergy { get { return SkillRuntime.MaximumEnergy; } }
        public EnemyController AimTarget { get; private set; }
        public Vector3 AimPoint { get { return aimPoint; } }
        public float MovementMultiplier { get { return (slowTime > 0 ? Mathf.Max(.3f, 1f - slowStrength) : 1f) * (charge != null && charge.IsCharging ? .4f : 1f); } }
        internal int CombatEpoch { get; private set; }

        private GameSession session;
        private StatBlock stats;
        private CombatModel model;
        private SkillTargetingController targeting;
        private SkillChargeController charge;
        private bool executingChargedSkill;
        private readonly Dictionary<EnemyController,Renderer[]> aimGeometry = new Dictionary<EnemyController,Renderer[]>();
        private readonly List<EnemyController> staleAimGeometry = new List<EnemyController>();
        private SkillRuntime skillRuntime;
        private float attackCooldown, attackAnimation, hurtTimer, dodgeCooldown, invulnerability, skillFeedbackCooldown;
        private float guardTime, guardPower, guardReduction, guardRadius, guardPulseTimer;
        private int guardRank, mobilityRank;
        private float healingProtectionTime, healingReduction, mobilityTime;
        private float slowTime, slowStrength;
        private bool jumping;
        private float jumpAge, movementSkillLock;
        private Vector3 jumpOrigin;
        private int traversalFrame = -1;
        private float passiveCooldown, passiveTime, passiveReduction, passiveSpeed;
        private Vector3 aimPoint;

        public void Initialize(GameSession game, HeroClass heroClass)
        {
            // Re-initializing the same hero cannot reset resources or cooldowns.
            // A different class belongs to a new character and a new controller.
            if (skillRuntime == null) skillRuntime = new SkillRuntime(heroClass);
            else if (skillRuntime.HeroClass != heroClass)
                throw new System.InvalidOperationException("A player controller cannot change class during an adventure.");
            session = game;
            HeroClass = heroClass;
            gameObject.name = "Hero - " + GameBalance.ClassName(heroClass);
            if (model != null) Destroy(model.gameObject);
            model = CombatModel.Hero(transform, heroClass);
            targeting = GetComponent<SkillTargetingController>();
            if (targeting == null) targeting = gameObject.AddComponent<SkillTargetingController>();
            targeting.Initialize(this,session);
            charge = GetComponent<SkillChargeController>();
            if (charge == null) charge = gameObject.AddComponent<SkillChargeController>();
            charge.Initialize(this,session);
            RefreshStats(true);
            aimPoint = transform.position + Vector3.forward * 5;
        }

        public void RefreshStats(bool heal)
        {
            if (session == null) return;
            float previousMaximum = MaxHealth;
            bool wasDead = previousMaximum > 0 && Health <= 0;
            stats = session.Progression.GetStats();
            if (model != null) model.ApplyFashion(session.Progression.EquippedFashion(FashionSlot.Wings),
                session.Progression.EquippedFashion(FashionSlot.Weapon));
            if (model != null) model.ApplyEquipment(session.Progression.Equipped(ItemSlot.Weapon),
                session.Progression.Equipped(ItemSlot.Armor), session.Progression.Equipped(ItemSlot.Relic));
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
            if (targeting != null) targeting.Cancel();
            if (charge != null) charge.Cancel();
            AimTarget = null;
            aimGeometry.Clear();
            position = WorldTraversal.NearestWalkable(position, .45f);
            transform.position = position;
            jumping = false;
            jumpAge = movementSkillLock = 0;
            aimPoint = position+transform.forward*5f;
            guardTime = healingProtectionTime = mobilityTime = passiveTime = 0;
            slowTime = slowStrength = 0;
            attackAnimation = 0;
            attackCooldown = .15f;
            invulnerability = .65f;
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
                if (targeting != null) targeting.Cancel();
                if (charge != null) charge.Cancel();
                if (jumping) transform.position = WorldTraversal.NearestWalkable(transform.position, .45f);
                jumping = false;
                AimTarget = null;
                model.transform.localRotation = Quaternion.Euler(0,0,75f);
                session.OnPlayerDied();
            }
        }

        public float CooldownRemaining(int slot) { return SkillCooldownRemaining(HotbarSkill(slot)); }
        public float SkillCooldownRemaining(int skillIndex) { return skillRuntime != null ? skillRuntime.Remaining(skillIndex) : 0f; }

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
            if (charge != null && charge.IsCharging && (Input.GetKeyDown(KeyCode.Escape) || AdventureCamera.CancelSkillRequested)) charge.Cancel();
            if (session.InputBlocked && targeting != null) targeting.Cancel();
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            attackCooldown = Mathf.Max(0,attackCooldown - dt);
            attackAnimation = Mathf.Max(0,attackAnimation - dt * 4f);
            hurtTimer = Mathf.Max(0,hurtTimer - dt);
            dodgeCooldown = Mathf.Max(0,dodgeCooldown - dt);
            movementSkillLock = Mathf.Max(0, movementSkillLock - dt);
            invulnerability = Mathf.Max(0,invulnerability - dt);
            skillFeedbackCooldown = Mathf.Max(0,skillFeedbackCooldown - dt);
            if (IsDead) return;
            skillRuntime.Advance(dt);
            slowTime = Mathf.Max(0, slowTime - dt);
            if (slowTime <= 0) slowStrength = 0;
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
                model.Animate(0,attackAnimation,hurtTimer > 0);
                return;
            }
            bool mobile = MobileControls.Active;
            Vector2 moveInput = mobile ? MobileControls.Move : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 movement = mobile ? new Vector3(moveInput.x, 0, moveInput.y) :
                AdventureCamera.CameraRelativeMovement(moveInput, Camera.main == null ? null : Camera.main.transform);
            movement = Vector3.ClampMagnitude(movement,1);
            bool wantsJump = mobile ? MobileControls.ConsumeJump() : Input.GetKeyDown(KeyCode.Space);
            if (wantsJump) TryJump();
            bool wantsBlink = mobile ? MobileControls.ConsumeDodge() : Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            if (wantsBlink) TryBlink(movement);
            if (jumping)
            {
                AdvanceJump(dt);
            }
            else if (!TraversalStartedThisFrame)
            {
                float movementBonus = passiveTime>0?passiveSpeed:0;
                if (mobilityTime>0) movementBonus += .1f+mobilityRank*.05f;
                transform.position = WorldTraversal.Move(transform.position, movement * stats.MoveSpeed * (1f+movementBonus) * MovementMultiplier * dt, .45f);
            }
            Vector3 bounded = transform.position;
            float bound = Mathf.Max(1,session.ArenaRadius - .65f);
            float airborneHeight = jumping ? bounded.y : 0;
            bounded.y = 0;
            bounded = Vector3.ClampMagnitude(bounded,bound);
            bounded.y = airborneHeight;
            transform.position = bounded;
            // Mouse selection uses this frame's final position. Walking only turns
            // the model; it never overwrites the independent mouse aim point.
            if ((charge == null || !charge.IsCharging) && (mobile || !session.PointerOverUI))
                aimPoint = mobile ? ResolveMobileAim(movement) : ResolveAim(Camera.main,Input.mousePosition);
            bool wantsBasic = mobile ? MobileControls.AttackHeld : !session.PointerOverUI && (Input.GetMouseButton(0) || Input.GetKey(KeyCode.J));
            if (movement.sqrMagnitude > .01f && !wantsBasic && (charge == null || !charge.IsCharging))
                transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(movement),720f*dt);
            if (!TraversalStartedThisFrame && !mobile && !session.PointerOverUI)
            {
                GameProfile profile = session.Progression.Profile;
                for (int slot=0;slot<GameBalance.HotbarSize;slot++)
                    if (profile.hotbarKeys != null && slot < profile.hotbarKeys.Length && Input.GetKeyDown((KeyCode)profile.hotbarKeys[slot]))
                    {
                        int skill = HotbarSkill(slot);
                        if (skill == GameBalance.HotbarPotion) { session.UseHotbarConsumable(); break; }
                        if (skill >= 0 && skill < GameBalance.SkillCount && targeting != null) targeting.Begin(skill);
                    }
            }
            bool suppressBasic = targeting != null && targeting.TickInput();
            if (wantsBasic && !suppressBasic && (charge == null || (!charge.IsCharging && !charge.ConsumedThisFrame)))
            {
                FaceAim();
                if (attackCooldown <= 0) BasicAttack();
            }
            model.Animate(movement.magnitude,attackAnimation,hurtTimer > 0);
            if (charge != null && charge.IsCharging) model.AnimateCharge(charge.Progress);
        }

        // Kept independent of Input so runtime validation can project a known body
        // point, move the player and check that the firing direction is recalculated.
        private void ApplyAim(Vector2 screenPosition)
        {
            if (charge != null && charge.IsCharging) return;
            aimPoint = ResolveAim(Camera.main,screenPosition);
            FaceAim();
        }

        private Vector3 ResolveAim(Camera camera,Vector2 screenPosition)
        {
            AimTarget = null;
            if (camera == null || !camera.pixelRect.Contains(screenPosition)) return aimPoint;
            Vector3 freeAim = aimPoint;
            Ray ray = camera.ScreenPointToRay(screenPosition);
            float enter;
            if (new Plane(Vector3.up,Vector3.zero).Raycast(ray,out enter)) freeAim = ray.GetPoint(enter);
            if (session == null) return freeAim;
            float margin = Mathf.Clamp(camera.pixelHeight/90f,6f,12f);
            float bestScore = float.PositiveInfinity;
            for (int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (!ValidAimTarget(enemy) || CombatFx.Flat(enemy.transform.position-transform.position).sqrMagnitude>18f*18f) continue;
                Bounds bounds = EnemyAimBounds(enemy);
                Rect projected;
                if (!ProjectedBounds(camera,bounds,out projected)) continue;
                bool direct = projected.Contains(screenPosition);
                Rect assisted = Rect.MinMaxRect(projected.xMin-margin,projected.yMin-margin,projected.xMax+margin,projected.yMax+margin);
                if (!assisted.Contains(screenPosition)) continue;
                Vector3 bodyScreen = camera.WorldToScreenPoint(EnemyBodyPoint(enemy));
                float bodyDistance = Vector2.Distance(screenPosition,new Vector2(bodyScreen.x,bodyScreen.y));
                float score = (direct?0f:10000f)+bodyDistance+bodyScreen.z*.02f;
                if (score<bestScore) { bestScore=score; AimTarget=enemy; }
            }
            PruneAimGeometry();
            return AimTarget != null ? CombatFx.Flat(AimTarget.transform.position) : freeAim;
        }

        private Vector3 ResolveMobileAim(Vector3 movement)
        {
            AimTarget = null;
            Vector3 direction = movement.sqrMagnitude > .01f ? movement.normalized : transform.forward;
            float nearest = 14f;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (!ValidAimTarget(enemy)) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position - transform.position);
                float distance = delta.magnitude;
                if (distance < nearest && Vector3.Angle(direction, delta) <= 75f)
                { nearest = distance; AimTarget = enemy; }
            }
            return AimTarget != null ? CombatFx.Flat(AimTarget.transform.position) : transform.position + direction * 8f;
        }

        private static bool ProjectedBounds(Camera camera,Bounds bounds,out Rect screenBounds)
        {
            Vector3 min=bounds.min,max=bounds.max;
            float left=float.PositiveInfinity,bottom=float.PositiveInfinity,right=float.NegativeInfinity,top=float.NegativeInfinity;
            for(int corner=0;corner<8;corner++)
            {
                Vector3 screen=camera.WorldToScreenPoint(new Vector3((corner&1)==0?min.x:max.x,(corner&2)==0?min.y:max.y,(corner&4)==0?min.z:max.z));
                if(screen.z<=camera.nearClipPlane) { screenBounds=new Rect(); return false; }
                left=Mathf.Min(left,screen.x); right=Mathf.Max(right,screen.x);
                bottom=Mathf.Min(bottom,screen.y); top=Mathf.Max(top,screen.y);
            }
            screenBounds=Rect.MinMaxRect(left,bottom,right,top);
            return camera.pixelRect.Overlaps(screenBounds);
        }

        private Bounds EnemyAimBounds(EnemyController enemy)
        {
            Renderer[] renderers;
            if(!aimGeometry.TryGetValue(enemy,out renderers))
            {
                CombatModel enemyModel=enemy.GetComponentInChildren<CombatModel>();
                renderers=enemyModel!=null?enemyModel.GetComponentsInChildren<Renderer>():new Renderer[0];
                aimGeometry.Add(enemy,renderers);
            }
            Bounds bounds=new Bounds(enemy.transform.position+Vector3.up,Vector3.one);
            bool found=false;
            for(int i=0;i<renderers.Length;i++)
                if(renderers[i]!=null && renderers[i].enabled)
                {
                    if(!found) { bounds=renderers[i].bounds; found=true; }
                    else bounds.Encapsulate(renderers[i].bounds);
                }
            return bounds;
        }

        internal Vector3 EnemyBodyPoint(EnemyController enemy)
        {
            if(enemy==null) return aimPoint+Vector3.up;
            Bounds bounds=EnemyAimBounds(enemy);
            Vector3 at=enemy.transform.position;
            at.y=Mathf.Clamp(bounds.center.y,at.y+.35f,at.y+3f);
            return at;
        }

        private static bool ValidAimTarget(EnemyController enemy)
        {
            return enemy!=null && !enemy.IsDead && enemy.gameObject.activeInHierarchy;
        }

        private void PruneAimGeometry()
        {
            if(aimGeometry.Count<=session.Enemies.Count+4) return;
            staleAimGeometry.Clear();
            foreach(KeyValuePair<EnemyController,Renderer[]> pair in aimGeometry)
                if(!ValidAimTarget(pair.Key)) staleAimGeometry.Add(pair.Key);
            for(int i=0;i<staleAimGeometry.Count;i++) aimGeometry.Remove(staleAimGeometry[i]);
        }

        private void FaceAim()
        {
            if(!ValidAimTarget(AimTarget)) AimTarget=null;
            if(AimTarget!=null) aimPoint=CombatFx.Flat(AimTarget.transform.position);
            Vector3 forward=CombatFx.Flat(aimPoint-transform.position);
            if(forward.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(forward.normalized);
        }

        private EnemyController MagicConeTarget()
        {
            Vector3 direction=CombatFx.Flat(aimPoint-transform.position).normalized;
            if(direction.sqrMagnitude<.01f) direction=transform.forward;
            EnemyController selected=null;
            float nearest=14f;
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(!ValidAimTarget(enemy)) continue;
                Vector3 delta=CombatFx.Flat(enemy.transform.position-transform.position);
                float distance=delta.magnitude;
                if(distance<=nearest && Vector3.Angle(direction,delta)<=35f) { selected=enemy; nearest=distance; }
            }
            return selected;
        }

        private float Damage(float multiplier)
        {
            return stats.Damage * multiplier * (Random.value < stats.CritChance ? 1.65f : 1f);
        }

        private void BasicAttack()
        {
            if (charge != null && (charge.IsCharging || charge.ConsumedThisFrame)) return;
            if ((HeroClass==HeroClass.Arcanist || HeroClass==HeroClass.Summoner) && !ValidAimTarget(AimTarget)) AimTarget=MagicConeTarget();
            FaceAim();
            GameAudio.Play(SoundCue.Attack);
            model.PlayAction(-1,true);
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
                Vector3 target=ValidAimTarget(AimTarget)?EnemyBodyPoint(AimTarget):new Vector3(aimPoint.x,1.15f,aimPoint.z);
                float distance=CombatFx.Flat(target-transform.position).magnitude;
                Vector3 muzzle=transform.position+Vector3.up*1.15f+transform.forward*Mathf.Min(.55f,distance*.3f);
                CombatProjectile.BasicShot(this,session,muzzle,target,Damage((ranger ? .78f : 1.15f)*(mobilityTime>0?1f+.12f*mobilityRank:1f)),color,ranger,AimTarget);
            }
            if (mobilityTime > 0) attackCooldown *= .8f;
        }

        private bool Melee(float range, float arc, float damage, float knockback, float stun, float knockdown = 0)
        {
            bool hit = false;
            for (int i = session.Enemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = CombatFx.Flat(enemy.transform.position-transform.position);
                if (delta.magnitude <= range + (enemy.IsBoss ? .5f : 0) &&
                    (delta.sqrMagnitude < .36f || Vector3.Angle(transform.forward,delta) <= arc*.5f) &&
                    WorldTraversal.HasGroundPath(transform.position, enemy.transform.position, .15f))
                {
                    hit = true;
                    enemy.TakeDamage(damage,delta.normalized,knockback,stun);
                    if (knockdown > 0 && enemy.StatusEffects != null) enemy.StatusEffects.Knockdown(knockdown);
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
            else if(HeroClass==HeroClass.Arcanist || HeroClass==HeroClass.Summoner)
            {
                passiveReduction=(HeroClass==HeroClass.Summoner?.25f:.3f)+rank*.1f;
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
            if (jumping) return;
            Vector3 flat = CombatFx.Flat(direction).normalized;
            Vector3 previous = transform.position;
            Vector3 destination = Vector3.ClampMagnitude(CombatFx.Flat(previous) + flat * distance,session.ArenaRadius-.65f);
            if (!WorldTraversal.CanLeap(previous, destination, .45f)) { TraversalFailure(); return; }
            transform.position = destination;
            invulnerability = Mathf.Max(invulnerability,protection);
            AdvancedSkillVfx.Beam(this,previous+Vector3.up,transform.position+Vector3.up,GameBalance.ClassColor(HeroClass),.55f,.35f);
        }

        internal bool TryJump()
        {
            if (session == null || IsDead || !session.HasStarted || session.InputBlocked || jumping || TraversalStartedThisFrame || movementSkillLock > 0 || (charge != null && charge.IsCharging)) return false;
            Vector3 origin = CombatFx.Flat(transform.position);
            if (!WorldTraversal.IsWalkable(origin, .45f)) return false;
            jumpOrigin = origin;
            jumpAge = 0;
            jumping = true;
            traversalFrame = Time.frameCount;
            GameAudio.Play(SoundCue.Dodge);
            return true;
        }

        internal bool TryBlink(Vector3 direction)
        {
            if (session == null || IsDead || !session.HasStarted || session.InputBlocked || jumping || dodgeCooldown > 0 || TraversalStartedThisFrame || movementSkillLock > 0) return false;
            Vector3 forward = CombatFx.Flat(direction);
            if (forward.sqrMagnitude < .01f) forward = transform.forward;
            Vector3 origin = CombatFx.Flat(transform.position);
            Vector3 destination = origin + forward.normalized * 4.8f;
            float bound = session.ArenaRadius - .65f;
            if (destination.sqrMagnitude > bound * bound || !WorldTraversal.CanLeap(origin, destination, .45f)) { TraversalFailure(); return false; }
            if (charge != null) charge.Cancel();
            transform.position = destination;
            dodgeCooldown = 2.1f;
            invulnerability = Mathf.Max(invulnerability, .38f);
            traversalFrame = Time.frameCount;
            Color color = GameBalance.ClassColor(HeroClass);
            AdvancedSkillVfx.Rune(this, origin, .9f, color, .3f, 1);
            AdvancedSkillVfx.Rune(this, destination, 1.1f, color, .38f, 1);
            AdvancedSkillVfx.Beam(this, origin + Vector3.up, destination + Vector3.up, color, .28f, .14f);
            GameAudio.Play(SoundCue.Dodge);
            return true;
        }

        private void AdvanceJump(float deltaTime)
        {
            if (!jumping || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            jumpAge += deltaTime;
            float progress = Mathf.Clamp01(jumpAge / .55f);
            transform.position = jumpOrigin + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 1.65f);
            if (progress >= 1f)
            {
                jumping = false;
                transform.position = jumpOrigin;
            }
        }

        private void TraversalFailure()
        {
            if (skillFeedbackCooldown > 0) return;
            session.Notify("前方有障碍或没有安全落点，请走桥或调整方向。");
            skillFeedbackCooldown = .8f;
        }

        private bool CanUseMovementSkill(int skill, int rank)
        {
            bool forwardDash = HeroClass == HeroClass.Vanguard && skill == 5;
            bool retreat = HeroClass == HeroClass.Ranger && skill == 4;
            if (!forwardDash && !retreat) return true;
            Vector3 direction = CombatFx.Flat((ValidAimTarget(AimTarget) ? AimTarget.transform.position : aimPoint) - transform.position);
            if (direction.sqrMagnitude < .0001f) direction = transform.forward;
            direction.Normalize();
            if (retreat) direction = -direction;
            float distance = (forwardDash ? 7f : 5f) * GameBalance.SkillRangeMultiplier(rank);
            Vector3 end = Vector3.ClampMagnitude(CombatFx.Flat(transform.position) + direction * distance, session.ArenaRadius - .65f);
            return WorldTraversal.CanLeap(transform.position, end, .45f);
        }

        internal bool CanBeginSkillTargeting(int skill)
        {
            if(session==null || IsDead || jumping || !session.HasStarted || session.InputBlocked || skill<0 || skill>=GameBalance.SkillCount || GameBalance.IsPassive(skill)) return false;
            if(charge != null && (charge.IsCharging || charge.ConsumedThisFrame)) return false;
            int rank=session.Progression.Profile.skillRanks[skill];
            string failure=null;
            if(rank<=0) failure="按 K 学习这个技能后再施放。";
            else if(skillRuntime.Remaining(skill)>0) failure=GameBalance.SkillName(HeroClass,skill)+" 冷却中（"+skillRuntime.Remaining(skill).ToString("0.0")+" 秒）";
            else if(Energy<GameBalance.SkillEnergyCost(HeroClass,skill)) failure="能量不足：普攻命中回复 8 点，持续回复每秒 4 点。";
            else if(!CanUseMovementSkill(skill,rank)) failure="前方有障碍或没有安全落点，请走桥或调整方向。";
            if(failure==null) return true;
            if(skillFeedbackCooldown<=0) { session.Notify(failure); skillFeedbackCooldown=.8f; }
            return false;
        }

        internal bool CastImmediateSkill(int skill)
        {
            if(SkillTargetingController.RequiresConfirmation(HeroClass,skill) || !CanBeginSkillTargeting(skill)) return false;
            // Mouse aim is resolved independently of movement. Re-read a selected
            // living target's position here so immediate directional casts face it.
            FaceAim();
            if (SkillChargeController.Duration(HeroClass, skill) > 0) return charge.Begin(skill);
            CastSkill(skill);
            return true;
        }

        internal bool ConfirmTargetedSkill(int skill,Vector3 worldPoint)
        {
            if(!SkillTargetingController.RequiresConfirmation(HeroClass,skill) || !CanBeginSkillTargeting(skill)) return false;
            AimTarget=null;
            aimPoint=CombatFx.Flat(worldPoint);
            FaceAim();
            if (SkillChargeController.Duration(HeroClass, skill) > 0) return charge.Begin(skill);
            CastSkill(skill);
            return true;
        }

        internal bool ExecuteChargedSkill(int skill)
        {
            if (charge == null || !CanBeginSkillTargeting(skill)) return false;
            AimTarget = null;
            aimPoint = charge.TargetPoint;
            transform.rotation = Quaternion.LookRotation(charge.Direction);
            executingChargedSkill = true;
            try { CastSkill(skill); }
            finally { executingChargedSkill = false; }
            return true;
        }

        public void ApplySlow(float duration, float strength)
        {
            if (IsDead || duration <= 0 || strength <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || float.IsNaN(strength) || float.IsInfinity(strength)) return;
            slowTime = Mathf.Max(slowTime, duration);
            slowStrength = Mathf.Max(slowStrength, Mathf.Clamp(strength, 0, .7f));
            CombatFx.Ring(transform.position, .9f, new Color(.42f, .85f, .3f), .3f, .08f);
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
            if (!CanUseMovementSkill(slot, rank)) { TraversalFailure(); return; }
            if (!skillRuntime.TryConsume(slot, rank))
            {
                if (skillFeedbackCooldown <= 0)
                {
                    float remaining = skillRuntime.Remaining(slot);
                    session.Notify(remaining > 0 ? GameBalance.SkillName(HeroClass,slot) + " 冷却中（" + remaining.ToString("0.0") + " 秒）" : "能量不足：需要 " + GameBalance.SkillEnergyCost(HeroClass,slot) + " 点；普攻命中回复 8 点，持续回复每秒 4 点。");
                    skillFeedbackCooldown = .8f;
                }
                return;
            }
            GameAudio.Play(SoundCue.Cast);
            if ((HeroClass == HeroClass.Vanguard && slot == 5) || (HeroClass == HeroClass.Ranger && slot == 4)) movementSkillLock = .15f;
            if (executingChargedSkill) model.ReleaseCharge(slot);
            else model.PlayAction(slot,false);
            attackAnimation = 1;
            float power = 1f + (rank-1)*.3f;
            float range = GameBalance.SkillRangeMultiplier(rank);
            Color color = GameBalance.ClassColor(HeroClass);
            Vector3 target = executingChargedSkill ? charge.TargetPoint : transform.position + Vector3.ClampMagnitude(CombatFx.Flat(aimPoint-transform.position),9f*range);
            target = Vector3.ClampMagnitude(target,session.ArenaRadius);
            if (HeroClass == HeroClass.Summoner)
            {
                if (slot == 5)
                {
                    guardTime = 6f + (rank - 1) * 2f;
                    guardRank = rank; guardReduction = .25f + rank * .1f;
                    AdvancedSkillVfx.Rune(this, transform.position, 2.8f * range, color, guardTime, rank + 1, true);
                }
                else SummonerSpell.Cast(this, session, slot, rank, target, stats.Damage * power);
                return;
            }
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
                    Melee(4.8f*range,90+(rank-1)*10,Damage(2.5f*power),1.9f,1.3f+(rank-1)*.3f,1.3f+(rank-1)*.3f);
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
                    CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(1.5f*power),2f,0,0,1f,new Color(.51f,.92f,1f),statusSkill:0,statusRank:rank);
                    if(rank>=2) CombatArea.Spawn(this,session,transform.position,3.7f*range,Damage(.75f*power),1.2f,.5f,0,1f,new Color(.51f,.92f,1f),statusSkill:0,statusRank:rank);
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

    }
}
