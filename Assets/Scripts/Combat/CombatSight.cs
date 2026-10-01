using UnityEngine;
namespace Emberfall
{
    /// <summary>Shared preview and impact policy. No physics/layer-dependent shortcuts.</summary>
    internal static class CombatSight
    {
        private static bool Finite(Vector3 p)
        {return !float.IsNaN(p.x)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.z);}
        private static bool Reach(CombatSightKind kind,Vector3 origin,Vector3 target)
        {
            if(!Finite(origin)||!Finite(target))return false;
            return CombatSightRules.Allows(kind,WorldTraversal.HasLineOfSight(origin,target),
                kind!=CombatSightKind.Melee||WorldTraversal.HasGroundPath(origin,target,.15f));
        }
        public static bool Direct(Vector3 origin,Vector3 target){return Reach(CombatSightKind.Direct,origin,target);}
        public static bool Area(Vector3 center,Vector3 target){return Reach(CombatSightKind.Area,center,target);}
        public static bool Chain(Vector3 previous,Vector3 target){return Reach(CombatSightKind.Chain,previous,target);}
        public static bool Melee(Vector3 origin,Vector3 target){return Reach(CombatSightKind.Melee,origin,target);}
        public static Vector3 GroundPoint(Vector3 origin,Vector3 desired) { return BoundaryPoint(CombatSightKind.GroundPlacement,origin,desired); }
        public static Vector3 BoundaryPoint(CombatSightKind kind,Vector3 origin,Vector3 desired)
        {
            origin=CombatFx.Flat(origin);desired=CombatFx.Flat(desired);
            if(!Finite(origin))return Vector3.zero;
            if(!Finite(desired))return origin;
            if(!Reach(kind,origin,origin))return origin;
            if(Reach(kind,origin,desired))return desired;
            float low=0,high=1;
            for(int n=0;n<CombatSightRules.RefinementSteps;n++)
            {float mid=(low+high)*.5f;if(Reach(kind,origin,Vector3.Lerp(origin,desired,mid)))low=mid;else high=mid;}
            return Vector3.Lerp(origin,desired,low);
        }
    }
}
