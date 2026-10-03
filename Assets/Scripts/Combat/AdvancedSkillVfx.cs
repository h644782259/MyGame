using UnityEngine;

namespace Emberfall
{
    // Cancellable owner of charge envelopes. FilledSkillVfx supplies the reusable
    // world-space volumes; attack event code remains the sole source of damage.
    internal sealed class AdvancedSkillVfx : MonoBehaviour
    {
        private const int MaximumEffects=36;
        private static int activeEffects;
        private PlayerController owner;
        private int epoch;
        private float age,duration;
        private bool follow,registered;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount(){activeEffects=0;}

        public static AdvancedSkillVfx Rune(PlayerController hero,Vector3 at,float size,Color color,float lifetime,int detail,bool followHero=false,int identity=0)
        {
            if(hero==null||hero.IsDead||activeEffects>=MaximumEffects)return null;
            var obj=new GameObject("Cancellable filled charge envelope");obj.transform.position=at;
            var fx=obj.AddComponent<AdvancedSkillVfx>();fx.owner=hero;fx.epoch=hero.CombatEpoch;
            fx.duration=Mathf.Clamp(lifetime,.12f,12);fx.follow=followHero;fx.registered=true;activeEffects++;
            FilledSkillVfx.Charge(fx.transform,hero,at,size,color,fx.duration,identity);
            return fx;
        }
        public static void Beam(PlayerController hero,Vector3 start,Vector3 end,Color color,float lifetime,float width=.18f)
        {FilledSkillVfx.Thrust(hero,start,end,color,lifetime,width,CombatVisualPriority.ActionBody);}
        public static void FallingBlade(PlayerController hero,Vector3 at,Color color,float scale=1f)
        {
            // Called on the real hit: the descending streak and ground rupture are
            // immediate. There is no cosmetic delayed arrival after damage has landed.
            FilledSkillVfx.Impact(hero,at,1.6f*scale,FilledVfxKind.Sword,color,CombatVisualPriority.RealContact);
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.ModeFinished)
            {Destroy(gameObject);return;}
            if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=duration){Destroy(gameObject);return;}
            if(follow)transform.position=owner.transform.position;
        }
        private void Release(){if(registered){registered=false;activeEffects=Mathf.Max(0,activeEffects-1);}}
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
    }
}
