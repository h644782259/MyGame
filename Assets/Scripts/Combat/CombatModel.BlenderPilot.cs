using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private BlenderPilotVisual blenderPilot;
        // This authored outfit represents only the exact unupgraded new-character kit.
        // Upgraded, dropped, mechanic-bearing or cosmetic gear keeps its real visuals.
        private static bool PilotStarterCompatible(ItemData item, ItemSlot slot)
        {
            if(item==null)return true;
            string name=slot==ItemSlot.Weapon?"初行长剑":slot==ItemSlot.Armor?"初行战衣":"初行护符";
            return item.slot==slot && item.name==name && item.level==1 && item.rarity==Rarity.Common &&
                item.upgradeLevel==0 && item.mechanic==EquipmentMechanic.None && !item.mechanicVariantUnlocked;
        }
        private bool pilotHasGear, pilotHasFashion, pilotAirborne, pilotCharging;
        private bool pilotVisible, pilotWasHurt, pilotOwnerDead;
        internal void SetBlenderPilotOwnerAlive(bool alive)
        {pilotOwnerDead=!alive;if(!alive)SetBlenderPilotVisible(false);}
        private float pilotHurtStarted=-10;
        private Renderer[] pilotHiddenRenderers;
        private bool[] pilotRendererStates;
        private void ConfigureBlenderPilot()
        {
            if (heroClass == HeroClass.Vanguard && BlenderPilotArt.Enabled)
                blenderPilot = BlenderPilotVisual.Create(transform);
        }
        private Vector3 pilotAcceptedWalkingWorld;
        private bool pilotWalkingKnown;
        private static bool PilotFinite(float value) { return !float.IsNaN(value)&&!float.IsInfinity(value); }
        private void RecordPilotWalking(Vector3 localDisplacement,float delta,float referenceSpeed,bool walking,bool airborne)
        {
            if(blenderPilot==null)return; // Default-off and other classes do no extra transform work.
            pilotWalkingKnown=PilotFinite(delta)&&delta>=0&&PilotFinite(referenceSpeed)&&referenceSpeed>0&&
                PilotFinite(localDisplacement.x)&&PilotFinite(localDisplacement.y)&&PilotFinite(localDisplacement.z);
            pilotAcceptedWalkingWorld=Vector3.zero;
            if(!pilotWalkingKnown || !walking || airborne)return;
            // SetLocomotion receives the owner's local accepted displacement before FaceAim.
            Transform owner=transform.parent;
            pilotAcceptedWalkingWorld=owner==null?localDisplacement:owner.TransformDirection(localDisplacement);
        }
        private bool PilotMovingBasicFacing()
        {
            if(!pilotWalkingKnown)return false;
            Transform owner=transform.parent;
            Vector3 local=owner==null?pilotAcceptedWalkingWorld:owner.InverseTransformDirection(pilotAcceptedWalkingWorld);
            if(!PilotFinite(local.x)||!PilotFinite(local.y)||!PilotFinite(local.z))return false;
            local.y=0;
            float extent=Mathf.Max(Mathf.Abs(local.x),Mathf.Abs(local.z));
            if(extent==0)return true; // Legal stop: keep the existing smoothed gait decay.
            local=new Vector3(local.x/extent,0,local.z/extent).normalized;
            return Mathf.Abs(local.x)<.1f && local.z>=-.05f;
        }
        private bool SampleBlenderPilot(bool acting, float progress, bool hurt)
        {
            if (blenderPilot == null) return false;
            if(hurt&&!pilotWasHurt)pilotHurtStarted=Time.time;
            pilotWasHurt=hurt;
            // Five pilot clips are deliberately bounded: ordinary forward movement,
            // base outfit, basic attack and representative skill 7 (earth rupture).
            bool supported = BlenderPilotArt.Enabled && !pilotHasGear && !pilotHasFashion &&
                !pilotAirborne && !pilotCharging && !dying && !pilotOwnerDead && locomotion.Landing <= 0 &&
                Mathf.Abs(locomotion.Side) < .1f && locomotion.Forward >= -.05f &&
                (!acting || actionBasic || locomotion.Speed <= .05f) && // Only basic attacks own a locomotion lower-body layer.
                (!acting || actionBasic || actionSkill == 7) &&
                (!acting || !actionBasic || locomotion.Speed<=.05f || PilotMovingBasicFacing());
            if (!supported) { SetBlenderPilotVisible(false); return false; }
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            try
            {
                blenderPilot.Sample(isolatedPreview?previewTime:Time.time, locomotion.Phase, locomotion.Speed, acting, actionBasic, progress, isolatedPreview?1:Time.time-pilotHurtStarted);
            }
            catch(System.Exception)
            {
                // An import/sample failure must not expose a partly composed skeleton.
                SetBlenderPilotVisible(false);
                return false;
            }
            SetBlenderPilotVisible(true);
            return true;
        }
        private void SetBlenderPilotVisible(bool visible)
        {
            if (blenderPilot == null) return;
            blenderPilot.gameObject.SetActive(visible);
            if (visible == pilotVisible) return;
            pilotVisible = visible;
            if (visible)
            {
                pilotHiddenRenderers = GetComponentsInChildren<Renderer>(true);
                pilotRendererStates = new bool[pilotHiddenRenderers.Length];
                for (int i=0;i<pilotHiddenRenderers.Length;i++)
                {
                    Renderer renderer=pilotHiddenRenderers[i];
                    pilotRendererStates[i]=renderer.enabled;
                    if (!renderer.transform.IsChildOf(blenderPilot.transform)) renderer.enabled=false;
                }
            }
            else if (pilotHiddenRenderers != null)
                for (int i=0;i<pilotHiddenRenderers.Length;i++)
                    if(pilotHiddenRenderers[i]!=null&&!pilotHiddenRenderers[i].transform.IsChildOf(blenderPilot.transform))
                        pilotHiddenRenderers[i].enabled=pilotRendererStates[i];
        }
    }
}
