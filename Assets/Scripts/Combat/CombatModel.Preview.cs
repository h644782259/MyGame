using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform previewOrbit;
        private Quaternion previewOrbitRest,previewDecorationRest;
        // Only the isolated mannequin opts in. No action dispatch, controller, random draw,
        // damage, leases or world clock is used while sampling a presentation pose.
        public void ConfigurePreview()
        {
            isolatedPreview=true;previewTime=0;
            previewOrbit=fashionWings==null?null:fashionWings.Find("Mechanical star-ring orbit");
            if(previewOrbit!=null)previewOrbitRest=previewOrbit.localRotation;
            if(decoration!=null)previewDecorationRest=decoration.localRotation;
        }
        public void SamplePreview(float time,CollectionPreviewAction action,float progress,float mechanicalYaw=float.NaN)
        {
            if(!isolatedPreview||!isHero||float.IsNaN(time)||float.IsInfinity(time)||float.IsNaN(progress)||float.IsInfinity(progress))return;
            previewTime=time;
            actionBasic=action==CollectionPreviewAction.Attack;
            actionSkill=heroClass==HeroClass.Summoner?2:heroClass==HeroClass.Arcanist?1:heroClass==HeroClass.Ranger?2:0;
            actionDuration=action==CollectionPreviewAction.Idle?0:1;
            actionAge=Mathf.Clamp01(progress);
            AnimateHero(0,0,false,0);
            if(decoration!=null)decoration.localRotation=previewDecorationRest*Quaternion.Euler(0,time*42,0);
            // The host owns a separate wrapped angle: its 16-degree orbit must not
            // inherit the 120s breathing/cloth clock reset. Framing uses static time=0.
            if(previewOrbit!=null)previewOrbit.localRotation=previewOrbitRest*Quaternion.Euler(0,0,float.IsNaN(mechanicalYaw)?time*16:mechanicalYaw);
            if(tailoredCloth!=null)tailoredCloth.SamplePreview(time,action==CollectionPreviewAction.Idle?0:Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI));
        }
    }
}
