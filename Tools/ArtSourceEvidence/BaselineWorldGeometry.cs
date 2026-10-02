// Read-only visual comparison baseline from 03422ab. Never runtime compiled.
using UnityEngine;namespace Emberfall {public static partial class WorldBuilder {
public static void BaselineTree(Transform parent, WorldResources r, Vector3 p, float size, int seed)
        {
            if (p.sqrMagnitude < 22f*22f) WorldTraversal.AddCircle(p, .22f);
            Material trunk = r.Material(new Color(.24f,.22f,.21f),false,VisualSurface.Wood);
            Material leaves = r.Material(seed % 2 == 0 ? new Color(.12f,.29f,.29f) : new Color(.2f,.37f,.32f),false,VisualSurface.Foliage);
            Primitive(parent, "Tree trunk", PrimitiveType.Cylinder, p + Vector3.up * size, new Vector3(.34f, size, .34f), trunk);
            for (int j = 0; j < 3; j++)
            {
                GameObject crown = Primitive(parent, "Rounded evergreen crown", PrimitiveType.Sphere,
                    p + Vector3.up * (1.65f + j * .73f) * size,
                    new Vector3(2.2f-j*.48f,1.75f-j*.17f,1.9f-j*.42f)*size, leaves,cameraOccluder:true);
                crown.GetComponent<MeshFilter>().sharedMesh = ProceduralVisuals.WeatheredRock;
                crown.transform.localRotation = Quaternion.Euler(0, seed*31f+j*57f, j%2==0 ? 7f : -7f);
            }
        }
public static void BaselineRoof(Transform root,WorldResources r){var roof=r.Material(new Color(.4f,.17f,.12f));Primitive(root,"Workshop sloping eave",PrimitiveType.Cube,Vector3.up*4,new Vector3(6.2f,.45f,6.2f),roof,cameraOccluder:true);Primitive(root,"Kiln chimney",PrimitiveType.Cube,new Vector3(1.5f,4.1f,1.2f),new Vector3(.8f,2,.8f),roof,cameraOccluder:true);}
}}