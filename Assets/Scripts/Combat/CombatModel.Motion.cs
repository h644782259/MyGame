using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private readonly LocomotionPoseState locomotion = new LocomotionPoseState();
        private EnemyPosePhase enemyActionPhase;
        private float enemyActionProgress;
        public void SetLocomotion(Vector3 localDisplacement,float delta,float referenceSpeed,bool walking,bool airborne=false,float jumpProgress=0)
        { locomotion.Advance(localDisplacement.x,localDisplacement.z,delta,referenceSpeed,walking,airborne,jumpProgress); }
        public void ResetLocomotion() { locomotion.Reset(); }
        public void SetEnemyAttackPose(EnemyPosePhase state,float progress)
        { enemyActionPhase=state;enemyActionProgress=Mathf.Clamp01(progress); }
        private void ApplyHeroLocomotion()
        {
            float stride=Mathf.Sin(locomotion.Phase),jump=locomotion.JumpTuck,land=locomotion.Landing;
            float forward=stride*locomotion.Forward,side=stride*locomotion.Side;
            leftLeg.localRotation=Quaternion.Euler(forward*30-jump*30-land*12,0,-2+side*24);
            rightLeg.localRotation=Quaternion.Euler(-forward*30-jump*20-land*12,0,2-side*24);
            leftKnee.localRotation=Quaternion.Euler(Mathf.Max(0,-forward)*42+jump*62+land*25,0,0);
            rightKnee.localRotation=Quaternion.Euler(Mathf.Max(0,forward)*42+jump*52+land*25,0,0);
            pelvis.localPosition+=new Vector3(locomotion.Side*.025f,-land*.065f,0);
            spine.localRotation*=Quaternion.Euler(locomotion.CloakPitch*.16f,0,-locomotion.Side*5);
            cloak.localRotation=Quaternion.Euler(8+locomotion.CloakPitch+jump*8,locomotion.CloakSide,side*3);
            if(tailoredCloth!=null)tailoredCloth.SetInertia(locomotion.CloakPitch,locomotion.CloakSide);
        }
    }
}
