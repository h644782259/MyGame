using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Keep the real contact/root fixed. Only cosmetic vertices are clipped to its visible region.
    internal static class AnchoredImpactMesh
    {
        internal static Mesh Create(Mesh source,Transform root,Vector3 offset,Vector3 scale,Quaternion rotation)
        {
            Vector3 origin=CombatFx.Flat(root.position);Vector3[] vertices=(Vector3[])source.vertices.Clone();
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 local=offset+rotation*Vector3.Scale(vertices[i],scale),world=root.TransformPoint(local);
                Vector3 clipped=CombatSight.BoundaryPoint(CombatSightKind.Area,origin,world);clipped.y=world.y;
                vertices[i]=root.InverseTransformPoint(clipped);
            }
            var triangles=new List<int>();int[] original=source.triangles;
            for(int i=0;i<original.Length;i+=3)
            {
                Vector3 a=root.TransformPoint(vertices[original[i]]),b=root.TransformPoint(vertices[original[i+1]]),c=root.TransformPoint(vertices[original[i+2]]);
                if(!CombatSight.Area(origin,(a+b+c)/3)||!CombatSight.Area(origin,(a+b)*.5f)||!CombatSight.Area(origin,(b+c)*.5f)||!CombatSight.Area(origin,(c+a)*.5f))continue;
                triangles.Add(original[i]);triangles.Add(original[i+1]);triangles.Add(original[i+2]);
            }
            var mesh=new Mesh{name=source.name+" / anchored cover clip",vertices=vertices,uv=source.uv,triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
