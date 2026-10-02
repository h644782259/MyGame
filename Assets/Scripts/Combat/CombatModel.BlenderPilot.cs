using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private BlenderPilotVisual blenderPilot;
        private bool pilotHasGear, pilotHasFashion, pilotAirborne, pilotCharging;
        private bool pilotVisible, pilotWasHurt;
        private float pilotHurtStarted=-10;
        private Renderer[] pilotHiddenRenderers;
        private bool[] pilotRendererStates;
        private void ConfigureBlenderPilot()
        {
            if (heroClass == HeroClass.Vanguard && BlenderPilotArt.Enabled)
                blenderPilot = BlenderPilotVisual.Create(transform);
        }
        private bool SampleBlenderPilot(bool acting, float progress, bool hurt)
        {
            if (blenderPilot == null) return false;
            if(hurt&&!pilotWasHurt)pilotHurtStarted=Time.time;
            pilotWasHurt=hurt;
            // Five pilot clips are deliberately bounded: ordinary forward movement,
            // base outfit, basic attack and representative skill 7 (earth rupture).
            bool supported = BlenderPilotArt.Enabled && !pilotHasGear && !pilotHasFashion &&
                !pilotAirborne && !pilotCharging && !dying && locomotion.Landing <= 0 &&
                Mathf.Abs(locomotion.Side) < .1f && locomotion.Forward >= -.05f &&
                (!acting || actionBasic || actionSkill == 7);
            SetBlenderPilotVisible(supported);
            if (!supported) return false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            blenderPilot.Sample(isolatedPreview?previewTime:Time.time, locomotion.Phase, locomotion.Speed, acting, actionBasic, progress, isolatedPreview?1:Time.time-pilotHurtStarted);
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
