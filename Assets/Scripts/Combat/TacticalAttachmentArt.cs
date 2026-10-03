using UnityEngine;
namespace Emberfall
{
    // Three independent body sockets. No gameplay state is stored here.
    internal sealed class TacticalAttachmentArt
    {
        internal static bool Enabled=true;
        private static readonly Mesh[] meshes=new Mesh[3];
        private static readonly Material[] materials=new Material[3];
        private static readonly bool[] attempted=new bool[3];
        private readonly MeshRenderer[] parts=new MeshRenderer[3];
        private static readonly string[] names={"SupplierBackpack","HuntBadge","SupportMantle"};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {for(int i=0;i<3;i++){if(meshes[i]!=null)Object.Destroy(meshes[i]);if(materials[i]!=null)Object.Destroy(materials[i]);meshes[i]=null;materials[i]=null;attempted[i]=false;}Enabled=true;}
        internal static TacticalAttachmentArt Create(EnemyController enemy)
        {
            var result=new TacticalAttachmentArt();if(!Enabled||enemy==null)return result;
            MeshFilter body=null;
            foreach(var filter in enemy.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null&&(filter.name=="Breastplate"||filter.name=="Slime Body"||filter.name=="Spirit Core")){body=filter;break;}
            if(body==null)return result; // Unmapped rigs retain the complete existing marker presentation.
            var bounds=body.sharedMesh.bounds;
            if(bounds.size.x<=0||bounds.size.y<=0||bounds.size.z<=0)return result;
            for(int i=0;i<3;i++)
            {
                if(!attempted[i])
                {
                    attempted[i]=true;var data=Resources.Load<TextAsset>("TacticalAttachments/"+names[i]);
                    meshes[i]=data==null?null:AuthoredActorMeshes.Decode(data.bytes,names[i]);
                    var shader=Shader.Find("Standard");
                    if(meshes[i]!=null&&shader!=null&&shader.isSupported)
                    {materials[i]=new Material(shader);materials[i].color=i==0?new Color(.92f,.64f,.12f):i==1?new Color(.93f,.25f,.12f):new Color(.12f,.72f,.84f);}
                }
                if(meshes[i]==null||materials[i]==null)continue;
                var obj=new GameObject("Tactical attachment / "+names[i]);obj.transform.SetParent(body.transform,false);
                // Coordinates are fractions of the real authored/procedural torso mesh; body scale,
                // crouch, knockdown, slime squash and boss scale remain inherited automatically.
                obj.transform.localScale=bounds.size;
                obj.transform.localPosition=bounds.center;
                obj.AddComponent<MeshFilter>().sharedMesh=meshes[i];
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[i];renderer.enabled=false;result.parts[i]=renderer;
            }
            return result;
        }
        internal bool Set(int slot,bool active)
        {var part=parts[slot];if(part==null)return false;part.enabled=Enabled&&active;return Enabled;}
        internal void Hide(){for(int i=0;i<3;i++)if(parts[i]!=null)parts[i].enabled=false;}
        internal void Destroy(){for(int i=0;i<3;i++)if(parts[i]!=null)Object.Destroy(parts[i].gameObject);}
    }
}
