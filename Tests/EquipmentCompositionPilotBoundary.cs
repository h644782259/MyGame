// Explicit test-only disabled feature boundary for EquipmentCompositionProductionTests.
// This preserves production Hero/ApplyEquipment/ApplyFashion/WeaponRig calls verbatim.
// It does not emulate or validate imported assets, animation, or fallback transitions.
using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private bool pilotHasGear, pilotHasFashion, pilotVisible;
        private BlenderPilotVisual blenderPilot;
        private static bool PilotStarterCompatible(ItemData item,ItemSlot slot) { return false; }
        private void ConfigureBlenderPilot() { blenderPilot=null;pilotVisible=false; }
        private void SetBlenderPilotVisible(bool visible)
        {
            if(visible)throw new InvalidOperationException("Equipment composition suite must keep optional Blender pilot disabled");
            pilotVisible=false;
        }
    }
    public sealed class BlenderPilotVisual
    {
        public bool Anchor(WeaponVisualAnchor anchor,out Vector3 position)
        {
            throw new InvalidOperationException("Disabled imported visual cannot supply procedural weapon anchors");
        }
    }
}
