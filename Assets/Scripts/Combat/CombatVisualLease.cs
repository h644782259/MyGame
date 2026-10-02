using UnityEngine;
namespace Emberfall
{
    internal sealed class CombatVisualLease:MonoBehaviour
    {
        private static CombatVisualBudget pool=new CombatVisualBudget();
        private CombatVisualBudget.Ticket ticket;
        private CombatVisualPriority priority;
        private bool initialized,retired;
        internal static int Active {get{return pool.Active;}}
        private static int Limit {get{return EffectPreferences.ReducedEffects?12:Application.isMobilePlatform?20:32;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){pool=new CombatVisualBudget();}
        internal static CombatVisualLease Attach(GameObject root,CombatVisualPriority priority)
        {
            var lease=root.AddComponent<CombatVisualLease>();lease.initialized=true;lease.priority=priority;
            lease.ticket=pool.Acquire(priority,Limit,lease.Retire);
            if(lease.ticket==null){lease.Retire();return null;}return lease;
        }
        internal void Promote(CombatVisualPriority value){priority=value;pool.Promote(ticket,value);}
        private void Retire(){if(retired)return;retired=true;Release();gameObject.SetActive(false);Destroy(gameObject);}
        private void Release(){pool.Release(ticket);ticket=null;}
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
        private void OnEnable(){if(retired){gameObject.SetActive(false);return;}if(initialized&&ticket==null){ticket=pool.Acquire(priority,Limit,Retire);if(ticket==null)Retire();}}
    }
}
