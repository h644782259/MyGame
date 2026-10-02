using System;using UnityEngine;namespace Emberfall{public partial class PlayerController{public void TakeDamageFrom(float amount, string source)
        {
            if (session == null || IsDead || session.CombatEnded || invulnerability > 0 || !session.HasStarted || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float damage = Mathf.Max(1, amount * CombatBalance.ArmorDamageMultiplier(stats.Armor, session.Progression.Profile.level));
            if (chargedWardTime > 0) damage *= .75f;
            if(coreWardTime>0)damage*=1f-masteryCore.WardReduction;
            if (guardTime > 0)
            {
                damage *= 1f-guardReduction;
                if (HeroClass == HeroClass.Vanguard)
                {
                    AdvancedSkillVfx.Rune(this, transform.position, guardRadius, new Color(1f,.84f,.4f), .55f, guardRank);
                    HitArea(transform.position, guardRadius, CombatAttack * guardPower, .5f, .2f,guardCastId);
                }
            }
            // A guard counter can finish a mode reentrantly. Paid victory wins that
            // tie; do not apply the already-in-flight hostile hit after terminal state.
            if(session.CombatEnded)return;
            if (healingProtectionTime > 0) damage *= 1f-healingReduction;
            if (passiveTime > 0) damage *= 1f-passiveReduction;
            if (ActiveRunBonuses != null) damage *= ActiveRunBonuses.IncomingDamageMultiplier(Health / Mathf.Max(1f, MaxHealth));
            damage = Mathf.Max(damage, amount * CombatBalance.MinimumCombinedDamageMultiplier);
            if (session.HasBlessing(RunBlessing.RiskContract)) damage *= 1.15f;
            session.RecordIncomingDamage(source, Mathf.Min(Health, damage));
            float healthBeforeHit = Health;
            Health = Mathf.Max(0,Health - damage);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("takendamage",CombatReviewObjectId.Get(this),amount:healthBeforeHit-Health,detail:source);
            if(Health>0){float recovery=masteryCore.DamageTaken(Health/MaxHealth);if(recovery>0){Heal(MaxHealth*recovery);session.RecordCombatAction("生机核心");}}
            GameAudio.Play(SoundCue.Hit);
            invulnerability = .2f;
            hurtTimer = .22f;
            session.SpawnFloatingText(transform.position + Vector3.up * 2.5f,"−" + Mathf.CeilToInt(damage),new Color(1f,.4f,.42f));
            CombatFx.Ring(transform.position,.85f,new Color(1f,.27f,.3f),.2f);
            if (Health > 0) TryDefensePassive();
            if (Health <= 0)
            {
                if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("death",CombatReviewObjectId.Get(this));
                CombatEpoch++;
            ClearMobilePinnedTarget();
                perfectDodgeCounterTime = 0;
                CancelCombatPose();
            masteryCore.Reset();coreWardTime=0;
                if (targeting != null) targeting.Cancel();
                if (charge != null) charge.Cancel();
                if (jumping) transform.position = WorldTraversal.NearestWalkable(transform.position, .45f);
                jumping = false;
                AimTarget = null;
                focusedEnemy = null; focusTime = blinkBufferTime = 0;
                model.SetBlenderPilotOwnerAlive(false); // Restore procedural visuals before the final death pose.
                model.transform.localRotation = Quaternion.Euler(0,0,75f);
                session.OnPlayerDied();
            }
        }internal void CancelCombatPose() { skillBasicRecovery.Clear(); if (model != null) model.CancelAction(); }public void RefreshStats(bool heal)
        {
            if (session == null) return;
            float previousMaximum = MaxHealth;
            bool wasDead = previousMaximum > 0 && Health <= 0;
            stats = session.Progression.GetStats();
            int selectedCore=session.Progression.Profile.masteryCore;
            int oldCore=masteryCore.Core,oldTier=masteryCore.Tier;
            masteryCore.Configure(selectedCore,selectedCore>=0&&selectedCore<4?session.Progression.Profile.masteryRanks[selectedCore]:0);
            if(oldCore!=masteryCore.Core||oldTier!=masteryCore.Tier)coreWardTime=0;
            if (model != null) model.ApplyFashion(session.Progression.EquippedFashion(FashionSlot.Wings),
                session.Progression.EquippedFashion(FashionSlot.Weapon));
            if (model != null) model.ApplyEquipment(session.Progression.Equipped(ItemSlot.Weapon),
                session.Progression.Equipped(ItemSlot.Armor), session.Progression.Equipped(ItemSlot.Relic));
            MaxHealth = Mathf.Max(1f, stats.MaxHealth);
            if (heal) Health = MaxHealth;
            else if (!wasDead) Health = Mathf.Clamp(Health, 1, MaxHealth);
            if(model!=null)model.SetBlenderPilotOwnerAlive(!IsDead);
            SummonedCompanion.RefreshBuild(this);
        }private void FaceAim()
        {
            if(!ValidAimTarget(AimTarget)) AimTarget=null;
            if(AimTarget!=null) aimPoint=CombatFx.Flat(AimTarget.transform.position);
            Vector3 forward=CombatFx.Flat(aimPoint-transform.position);
            if(forward.sqrMagnitude>.0001f) transform.rotation=Quaternion.LookRotation(forward.normalized);
        }private void BasicAttack()
        {
            if (TraversalStartedThisFrame || skillBasicRecovery.Blocked) return;
            if(!MobilePinnedActionAllowed(-1,true))return;
            if (charge != null && (charge.IsCharging || charge.ConsumedThisFrame)) return;
            if ((HeroClass==HeroClass.Arcanist || HeroClass==HeroClass.Summoner) && !ValidAimTarget(AimTarget)) AimTarget=MagicConeTarget();
            FaceAim();
            GameAudio.Play(SoundCue.Attack);
            if (CombatReviewEvents.Enabled) CombatReviewEvents.Emit("attack",CombatReviewObjectId.Get(this));
            attackCooldown = SkillDamageBudgets.BasicInterval(HeroClass);
            if (mobilityTime > 0) attackCooldown *= .8f;
            if (ActiveRunBonuses != null) attackCooldown /= ActiveRunBonuses.AttackSpeedMultiplier;
            attackCooldown = Mathf.Max(.18f, attackCooldown);
            model.PlayAction(-1,true,attackCooldown);
            attackAnimation = 1f;
}public void FacingFrame(Vector3 movement,float dt){bool mobile=false;float movementBonus=0;Vector3 walkingDisplacement;                Vector3 walkingStart = transform.position;
                transform.position = WorldTraversal.Move(transform.position, movement * stats.MoveSpeed * (1f+movementBonus) * MovementMultiplier * dt, .45f);
                walkingDisplacement = CombatFx.Flat(transform.position - walkingStart);            Vector3 beforeBoundary = transform.position;
            Vector3 bounded = transform.position;
            float bound = Mathf.Max(1,session.ArenaRadius - .65f);
            float airborneHeight = jumping ? bounded.y : 0;
            bounded.y = 0;
            bounded = Vector3.ClampMagnitude(bounded,bound);
            bounded.y = airborneHeight;
            transform.position = bounded;
            if (walkingDisplacement.sqrMagnitude > 0) walkingDisplacement += CombatFx.Flat(bounded-beforeBoundary);
            // Commit actual walking before any attack samples its final weapon pose.
            model.SetLocomotion(transform.InverseTransformDirection(walkingDisplacement),dt,stats.MoveSpeed,!TraversalStartedThisFrame,jumping,jumpAge/.55f);
            // Mouse selection uses this frame's final position. Walking only turns
            // the model; it never overwrites the independent mouse aim point.
            if ((charge == null || !charge.IsCharging) && (mobile || !session.PointerOverUI))
                aimPoint = mobile ? ResolveMobileAim(movement) : ResolveAim(Camera.main,Input.mousePosition);
            bool attackHeld = mobile ? MobileControls.AttackHeld : Input.GetMouseButton(0) || Input.GetKey(KeyCode.J);
            if (!attackHeld) suppressBasicUntilReleased = false;
            bool wantsBasic = !suppressBasicUntilReleased && attackHeld && (mobile || !session.PointerOverUI);
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
            if (!TraversalStartedThisFrame && wantsBasic && !suppressBasic && (charge == null || (!charge.IsCharging && !charge.ConsumedThisFrame)))
            {
                FaceAim();
                if (attackCooldown <= 0) BasicAttack();
            }
            model.Animate(movement.magnitude,attackAnimation,hurtTimer > 0);}}}