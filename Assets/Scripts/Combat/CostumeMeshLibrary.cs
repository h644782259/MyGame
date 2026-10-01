using UnityEngine;
namespace Emberfall
{
    internal static class CostumeMeshLibrary
    {
        private static readonly Mesh[] meshes=new Mesh[3];
        public static Mesh Get(WingSilhouette style)
        {
            int index=(int)style;if(meshes[index]!=null)return meshes[index];
            var data=style==WingSilhouette.Feather?CostumeRecipes.Feather():style==WingSilhouette.Crystal?CostumeRecipes.Crystal():CostumeRecipes.Ring();
            var vertices=new Vector3[data.Positions.Length/3];
            for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(data.Positions[i*3],data.Positions[i*3+1],data.Positions[i*3+2]);
            var mesh=new Mesh{name="Shared costume "+style,vertices=vertices,triangles=data.Triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes[index]=mesh;return mesh;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){for(int i=0;i<meshes.Length;i++){if(meshes[i]!=null)Object.Destroy(meshes[i]);meshes[i]=null;}}
    }
    internal sealed class FashionOrbit : MonoBehaviour
    {
        private float angle;
        private Quaternion rest;
        private void Awake(){rest=transform.localRotation;}
        private void Update()
        {
            if(Time.deltaTime<=0)return;
            angle=Mathf.Repeat(angle+Time.deltaTime*16f,360f);
            transform.localRotation=rest*Quaternion.Euler(0,0,angle);
        }
    }
}
