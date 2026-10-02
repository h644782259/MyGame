using System;
namespace Emberfall
{
    public enum EscapeRole { None, GateGuard, Pursuer, GateSupplier, SideFlanker }
    // Escape-only posts; other rooms and all bosses retain their normal controller.
    public sealed class EscapePostPolicy
    {
        public readonly EscapeRole Role;
        private float chaseSeconds,restSeconds;
        private bool returning;
        public EscapePostPolicy(EscapeRole role){Role=role;}
        public bool ReturnToPost(float delta,float fromPost,float targetFromPost)
        {
            if(Role==EscapeRole.None||Role==EscapeRole.Pursuer)return false;
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return returning;
            restSeconds=Math.Max(0,restSeconds-delta);
            if(returning&&fromPost<=.25f){returning=false;chaseSeconds=0;restSeconds=1;}
            float leash=Role==EscapeRole.GateGuard?5:Role==EscapeRole.GateSupplier?2.6f:4.5f;
            float acquire=Role==EscapeRole.GateGuard?7:Role==EscapeRole.GateSupplier?7.5f:6;
            if(Role==EscapeRole.GateGuard)chaseSeconds=fromPost>.8f?chaseSeconds+delta:0;
            if(fromPost>leash||targetFromPost>acquire||Role==EscapeRole.GateGuard&&chaseSeconds>=3.5f)returning=true;
            return returning||restSeconds>0;
        }
    }
}
