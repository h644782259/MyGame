using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed class SummonedCompanion : MonoBehaviour
    {
        public enum Kind { Wolf, Spirit, Treant }
        private static readonly List<SummonedCompanion> active = new List<SummonedCompanion>();
        private sealed class BondState
        {
            public int Epoch;
            public readonly CompanionDirective<EnemyController> Directive = new CompanionDirective<EnemyController>();
            public readonly CompanionCommandOpportunity Commands = new CompanionCommandOpportunity();
            public readonly CompanionCooperationTracker<EnemyController> Cooperation = new CompanionCooperationTracker<EnemyController>();
        }
        private static readonly Dictionary<PlayerController, BondState> bonds = new Dictionary<PlayerController, BondState>();
        private static readonly List<PlayerController> staleOwners = new List<PlayerController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            active.Clear(); bonds.Clear(); staleOwners.Clear();
        }

        internal static void RetireOwner(PlayerController owner)
        {
            // Unity's destroyed-object equality must not hide a dictionary key
            // while its owner's OnDestroy is releasing that last reference.
            if (object.ReferenceEquals(owner, null)) return;
            bonds.Remove(owner);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                SummonedCompanion pet = active[i];
                if (pet == null) active.RemoveAt(i);
                else if (object.ReferenceEquals(pet.Owner, owner)) pet.Dismiss();
            }
        }
        public PlayerController Owner { get; private set; }
        public Kind Form { get; private set; }
        public bool IsStarter { get; private set; }
        public bool IsPermanent { get; private set; }
        public bool IsRecalling { get { return recallTime > 0; } }
        public int Rank { get { return rank; } }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float RemainingLifetime { get; private set; }
        public bool IsAlive { get { return Health > 0 && Owner != null && !Owner.IsDead && Owner.gameObject.activeInHierarchy && gameObject.activeInHierarchy && session != null && session.HasStarted && session.Player == Owner && Owner.CombatEpoch == epoch; } }
        private GameSession session;
        private int epoch, rank;
        private float damage, cooldown, attackPose, baseMaxHealth, statRefresh;
        private float recallTime, commandTime, commandMultiplier = 1, idleTime;
        private bool commandEmpowered;
        private EnemyController commandedTarget;
        private Vector3 commandedPoint;
        private bool hasCommandPoint;
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

        private static BondState State(PlayerController owner)
        {
            staleOwners.Clear();
            foreach (var pair in bonds) if (pair.Key == null) staleOwners.Add(pair.Key);
            foreach (PlayerController key in staleOwners) bonds.Remove(key);
            staleOwners.Clear();
            BondState state;
            if (!bonds.TryGetValue(owner, out state) || state.Epoch != owner.CombatEpoch)
            {
                state = new BondState { Epoch = owner.CombatEpoch };
                bonds[owner] = state;
            }
            return state;
        }

        public static SummonedCompanion[] Snapshot(PlayerController owner)
        {
            return active.FindAll(pet => pet != null && pet.IsAlive && pet.Owner == owner).ToArray();
        }

        private static SummonedCompanion EvictionCandidate(PlayerController owner, Kind incoming)
        {
            return active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && !pet.IsPermanent && !pet.IsStarter && pet.Form == incoming)
                ?? active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && !pet.IsPermanent && !pet.IsStarter && pet.Form != Kind.Treant)
                ?? active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && !pet.IsStarter && pet.Form != Kind.Treant);
        }

        // Low-level spawn remains available for timed pack members. Permanent
        // contracts use CastContract so living partners are never replaced.
        public static SummonedCompanion Summon(PlayerController owner, GameSession game, Kind form, int rank, Vector3 at, float strength,
            bool permanent = false, bool foundation = false)
        {
            if (owner == null || owner.IsDead || game == null) return null;
            if (foundation)
            {
                SummonedCompanion existing = active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && pet.IsStarter);
                if (existing != null) return existing;
            }
            int cap = form == Kind.Treant ? 1 : CompanionRules.NormalCapacity(owner.HasMechanic(EquipmentMechanic.TwinSummonResonance));
            while (Count(owner, form == Kind.Treant) >= cap)
            {
                SummonedCompanion oldest = form == Kind.Treant
                    ? active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && pet.Form == Kind.Treant)
                    : EvictionCandidate(owner, form);
                if (oldest == null) return null;
                oldest.Dismiss();
            }
            var obj = new GameObject(form == Kind.Wolf ? "灵狼" : form == Kind.Spirit ? "星灵" : "远古树灵");
            obj.transform.position = WorldTraversal.NearestWalkable(at, RadiusFor(form));
            var companion = obj.AddComponent<SummonedCompanion>();
            companion.Owner = owner; companion.session = game; companion.Form = form;
            companion.rank = Mathf.Clamp(rank, 0, 3); companion.epoch = owner.CombatEpoch; companion.damage = strength;
            companion.IsStarter = foundation; companion.IsPermanent = permanent;
            companion.RefreshPower(true);
            companion.RemainingLifetime = CompanionRules.ContractLifetime((int)form,rank,permanent);
            companion.model = CombatModel.Companion(obj.transform, form);
            companion.BuildHealthBar();
            active.Add(companion);
            AdvancedSkillVfx.Rune(owner, obj.transform.position, form == Kind.Treant ? 2.6f : 1.3f, GameBalance.ClassColor(HeroClass.Summoner), .7f, Mathf.Max(1, rank));
            return companion;
        }

        public static bool HasStarter(PlayerController owner)
        {
            foreach (SummonedCompanion pet in active)
                if (pet != null && pet.IsAlive && pet.Owner == owner && pet.IsStarter) return true;
            return false;
        }

        public static SummonedCompanion SummonStarter(PlayerController owner, GameSession game, float strength)
        {
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner || HasStarter(owner)) return null;
            int learnedRank = game.Progression.Profile.skillRanks[2];
            SummonedCompanion companion = Summon(owner, game, Kind.Wolf, learnedRank,
                owner.transform.position - owner.transform.forward * 1.2f, strength, true, true);
            if (companion != null) companion.gameObject.name = "灵狼 · 常驻伙伴";
            return companion;
        }

        public static void DismissStarters(PlayerController owner)
        {
            foreach (SummonedCompanion pet in active.ToArray())
                if (pet != null && pet.Owner == owner && pet.IsStarter) pet.Dismiss();
        }

        public static void EnforceCapacity(PlayerController owner)
        {
            if (owner == null || owner.IsDead) return;
            bool bonded = owner != null && GameSession.Instance != null && GameSession.Instance.Progression.Profile.summonerRoute == SummonerRoute.Bonded;
            bool keptSpirit = false;
            // Keep the oldest living spirit. Dismiss removes the current entry
            // immediately; examine the shifted entry before advancing the cursor.
            for (int i = 0; i < active.Count;)
            {
                SummonedCompanion pet = active[i];
                if (pet == null || !pet.IsAlive || pet.Owner != owner) { i++; continue; }
                if (bonded && pet.Form == Kind.Spirit)
                {
                    if (keptSpirit) { pet.Dismiss(); continue; }
                    keptSpirit = true;
                }
                if (pet.Form == Kind.Spirit && bonded && !pet.IsPermanent)
                { pet.IsPermanent = true; pet.RemainingLifetime = float.PositiveInfinity; }
                else if (pet.Form == Kind.Spirit && !bonded && pet.IsPermanent)
                {
                    // A preset can lower rank and leave Bonded in one commit.
                    // Reconcile the former permanent body before making it timed.
                    pet.RefreshPower(false);
                    pet.IsPermanent = false; pet.RemainingLifetime = CompanionRules.ContractLifetime((int)pet.Form,pet.rank,false);
                }
                if (bonded && pet.Form == Kind.Wolf && !pet.IsStarter) { pet.Dismiss(); continue; }
                i++;
            }
            int cap = CompanionRules.NormalCapacity(owner.HasMechanic(EquipmentMechanic.TwinSummonResonance));
            while (Count(owner) > cap)
            {
                SummonedCompanion oldest = EvictionCandidate(owner, Kind.Wolf);
                if (oldest == null) break;
                oldest.Dismiss();
            }
        }

        // Profile notifications also run while menus pause Update. Reconcile the
        // route/cap first so a just-bonded Spirit can survive immediate travel.
        public static void RefreshBuild(PlayerController owner)
        {
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner) return;
            EnforceCapacity(owner);
            foreach (SummonedCompanion pet in active)
                if (pet != null && pet.Owner == owner && pet.IsAlive) pet.RefreshPower(false);
        }

        public static void TransferPermanentPartners(PlayerController owner)
        {
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner) return;
            foreach (SummonedCompanion pet in active.ToArray())
            {
                // IsAlive includes the old epoch, so inspect the surviving body
                // directly after Teleport increments the owner's combat epoch.
                if (pet == null || pet.Owner != owner || !CompanionRules.CanTransfer(pet.IsPermanent, pet.Health, pet.gameObject.activeInHierarchy) || pet.session == null || pet.session.Player != owner) continue;
                pet.epoch = owner.CombatEpoch;
                pet.commandedTarget = pet.target = null; pet.hasCommandPoint = false;
                pet.commandTime = pet.recallTime = pet.idleTime = pet.attackPose = 0;
                pet.commandMultiplier = 1;
                pet.commandEmpowered = false;
                pet.cooldown = Mathf.Max(.35f, pet.cooldown);
                pet.RefreshPower(false);
                pet.statRefresh = .5f;
                Vector3 anchor = owner.transform.position - owner.transform.forward * 1.2f + owner.transform.right * (pet.Form == Kind.Wolf ? -1.1f : 1.1f);
                anchor = WorldTraversal.NearestWalkable(anchor, pet.NavigationRadius);
                if (!WorldTraversal.HasGroundPath(owner.transform.position, anchor, pet.NavigationRadius))
                    anchor = WorldTraversal.NearestWalkable(owner.transform.position, pet.NavigationRadius);
                pet.transform.position = anchor;
                pet.RemainingLifetime = float.PositiveInfinity;
            }
            // New encounter cannot inherit cooperation marks or a queued command.
            bonds[owner] = new BondState { Epoch = owner.CombatEpoch };
        }

        public static void OnPerfectDodge(PlayerController owner)
        {
            if (owner == null || owner.IsDead) return;
            State(owner).Commands.Grant(Time.time);
            // Protection changes only damage taken. It never clears targets, recalls,
            // teleports or pauses companion attacks.
            var game=GameSession.Instance;
            if(game!=null)game.LogSystem("护契 · 伙伴减伤3秒，下次契约强化保留16秒");
        }
        public static float CommandOpportunityRemaining(PlayerController owner)
        {return owner==null||owner.IsDead?0:State(owner).Commands.Remaining(Time.time);}

        // Free orders change only navigation/target choice. They never invoke a
        // contract, reset recovery, consume a dodge opportunity or grant protection.
        public static bool SetFreeFocus(PlayerController owner, EnemyController enemy)
        {
            var game = GameSession.Instance;
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner || game == null || game.InputBlocked || game.CombatEnded || enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy ||
                CombatFx.Flat(enemy.transform.position - owner.transform.position).sqrMagnitude > 196f) return false;
            State(owner).Directive.Focus(enemy);
            foreach (var pet in Snapshot(owner)) { pet.commandedTarget = enemy; pet.hasCommandPoint = false; }
            return true;
        }
        public static bool FreeRecall(PlayerController owner)
        {
            var game = GameSession.Instance;
            if (owner == null || owner.IsDead || owner.HeroClass != HeroClass.Summoner || game == null || game.InputBlocked || game.CombatEnded) return false;
            State(owner).Directive.Recall();
            foreach (var pet in Snapshot(owner)) { pet.commandedTarget = pet.target = null; pet.hasCommandPoint = false; }
            return true;
        }
        public static EnemyController ExplicitFocus(PlayerController owner)
        {
            if (owner == null || owner.IsDead) return null;
            var directive = State(owner).Directive;
            return directive.Resolve(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy && CombatFx.Flat(e.transform.position-owner.transform.position).sqrMagnitude <= 196f);
        }

        public static void RecallAll(PlayerController owner)
        {
            if (owner != null) State(owner).Directive.Clear();
            foreach (SummonedCompanion pet in Snapshot(owner))
            {
                pet.recallTime = CompanionRules.RecallDuration;
                pet.commandedTarget = null; pet.hasCommandPoint = false;
                pet.target = null;
            }
        }

        public static SummonedCompanion CastContract(PlayerController owner, GameSession game, Kind form, int rank, Vector3 at,
            float strength, bool timedPackRoute, EnemyController focus = null, bool preserveTargetPoint = false)
        {
            if (owner == null || owner.IsDead || game == null) return null;
            SummonedCompanion partner = form == Kind.Wolf
                ? active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && pet.IsStarter)
                : active.Find(pet => pet != null && pet.IsAlive && pet.Owner == owner && pet.Form == form);
            if (CompanionRules.ShouldCreatePartner(partner != null))
                partner = form == Kind.Wolf ? SummonStarter(owner, game, strength)
                    : Summon(owner, game, form, rank, at, strength, CompanionRules.PermanentPartner(false, (int)form, timedPackRoute));
            if (partner == null) return null;
            partner.RefreshContractPower(rank);
            if (!partner.IsPermanent) partner.RemainingLifetime = Mathf.Max(partner.RemainingLifetime, CompanionRules.ContractLifetime((int)form,rank,false));
            BondState state = State(owner);
            state.Directive.Clear();
            bool empowered = state.Commands.TryConsume(Time.time);
            partner.Command(focus, empowered, at, preserveTargetPoint);
            if (timedPackRoute && form == Kind.Wolf)
            {
                // Refresh the living pack even when no slots remain. No health fill,
                // replacement, stacking lifetime, or capacity change occurs.
                foreach (SummonedCompanion living in Snapshot(owner))
                    if (living != partner && living.Form == Kind.Wolf && !living.IsPermanent)
                    {
                        living.RefreshContractPower(rank);
                        living.RemainingLifetime = CompanionRules.RefreshPackLifetime(living.RemainingLifetime,rank);
                        living.Command(focus,empowered,at,preserveTargetPoint);
                    }
                int reinforcements = CompanionRules.PackReinforcements(Count(owner), rank, owner.HasMechanic(EquipmentMechanic.TwinSummonResonance));
                for (int i = 0; i < reinforcements; i++)
                {
                    SummonedCompanion pet = Summon(owner, game, Kind.Wolf, rank, owner.transform.position + owner.transform.right * (i % 2 == 0 ? -1.5f : 1.5f), strength);
                    if (pet != null) { pet.RemainingLifetime = CompanionRules.PackLifetime(rank); pet.Command(focus, empowered, at, preserveTargetPoint); }
                }
            }
            game.RecordCombatAction("契约指令");
            return partner;
        }

        private void RefreshContractPower(int requestedRank)
        {
            float healthBefore=Health;
            rank=Mathf.Clamp(requestedRank,1,3);RefreshPower(false);
            Health=CompanionRules.PreserveRecastHealth(healthBefore,MaxHealth);
        }

        private void RefreshPower(bool fill)
        {
            if (Owner == null || session == null) return;
            int[] learned = session.Progression.Profile.skillRanks;
            rank = CompanionRules.EffectiveRank((int)Form, IsStarter, IsPermanent, rank, learned[2], learned[4]);
            StatBlock stats = session.Progression.GetStats();
            damage = stats.Damage * CompanionRules.RankPower(rank);
            baseMaxHealth = stats.MaxHealth * CompanionRules.HealthFraction((int)Form, rank, IsStarter);
            float maximum = baseMaxHealth * CompanionRules.HealthMultiplier(Owner.HasMechanic(EquipmentMechanic.TwinSummonResonance));
            // Build/gear/rank changes must not heal a damaged living body. This
            // also makes repeated preset application and transfers idempotent.
            Health = fill ? maximum : CompanionRules.PreserveRecastHealth(Health, maximum);
            MaxHealth = maximum;
            if (commandTime > 0) commandMultiplier = CompanionRules.ActiveCommandMultiplier(rank, commandEmpowered);
        }

        private bool ValidTarget(EnemyController enemy)
        {
            return enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy &&
                CombatFx.Flat(enemy.transform.position - Owner.transform.position).sqrMagnitude <= 14f * 14f;
        }

        private void Command(EnemyController focus, bool empowered, Vector3 point, bool preservePoint)
        {
            recallTime = 0;
            hasCommandPoint = false;
            commandedTarget = ValidTarget(focus) ? focus : preservePoint ? null : Owner.FocusTarget;
            if (!ValidTarget(commandedTarget)) commandedTarget = preservePoint ? null : AcquireTarget();
            if (preservePoint && commandedTarget == null)
            { commandedPoint = WorldTraversal.NearestWalkable(CombatSight.GroundPoint(Owner.transform.position,point),NavigationRadius); hasCommandPoint = true; }
            commandTime = CompanionRules.CommandDuration(empowered);
            commandEmpowered = empowered;
            commandMultiplier = CompanionRules.ActiveCommandMultiplier(rank, commandEmpowered);
            if (commandedTarget == null) { if (!hasCommandPoint) recallTime = CompanionRules.RecallDuration; return; }
            if (Form == Kind.Wolf)
            {
                Vector3 previous = transform.position;
                Vector3 towards = CombatFx.Flat(commandedTarget.transform.position - previous);
                transform.position = WorldTraversal.Move(previous, towards.normalized * Mathf.Min(3.5f, Mathf.Max(0, towards.magnitude - .7f)), NavigationRadius);
                AdvancedSkillVfx.Beam(Owner, previous + Vector3.up * .5f, transform.position + Vector3.up * .5f, GameBalance.ClassColor(HeroClass.Summoner), .25f, .15f);
                if (CombatFx.Flat(commandedTarget.transform.position - transform.position).magnitude <= 1.8f && CanReachTarget(commandedTarget.transform.position))
                {
                    commandedTarget.TakeDamage(damage * AttackMultiplier * CompanionRules.WolfCommandCoefficient, towards.normalized, .35f, .2f);
                    OnConfirmedHit(commandedTarget);
                    cooldown = CompanionRules.WolfCommandRecovery; attackPose = 1;
                }
            }
            else cooldown = Mathf.Min(cooldown, CompanionRules.CommandReadyDelay);
        }

        private float AttackMultiplier { get { return CompanionRules.DamageMultiplier(Owner.HasMechanic(EquipmentMechanic.TwinSummonResonance)) * (commandTime > 0 ? commandMultiplier : 1f); } }

        public void OnConfirmedHit(EnemyController enemy)
        {
            if (!IsAlive || enemy == null) return;
            if (commandedTarget == enemy || ExplicitFocus(Owner) == enemy)
                session.RecordClassTutorial(HeroClass.Summoner);
            if (!ValidTarget(enemy)) return;
            if (!Owner.HasMechanic(EquipmentMechanic.TwinSummonResonance)) return;
            bool commandedFocus = false;
            foreach (SummonedCompanion partner in active)
                if (partner != null && partner.Owner == Owner && partner.IsAlive && partner.commandTime > 0 && partner.commandedTarget == enemy)
                { commandedFocus = true; break; }
            if (!CompanionRules.CoordinatedTarget(Owner.FocusTarget == enemy, commandedFocus)) return;
            if (!State(Owner).Cooperation.RegisterHit(enemy, (int)Form, Time.time)) return;
            enemy.TakeDamage(damage * CompanionRules.DamageMultiplier(true) * CompanionRules.CooperationDamage, Vector3.zero, impact: false);
            AdvancedSkillVfx.Beam(Owner, transform.position + Vector3.up, enemy.transform.position + Vector3.up, new Color(.5f, 1f, .9f), .3f, .2f);
            session.SpawnMechanismText(enemy.transform.position + Vector3.up * 2f, "异契共鸣", new Color(.5f, 1f, .9f));
            session.RecordCombatAction("双契共鸣");
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

        public void TakeDamage(float amount, bool areaAttack = false)
        {
            if (!IsAlive) return;
            amount = CompanionRules.DamageTaken(amount, areaAttack, recallTime > 0, State(Owner).Commands.IsProtected(Time.time));
            if (amount <= 0) return;
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
            if (recallTime > 0) return null;
            var directive = State(Owner).Directive;
            EnemyController explicitTarget = directive.Resolve(ValidTarget);
            if (directive.Recalling) return null;
            if (explicitTarget != null) return explicitTarget;
            if (ValidTarget(commandedTarget)) return commandedTarget;
            if (commandTime > 0 && hasCommandPoint) return null;
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
            recallTime = Mathf.Max(0, recallTime - dt);
            commandTime = Mathf.Max(0, commandTime - dt);
            if (!ValidTarget(commandedTarget)) commandedTarget = null;
            if (commandTime <= 0) hasCommandPoint = false;
            statRefresh -= dt;
            if (statRefresh <= 0) { statRefresh = .5f; RefreshPower(false); }
            RemainingLifetime -= dt;
            if (RemainingLifetime <= 0) { Dismiss(); return; }
            cooldown -= dt;
            attackPose = Mathf.Max(0, attackPose - dt * 3);
            target = AcquireTarget();
            bool threatened = false;
            foreach (EnemyController hostile in session.Enemies)
                if (hostile != null && !hostile.IsDead && hostile.IsAggro && CombatFx.Flat(hostile.transform.position - Owner.transform.position).sqrMagnitude <= CompanionRules.RestThreatRadius * CompanionRules.RestThreatRadius) { threatened = true; break; }
            idleTime = target == null && !threatened ? idleTime + dt : 0;
            if (IsPermanent && idleTime > 3f && CombatFx.Flat(transform.position - Owner.transform.position).sqrMagnitude < 9f)
                Health = Mathf.Min(MaxHealth, Health + MaxHealth * .06f * dt);
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
            Vector3 destination = target == null ? hasCommandPoint && commandTime > 0 ? commandedPoint : followAnchor : target.transform.position;
            Vector3 delta = CombatFx.Flat(destination - transform.position);
            float attackRange = Form == Kind.Spirit ? 7.5f : Form == Kind.Treant ? 2.8f : 1.4f;
            bool attackPath = target == null || CanReachTarget(target.transform.position);
            bool moving = delta.magnitude > (target == null ? .5f : attackRange * .85f) || !attackPath;
            Vector3 heading = moving ? route.Direction(transform.position, destination, NavigationRadius) : delta.normalized;
            if (heading.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), 1 - Mathf.Exp(-10 * dt));
            Vector3 previous = transform.position;
            transform.position = WorldTraversal.Move(transform.position, moving ? heading * Mathf.Min(delta.magnitude, dt * (Form == Kind.Treant ? 4.2f : recallTime > 0 ? 9f : 7f)) : Vector3.zero, NavigationRadius);
            moving = (transform.position - previous).sqrMagnitude > .000001f;
            delta = CombatFx.Flat(destination - transform.position);
            if (target != null && delta.magnitude <= attackRange && cooldown <= 0 && CanReachTarget(target.transform.position))
            {
                attackPose = 1;
                float attackDamage = damage * AttackMultiplier;
                cooldown = CompanionRules.AttackInterval((int)Form);
                if (Form == Kind.Spirit)
                    CombatProjectile.Friendly(Owner, session, transform.position, delta.normalized, attackDamage * CompanionRules.AttackCoefficient((int)Form), GameBalance.ClassColor(HeroClass.Summoner), tracking: target, companionSource: this);
                else if (Form == Kind.Treant)
                {
                    CombatFx.Ring(transform.position, 3.3f * GameBalance.SkillRangeMultiplier(rank), new Color(.48f, 1f, .63f), .4f, .2f);
                    foreach (var enemy in session.Enemies.ToArray())
                        if (enemy != null && !enemy.IsDead && CombatFx.Flat(enemy.transform.position - transform.position).magnitude < 3.3f * GameBalance.SkillRangeMultiplier(rank)
                            && WorldTraversal.HasGroundPath(transform.position, enemy.transform.position, .12f))
                        {
                            enemy.TakeDamage(attackDamage * CompanionRules.AttackCoefficient((int)Form), enemy.transform.position - transform.position, .55f, .3f);
                            if (!enemy.IsDead) enemy.StatusEffects.Knockdown(.45f + rank * .12f);
                            OnConfirmedHit(enemy);
                        }
                }
                else
                {
                    CombatFx.Slash(transform.position, delta.normalized, 1.25f, new Color(.55f, 1f, .87f));
                    target.TakeDamage(attackDamage * CompanionRules.AttackCoefficient((int)Form), delta.normalized, .13f, .05f);
                    OnConfirmedHit(target);
                }
            }
            // Sample only this frame's navigated displacement, after the actual
            // release event so projectile/damage and contact pose share a frame.
            float locomotion = CombatFx.Flat(transform.position - previous).magnitude / Mathf.Max(.0001f, dt * (Form == Kind.Treant ? 4.2f : 7f));
            model.Animate(Mathf.Clamp01(locomotion), attackPose, false);
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
