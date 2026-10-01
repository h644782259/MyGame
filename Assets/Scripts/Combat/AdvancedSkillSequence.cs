using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // A cast has a finite event budget. It keeps the cast's stats/aim and never
    // follows a newly loaded hero or survives a teleport to another encounter.
    internal sealed class AdvancedSkillSequence : MonoBehaviour
    {
        private PlayerController owner;
        private GameSession session;
        private HeroClass heroClass;
        private int skill, rank, epoch, step, steps;
        private float damage, range, interval, age, nextEvent;
        private Vector3 target, forward, origin;
        private Color color;
        private EnemyController lockedTarget;

        public static void Spawn(PlayerController hero, GameSession game, int index, int skillRank, Vector3 aim, Vector3 direction, float strength, Color tint)
        {
            GameObject obj = new GameObject("Skill Sequence " + index);
            AdvancedSkillSequence sequence = obj.AddComponent<AdvancedSkillSequence>();
            sequence.owner = hero; sequence.session = game; sequence.heroClass = hero.HeroClass;
            sequence.skill = index; sequence.rank = skillRank; sequence.epoch = hero.CombatEpoch;
            sequence.damage = strength; sequence.range = GameBalance.SkillRangeMultiplier(skillRank);
            sequence.target = aim; sequence.origin = hero.transform.position; sequence.forward = CombatFx.Flat(direction).normalized;
            sequence.color = tint;
            sequence.Configure();
        }

        private void Configure()
        {
            steps = 1; interval = .2f;
            if(skill==6)
            {
                steps=5; interval=1f; nextEvent=1f;
                owner.HealingProtection(rank);
                AdvancedSkillVfx.Rune(owner,origin,3.2f*range,color,5.3f,rank,true);
                return;
            }
            if (heroClass == HeroClass.Vanguard)
            {
                if (skill == 7) { steps = 6 + rank-1; interval = .18f; }
                if (skill == 9) { steps = 6 + rank-1; interval = .4f; nextEvent = .6f; }
            }
            else if (heroClass == HeroClass.Arcanist)
            {
                if (skill == 7) { steps = 9 + (rank-1)*2; interval = .45f; nextEvent = .3f; }
                if (skill == 9) { steps = 6 + rank-1; interval = .45f; nextEvent = .6f; }
            }
            else
            {
                if (skill == 7) { steps = 14 + (rank-1)*3; interval = .12f; lockedTarget = Nearest(target,10f*range); }
                if (skill == 9) { steps = 9 + rank-1; interval = .25f; nextEvent = .6f; }
            }
            if (skill >= 6)
                AdvancedSkillVfx.Rune(owner,skill==7 && heroClass==HeroClass.Ranger?origin:target,4.2f*range,color,nextEvent+steps*interval+.5f,rank+1);
            else AdvancedSkillVfx.Rune(owner,origin,1.6f*range,color,.7f,rank);
        }

        private void Update()
        {
            if (owner == null || owner.IsDead || session == null || session.Player != owner || !session.HasStarted || owner.CombatEpoch != epoch)
            { Destroy(gameObject); return; }
            if (Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (heroClass == HeroClass.Arcanist && skill == 7) Pull(target,5.2f*range,rank==3?6f:4f);
            // Limit catch-up to three events per frame after a frame-time spike.
            int catchup = 0;
            while (step < steps && age >= nextEvent && catchup++ < 3)
            {
                if (skill==6) Healing();
                else if (heroClass == HeroClass.Vanguard) Vanguard();
                else if (heroClass == HeroClass.Arcanist) Arcanist();
                else Ranger();
                step++; nextEvent += interval;
            }
            if (step >= steps) Destroy(gameObject);
        }

        private void Healing()
        {
            float total=rank==3?.55f:rank==2?.42f:.3f;
            owner.Heal(owner.MaxHealth*total/5f);
            if (heroClass == HeroClass.Summoner) SummonedCompanion.HealAll(owner, total / 5f);
            Vector3 at=owner.transform.position;
            CombatFx.Ring(at,3.2f*range,color,.7f,.13f);
            if(heroClass==HeroClass.Vanguard)
            {
                AdvancedSkillVfx.Beam(owner,at,at+Vector3.up*3.5f,new Color(1f,.85f,.38f),.85f,.12f);
                AdvancedSkillVfx.Beam(owner,at+Vector3.up*3.2f,at+Vector3.up*2.2f+Vector3.right*1.3f,new Color(1f,.85f,.38f),.85f,.25f);
            }
            else if(heroClass==HeroClass.Arcanist)
            {
                AdvancedSkillVfx.Rune(owner,at,2f*range,new Color(.5f,.88f,1f),.8f,rank);
                AdvancedSkillVfx.Beam(owner,at+Vector3.up*.2f,at+Vector3.up*4f,new Color(.7f,.94f,1f),.6f,.18f);
            }
            else
            {
                for(int i=0;i<4;i++)
                {
                    Vector3 root=at+Circle(i*Mathf.PI*.5f,1.4f*range);
                    AdvancedSkillVfx.Beam(owner,root,Vector3.Lerp(root,at,.5f)+Vector3.up*2.8f,new Color(.53f,1f,.56f),.75f,.09f);
                }
            }
            if(rank==3 && step==steps-1) owner.RestoreSkillEnergy(8f);
        }

        private void Vanguard()
        {
            switch (skill)
            {
                case 5: // Dash damages the whole traversed lane once, not only its endpoint.
                    Vector3 start = owner.transform.position;
                    owner.SkillDash(forward,7f*range,.5f);
                    HitLine(start,owner.transform.position,1.3f*range,damage*5.2f,1.3f,.55f+rank*.15f);
                    AdvancedSkillVfx.Rune(owner,owner.transform.position,2f*range,color,.65f,rank);
                    if (rank >= 2) owner.HitArea(owner.transform.position,2.7f*range,damage*1.2f,.8f,.5f);
                    if (rank == 3) CombatArea.Spawn(owner,session,start,2.5f*range,damage*1.4f,.6f,.4f,0,1,color);
                    break;
                case 7: // A travelling fault with perpendicular fissures.
                    Vector3 fault = Clamp(origin+forward*(2f+step*1.8f)*range);
                    Vector3 tangent = Vector3.Cross(Vector3.up,forward);
                    AdvancedSkillVfx.Beam(owner,fault-tangent*2.7f*range,fault+tangent*2.7f*range,new Color(1f,.65f,.23f),.65f,.28f);
                    AdvancedSkillVfx.FallingBlade(owner,fault,color,.5f);
                    owner.HitArea(fault,2.6f*range,damage*2.8f,.7f,.5f);
                    LaunchArea(fault, 2.6f * range, .6f + rank * .12f, 1f + rank * .2f);
                    if (rank==3 && step==steps-1) Burst(fault,4f*range,damage*4f,color,3);
                    break;
                case 9: // Successive executions culminate in one enormous falling blade.
                    if (step < steps-1)
                    {
                        float radius = (2.4f+step*.55f)*range;
                        AdvancedSkillVfx.FallingBlade(owner,target+Circle(step*2.1f,1.8f*range),color,.85f);
                        CombatFx.Ring(target,radius,color,.5f,.18f);
                        owner.HitArea(target,radius,damage*2.2f,.1f,.2f);
                    }
                    else
                    {
                        AdvancedSkillVfx.FallingBlade(owner,target,new Color(1f,.95f,.63f),2.1f);
                        Burst(target,6.2f*range,damage*8f,color,3);
                        if (rank==3) CombatArea.Spawn(owner,session,target,5.2f*range,damage*1.1f,.25f,.5f,2f,.5f,color);
                    }
                    break;
            }
        }

        private void Arcanist()
        {
            switch (skill)
            {
                case 4:
                    ChainLightning(6+(rank-1)*2);
                    break;
                case 7:
                    if(step<steps-1)
                    {
                        float a=step*.95f;
                        AdvancedSkillVfx.Beam(owner,target+Circle(a,4.3f*range)+Vector3.up*2,target+Vector3.up*.4f,new Color(.74f,.42f,1f),.45f,.17f);
                        owner.HitArea(target,4.8f*range,damage*.85f,0,.12f);
                    }
                    else Burst(target,5.3f*range,damage*(rank==3?6f:4f),new Color(.84f,.6f,1f),3);
                    break;
                case 9:
                    Color element = step%3==0?new Color(1f,.5f,.28f):step%3==1?new Color(.5f,.92f,1f):new Color(.77f,.48f,1f);
                    if(step<steps-1)
                    {
                        Vector3 axis = Circle(step*.65f,6f*range);
                        AdvancedSkillVfx.Beam(owner,target-axis+Vector3.up,target+axis+Vector3.up,element,.65f,.34f);
                        AdvancedSkillVfx.Beam(owner,target+Vector3.up*10f,target,element,.65f,.24f);
                        owner.HitArea(target,5.5f*range,damage*2.2f,0,step%3==1?.8f:.1f);
                    }
                    else
                    {
                        Burst(target,6.5f*range,damage*9f,new Color(.92f,.83f,1f),3);
                        if(rank==3) CombatArea.Spawn(owner,session,target,5.5f*range,damage*1.1f,.3f,.6f,2.4f,.6f,color,false,false,3f);
                    }
                    break;
            }
        }

        private void Ranger()
        {
            switch(skill)
            {
                case 4:
                    owner.SkillDash(-forward,5f*range,.65f+(rank-1)*.15f);
                    owner.MobilityBuff(rank);
                    for(int i=0;i<3+rank-1;i++)
                    {
                        Vector3 dir=Quaternion.Euler(0,(i-(rank+1)*.5f)*9f,0)*forward;
                        if (WorldTraversal.HasLineOfSight(owner.transform.position, owner.transform.position + dir))
                            CombatProjectile.Friendly(owner,session,owner.transform.position+dir,dir,damage*1.65f,color,rank>=2,true,false,range,21f*range,rank==3?Nearest(target,8f*range):null);
                    }
                    if(rank==3) CombatArea.Spawn(owner,session,origin,3f*range,damage*2f,1.2f,.5f,0,1,color);
                    break;
                case 5:
                    AdvancedSkillVfx.Rune(owner,target,3.7f*range,new Color(.55f,.95f,.3f),4.3f+rank,rank+1);
                    CombatArea.Spawn(owner,session,target,3.7f*range,damage*.7f,0,.25f,3.6f+(rank-1)*.8f,.6f,new Color(.51f,.88f,.32f),false,false,rank==3?2.8f:0,rank==3?damage*2.5f:0,5,rank);
                    break;
                case 7:
                    EnemyController mark=lockedTarget != null && !lockedTarget.IsDead ? lockedTarget : null;
                    if (mark != null && mark.StatusEffects != null) mark.StatusEffects.Mark(4f, .08f + rank * .04f);
                    Vector3 fireDirection=mark!=null?CombatFx.Flat(mark.transform.position-owner.transform.position).normalized:forward;
                    Vector3 side=Vector3.Cross(Vector3.up,fireDirection)*(step%2==0?-.55f:.55f);
                    if (WorldTraversal.HasLineOfSight(owner.transform.position, owner.transform.position + side + fireDirection))
                        CombatProjectile.Friendly(owner,session,owner.transform.position+side+fireDirection,fireDirection,damage*1.05f,color,rank>=2,true,false,range,24f*range,mark);
                    if(step%4==0) AdvancedSkillVfx.Beam(owner,owner.transform.position+side+Vector3.up,owner.transform.position+fireDirection*7f+side+Vector3.up,color,.2f,.07f);
                    break;
                case 9:
                    if(step<steps-1)
                    {
                        Vector3 rainAt=target+Circle(step*2.4f,1.8f*range);
                        for(int i=0;i<3+rank;i++)
                        {
                            Vector3 landing=rainAt+Circle(i*Mathf.PI*2/(3+rank),.65f*range);
                            AdvancedSkillVfx.Beam(owner,landing+new Vector3(2,8,1),landing,new Color(.75f,1f,.63f),.4f,.09f);
                        }
                        owner.HitArea(rainAt,3.4f*range,damage*1.35f,0,.12f);
                    }
                    else
                    {
                        AdvancedSkillVfx.Beam(owner,target+Vector3.up*12f,target,new Color(.93f,1f,.65f),.8f,.7f);
                        Burst(target,6f*range,damage*8.5f,color,3);
                        if(rank==3) for(int i=0;i<12;i++) CombatProjectile.Friendly(owner,session,target,Circle(i*Mathf.PI/6,1),damage*1.2f,color,true,true,false,1.5f,22f);
                    }
                    break;
            }
        }

        private void LaunchArea(Vector3 center, float radius, float duration, float height)
        {
            foreach (EnemyController enemy in session.Enemies)
                if (enemy != null && !enemy.IsDead && enemy.StatusEffects != null && CombatFx.Flat(enemy.transform.position - center).magnitude <= radius)
                    enemy.StatusEffects.Knockup(duration, height);
        }

        private void ChainLightning(int maximumTargets)
        {
            HashSet<EnemyController> struck = new HashSet<EnemyController>();
            Vector3 previous = owner.transform.position;
            Vector3 search = target;
            for(int i=0;i<maximumTargets;i++)
            {
                EnemyController nearest=null;
                float best=(i==0?6f:7f)*range;
                for(int j=0;j<session.Enemies.Count;j++)
                {
                    EnemyController enemy=session.Enemies[j];
                    if(enemy==null || enemy.IsDead || struck.Contains(enemy)) continue;
                    float distance=CombatFx.Flat(enemy.transform.position-search).magnitude;
                    if(distance<best) { best=distance; nearest=enemy; }
                }
                if(nearest==null) break;
                struck.Add(nearest);
                Vector3 position=nearest.transform.position;
                AdvancedSkillVfx.Beam(owner,previous+Vector3.up*1.1f,position+Vector3.up*1.1f,new Color(.7f,.85f,1f),.55f,.17f);
                nearest.TakeDamage(damage*2.9f,forward,.05f,.35f+rank*.15f);
                if(rank==3) owner.HitArea(position,1.8f*range,damage*.65f,0,.1f);
                previous=search=position;
            }
            if(struck.Count==0) AdvancedSkillVfx.Beam(owner,previous+Vector3.up,target+Vector3.up,new Color(.7f,.85f,1f),.45f,.18f);
        }

        private EnemyController Nearest(Vector3 at,float maximumDistance)
        {
            EnemyController nearest=null;
            float best=maximumDistance;
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy.IsDead) continue;
                float distance=CombatFx.Flat(enemy.transform.position-at).magnitude;
                if(distance<best) { nearest=enemy; best=distance; }
            }
            return nearest;
        }

        private void Pull(Vector3 at,float radius,float strength)
        {
            for(int i=0;i<session.Enemies.Count;i++)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy==null || enemy.IsDead) continue;
                Vector3 delta=CombatFx.Flat(at-enemy.transform.position);
                if(delta.magnitude<radius && delta.magnitude>.6f)
                    enemy.transform.position = WorldTraversal.Move(enemy.transform.position, delta.normalized * Mathf.Min(delta.magnitude - .6f, strength * Time.deltaTime * (enemy.IsBoss ? .25f : 1f)), enemy.NavigationRadius);
            }
        }

        private void HitLine(Vector3 a,Vector3 b,float width,float amount,float knockback,float stun)
        {
            for(int i=session.Enemies.Count-1;i>=0;i--)
            {
                EnemyController enemy=session.Enemies[i];
                if(enemy!=null && !enemy.IsDead && CombatFx.SegmentDistance(enemy.transform.position,a,b)<=width+(enemy.IsBoss?.8f:.4f))
                    enemy.TakeDamage(amount,forward,knockback,stun);
            }
        }

        private void Burst(Vector3 at,float radius,float amount,Color tint,int detail)
        {
            AdvancedSkillVfx.Rune(owner,at,radius,tint,.8f,detail);
            CombatFx.Ring(at,radius,tint,.6f,.25f);
            owner.HitArea(at,radius,amount,1.1f,.7f);
        }

        private Vector3 Clamp(Vector3 point) { return Vector3.ClampMagnitude(CombatFx.Flat(point),session.ArenaRadius-.7f); }
        private static Vector3 Circle(float angle,float radius) { return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius; }
    }
}
