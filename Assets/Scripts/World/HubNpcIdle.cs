using UnityEngine;
namespace Emberfall
{
    // Three fixed NPC rigs: no spawned effects, physics, audio, or per-frame allocations.
    internal sealed class HubNpcIdle : MonoBehaviour
    {
        private Transform arm,otherArm,dial;private int role;private float age,angle;
        public void Initialize(int kind,Transform right,Transform left,Transform ornament)
        {role=kind;arm=right;otherArm=left;dial=ornament;
            // Establish the tool/working silhouette before the first Update (including paused entry).
            if(arm!=null)arm.localRotation=Quaternion.Euler(role==1?-38:role==2?-35:-18,0,-8);
            if(otherArm!=null)otherArm.localRotation=Quaternion.Euler(role==2?-28:role==1?-12:0,0,8);}
        private void Update()
        {
            if(Time.deltaTime<=0)return;age+=Time.deltaTime;
            if(arm!=null)
            {
                float swing=role==1?Mathf.Pow(Mathf.Max(0,Mathf.Sin(age*2.2f)),3)*-58:role==0?Mathf.Sin(age*1.3f)*14-18:Mathf.Sin(age*.8f)*8-35;
                arm.localRotation=Quaternion.Euler(swing,role==0?Mathf.Sin(age*.65f)*14:0,-8);
            }
            if(otherArm!=null)otherArm.localRotation=Quaternion.Euler(role==2?-28:role==1?-12:Mathf.Sin(age*.9f)*6,0,8);
            if(dial!=null){angle=Mathf.Repeat(angle+Time.deltaTime*18,360);dial.localRotation=Quaternion.Euler(70,angle,0);}
        }
    }
}
