using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private readonly VisualMotionEnvelope visualMotion = new VisualMotionEnvelope();
        private Transform[] recoveryJoints;
        private Quaternion[] recoveryRotations;
        private float recoveryAge=1, previousVisualYaw;
        private bool visualYawReady;
        private void BeginVisualRecovery()
        {
            if(!isHero||spine==null)return;
            if(recoveryJoints==null)
            {
                recoveryJoints=new[]{spine,headRig,leftArm,rightArm,leftElbow,rightElbow,swordRig,staffRig,bowRig};
                recoveryRotations=new Quaternion[recoveryJoints.Length];
            }
            for(int i=0;i<recoveryJoints.Length;i++)if(recoveryJoints[i]!=null)recoveryRotations[i]=recoveryJoints[i].localRotation;
            recoveryAge=0;
        }
        private void ApplyVisualRecovery(float delta)
        {
            if(recoveryJoints==null||recoveryAge>=.12f)return;
            recoveryAge+=Mathf.Max(0,delta);
            float weight=VisualMotionEnvelope.RecoveryWeight(recoveryAge);
            for(int i=0;i<recoveryJoints.Length;i++)if(recoveryJoints[i]!=null)
                recoveryJoints[i].localRotation=Quaternion.Slerp(recoveryJoints[i].localRotation,recoveryRotations[i],weight);
            if(bowRig!=null)
            {
                // Preserve grip and drawing-hand contacts while the upper-body pose settles.
                bowRig.localPosition=new Vector3(0,-.23f,.04f)-bowRig.localRotation*WeaponAnchorLocal(WeaponVisualAnchor.BowGrip);
                AimArm(rightArm,rightElbow,spine.InverseTransformPoint(bowRig.TransformPoint(WeaponAnchorLocal(WeaponVisualAnchor.BowNock))),new Vector3(1,-.2f,-.2f));
            }
        }
        private void AdvanceVisualMotion(float delta)
        {
            float yaw=transform.parent!=null?transform.parent.eulerAngles.y:transform.eulerAngles.y;
            float change=visualYawReady?Mathf.DeltaAngle(previousVisualYaw,yaw):0;
            previousVisualYaw=yaw;visualYawReady=true;
            visualMotion.Advance(change,locomotion.Side,locomotion.Forward,delta);
        }
    }
}
