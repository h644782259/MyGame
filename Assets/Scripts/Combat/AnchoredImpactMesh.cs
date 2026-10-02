using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Keep the real contact/root fixed. Certify entire faces, not just a few rays through them.
    internal static class AnchoredImpactMesh
    {
        private const int MaximumDepth=6,MaximumTriangles=4096,MaximumChecks=4096;
        // WorldTraversal samples every <= .18m. Inflation covers its half-step gap plus
        // the .04m damage LOS radius, so a passing swept disk encloses every face ray.
        private const float VisibilityMargin=.131f;
        internal static Mesh Create(Mesh source,Transform root,Vector3 offset,Vector3 scale,Quaternion rotation)
        {
            Vector3 origin=CombatFx.Flat(root.position);Vector3[] points=(Vector3[])source.vertices.Clone();
            for(int i=0;i<points.Length;i++)
            {
                Vector3 world=root.TransformPoint(offset+rotation*Vector3.Scale(points[i],scale));
                Vector3 clipped=CombatSight.BoundaryPoint(CombatSightKind.Area,origin,world);clipped.y=world.y;
                points[i]=clipped;
            }
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var sourceUv=source.uv;var original=source.triangles;int remainingChecks=MaximumChecks;
            for(int i=0;i<original.Length;i+=3)
            {
                int a=original[i],b=original[i+1],c=original[i+2];
                // Share the creation budget across source faces: hidden early faces must not
                // spend every query before later, visible identity features are considered.
                int allowance=System.Math.Min(remainingChecks,System.Math.Max(1,MaximumChecks/System.Math.Max(1,original.Length/3)));
                int remaining=allowance;
                Add(root,origin,points[a],points[b],points[c],sourceUv[a],sourceUv[b],sourceUv[c],0,ref remaining,vertices,uv,triangles);
                remainingChecks-=allowance-remaining;
            }
            var mesh=new Mesh{name=source.name+" / anchored cover clip",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Add(Transform root,Vector3 origin,Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc,int depth,ref int remainingChecks,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            if(triangles.Count>=MaximumTriangles*3||remainingChecks<=0)return;
            remainingChecks--;
            Vector3 center=(a+b+c)/3;
            float extent=Mathf.Max(CombatFx.Flat(a-center).magnitude,Mathf.Max(CombatFx.Flat(b-center).magnitude,CombatFx.Flat(c-center).magnitude));
            if(CombatSight.VisualFootprint(origin,center,extent+VisibilityMargin))
            {
                int first=vertices.Count;vertices.Add(root.InverseTransformPoint(a));vertices.Add(root.InverseTransformPoint(b));vertices.Add(root.InverseTransformPoint(c));uv.Add(ua);uv.Add(ub);uv.Add(uc);
                triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);return;
            }
            if(depth>=MaximumDepth)return; // Uncertified slivers are omitted, never painted across cover.
            float ab=CombatFx.Flat(a-b).sqrMagnitude,bc=CombatFx.Flat(b-c).sqrMagnitude,ca=CombatFx.Flat(c-a).sqrMagnitude;
            if(bc>ab&&bc>=ca){Vector3 old=a;a=b;b=c;c=old;Vector2 oldUv=ua;ua=ub;ub=uc;uc=oldUv;}
            else if(ca>ab&&ca>bc){Vector3 old=c;c=b;b=a;a=old;Vector2 oldUv=uc;uc=ub;ub=ua;ua=oldUv;}
            Vector3 mid=(a+b)*.5f;Vector2 midUv=new Vector2((ua.x+ub.x)*.5f,(ua.y+ub.y)*.5f);
            Add(root,origin,a,mid,c,ua,midUv,uc,depth+1,ref remainingChecks,vertices,uv,triangles);
            Add(root,origin,mid,b,c,midUv,ub,uc,depth+1,ref remainingChecks,vertices,uv,triangles);
        }
    }
}
