using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private WeaponStructure weaponStructure = new WeaponStructure(0);
        // For visual ribbons/connector segments only. Never use these animated positions
        // for collision origins, target selection or combat reach.
        public bool TryGetWeaponVisualAnchor(WeaponVisualAnchor anchor, out Vector3 worldPosition)
        {
            Transform rig = anchor <= WeaponVisualAnchor.SwordTip ? swordRig :
                anchor <= WeaponVisualAnchor.StaffTop ? staffRig : bowRig;
            worldPosition = transform.position;
            if (rig == null || anchor < WeaponVisualAnchor.SwordPommel || anchor > WeaponVisualAnchor.BowArrowRest) return false;
            worldPosition = rig.TransformPoint(WeaponAnchorLocal(anchor));
            return true;
        }
        // Alternating swing direction is shared by pose and its visual ribbon.
        public int WeaponSwingSide { get { return swingCount % 2 == 0 ? 1 : -1; } }
        private Vector3 WeaponAnchorLocal(WeaponVisualAnchor anchor)
        {
            switch (anchor)
            {
                case WeaponVisualAnchor.SwordPommel: return new Vector3(0, weaponStructure.Tier == 0 ? -.17f : -.2f, 0);
                case WeaponVisualAnchor.SwordGrip: return new Vector3(0, weaponStructure.Tier == 0 ? 0 : -.03f, 0);
                case WeaponVisualAnchor.SwordGuard: return new Vector3(0, weaponStructure.Tier == 0 ? .16f : .17f, 0);
                case WeaponVisualAnchor.SwordRoot: return new Vector3(0, weaponStructure.SwordRoot, 0);
                case WeaponVisualAnchor.SwordTip: return new Vector3(0, weaponStructure.SwordTip, 0);
                case WeaponVisualAnchor.StaffBottom: return new Vector3(0, weaponStructure.StaffBottom, .03f);
                case WeaponVisualAnchor.StaffGrip: return new Vector3(0, 0, .03f);
                case WeaponVisualAnchor.StaffCollar: return new Vector3(0, weaponStructure.StaffCollar, .03f);
                case WeaponVisualAnchor.StaffCore: return new Vector3(0, weaponStructure.StaffCore, .03f);
                case WeaponVisualAnchor.StaffTop: return new Vector3(0, weaponStructure.StaffTop, .03f);
                case WeaponVisualAnchor.BowGrip: return new Vector3(0, 0, .275f);
                case WeaponVisualAnchor.BowUpperTip: return new Vector3(0, weaponStructure.BowReach, .05f);
                case WeaponVisualAnchor.BowLowerTip: return new Vector3(0, -weaponStructure.BowReach, .05f);
                case WeaponVisualAnchor.BowNock: return arrowRig != null ? arrowRig.localPosition : new Vector3(0, 0, .05f);
                case WeaponVisualAnchor.BowArrowRest: return new Vector3(0, 0, .275f);
                default: return Vector3.zero;
            }
        }
    }
}
