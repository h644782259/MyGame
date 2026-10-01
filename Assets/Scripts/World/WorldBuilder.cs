using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Entire prototype scene is authored here so a fresh checkout needs no imported art.
    public static class WorldBuilder
    {
        public static GameObject Build(ZoneKind zone)
        {
            GameObject root = new GameObject(zone == ZoneKind.Wilderness ? "Windwhisper Fields" : "Fallen Star Sanctum");
            WorldResources resources = root.AddComponent<WorldResources>();
            bool dungeon = zone == ZoneKind.Dungeon;
            RenderSettings.ambientLight = dungeon ? new Color(.29f, .29f, .43f) : new Color(.48f, .55f, .59f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = dungeon ? new Color(.055f, .055f, .11f) : new Color(.1f, .18f, .24f);
            RenderSettings.fogDensity = dungeon ? .016f : .009f;

            GameObject sunlight = new GameObject("Sun");
            sunlight.transform.SetParent(root.transform);
            sunlight.transform.rotation = Quaternion.Euler(dungeon ? 48 : 42, -35, 0);
            Light light = sunlight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = dungeon ? new Color(.64f, .72f, 1) : new Color(1, .88f, .66f);
            light.intensity = dungeon ? 1.1f : 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .7f;
            if (dungeon) BuildDungeon(root.transform, resources); else BuildWilderness(root.transform, resources);
            return root;
        }

        private static void BuildWilderness(Transform parent, WorldResources r)
        {
            Material grass = r.Material(new Color(.18f, .32f, .28f));
            Material darkRock = r.Material(new Color(.14f, .2f, .25f));
            Material stone = r.Material(new Color(.37f, .45f, .43f));
            Material edge = r.Material(new Color(.22f, .29f, .3f));
            Material gold = r.Material(new Color(.76f, .57f, .28f));
            Material jade = r.Material(new Color(.24f, .91f, .77f), true);
            Primitive(parent, "Floating island", PrimitiveType.Cylinder, new Vector3(0, -1.2f, 0), new Vector3(49, 1.15f, 49), darkRock);
            Primitive(parent, "Moss rim", PrimitiveType.Cylinder, new Vector3(0, -.3f, 0), new Vector3(48, .35f, 48), edge);
            Primitive(parent, "Meadow", PrimitiveType.Cylinder, new Vector3(0, -.08f, 0), new Vector3(46.5f, .08f, 46.5f), grass);

            System.Random random = new System.Random(32019);
            for (int i = 0; i < 74; i++)
            {
                float angle = i * 2.399963f;
                float radius = 20.8f + (float)random.NextDouble() * 2.1f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                if (i % 3 == 0) Tree(parent, r, p, .85f + (float)random.NextDouble() * .6f, i);
                else Rock(parent, r, p, .55f + (float)random.NextDouble() * 1.2f, i);
            }
            // Broken paving stones guide the player from camp to the gate.
            for (int i = 0; i < 17; i++)
            {
                GameObject tile = Primitive(parent, "Ancient path", PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(i * .9f) * .45f, .015f, -9 + i * 1.18f), new Vector3(1.4f + (i % 3) * .2f, .06f, .87f), stone);
                tile.transform.rotation = Quaternion.Euler(0, (i * 31) % 24 - 12, 0);
            }
            // Scenic ruins sit at the edge, leaving the fighting area unobstructed.
            for (int i = 0; i < 5; i++)
            {
                float x = -17 + i * 8.5f;
                float z = 15.8f - Mathf.Abs(x) * .13f;
                Pillar(parent, r, new Vector3(x, 0, z), 2.2f + i % 3, false);
            }
            for (int i = 0; i < 65; i++)
            {
                Vector3 p = new Vector3((float)random.NextDouble() * 39 - 19.5f, .03f, (float)random.NextDouble() * 37 - 18.5f);
                if (p.magnitude > 20 || Mathf.Abs(p.x) < 2) continue;
                Material flowers = r.Material(i % 2 == 0 ? new Color(.65f,.58f,.8f) : new Color(.63f,.76f,.53f));
                Primitive(parent, "Wildflowers", PrimitiveType.Sphere, p + Vector3.up * .08f, new Vector3(.16f,.19f,.16f), flowers);
                Primitive(parent, "Wildflowers", PrimitiveType.Sphere, p + new Vector3(.23f,.03f,.11f), new Vector3(.12f,.14f,.12f), flowers);
            }

            Primitive(parent, "Camp platform", PrimitiveType.Cylinder, new Vector3(0, .01f, -10), new Vector3(7, .05f, 7), stone);
            Ring(parent, r, "Camp inlay", new Vector3(0, .074f, -10), 3.1f, .035f, gold, false);
            BuildCampfire(parent, r, new Vector3(-3.7f, 0, -11));
            Tent(parent, r, new Vector3(4.5f, 0, -13.5f));
            Label(parent, "CAMP", new Vector3(0, .13f, -13.9f), .13f, new Color(.74f, .82f, .73f), true);
            Portal(parent, r, new Vector3(0, 0, 11), jade);
            Label(parent, "FALLEN STAR", new Vector3(0, 4.65f, 11), .10f, new Color(.65f, 1, .87f), false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 31, -3 - i % 4, Mathf.Sin(angle) * 31);
                Rock(parent, r, p, 2 + i % 3, i);
            }
        }

        private static void BuildDungeon(Transform parent, WorldResources r)
        {
            Material baseStone = r.Material(new Color(.095f, .11f, .17f));
            Material slab = r.Material(new Color(.21f, .23f, .31f));
            Material border = r.Material(new Color(.31f, .31f, .42f));
            Material rune = r.Material(new Color(.34f, .62f, .98f), true);
            Primitive(parent, "Sanctum base", PrimitiveType.Cylinder, new Vector3(0, -.7f, 0), new Vector3(42, .7f, 42), baseStone);
            Primitive(parent, "Sanctum floor", PrimitiveType.Cylinder, new Vector3(0, -.09f, 0), new Vector3(40, .09f, 40), slab);
            Ring(parent, r, "Outer sigil", new Vector3(0,.035f,0), 18.6f, .07f, rune, false);
            Ring(parent, r, "Inner sigil", new Vector3(0,.04f,0), 7, .035f, rune, false);
            Ring(parent, r, "Dais border", new Vector3(0,.045f,0), 5.6f, .08f, border, false);
            for (int x = -4; x <= 4; x++)
                for (int z = -4; z <= 4; z++)
                {
                    if (x * x + z * z > 23) continue;
                    Primitive(parent, "Floor joint", PrimitiveType.Cube, new Vector3(x * 4, .006f, z * 4), new Vector3(3.94f,.018f,3.94f), (x+z)%2 == 0 ? slab : r.Material(new Color(.195f,.215f,.29f)));
                }
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector3 p = new Vector3(Mathf.Cos(angle) * 19.5f, 0, Mathf.Sin(angle) * 19.5f);
                Pillar(parent, r, p, 4.5f + (i % 2), true);
                Crystal(parent, r, p + Vector3.up * (5f + i % 2), .7f, rune);
                Vector3 mark = new Vector3(Mathf.Cos(angle) * 17.8f, .035f, Mathf.Sin(angle) * 17.8f);
                GameObject glyph = Primitive(parent, "Boundary rune", PrimitiveType.Cube, mark, new Vector3(.12f,.03f,1.1f), rune);
                glyph.transform.rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 5.8f;
                Primitive(parent, "Lost wall", PrimitiveType.Cube, new Vector3(x, 2.8f, 20.8f), new Vector3(5.5f, 5.6f + i % 2, .7f), baseStone);
            }
            Label(parent, "THE FALLEN SANCTUM", new Vector3(0,.10f,0), .18f, new Color(.46f,.55f,.72f), true);
            Portal(parent, r, new Vector3(0, 0, -16), r.Material(new Color(.58f,.38f,.94f), true));
            PointLight(parent, new Vector3(-9,4,1), new Color(.18f,.48f,1), 2, 17);
            PointLight(parent, new Vector3(9,4,7), new Color(.7f,.25f,1), 1.8f, 16);
        }

        private static void Tree(Transform parent, WorldResources r, Vector3 p, float size, int seed)
        {
            Material trunk = r.Material(new Color(.24f,.22f,.21f));
            Material leaves = r.Material(seed % 2 == 0 ? new Color(.12f,.29f,.29f) : new Color(.2f,.37f,.32f));
            Primitive(parent, "Tree trunk", PrimitiveType.Cylinder, p + Vector3.up * size, new Vector3(.34f, size, .34f), trunk);
            for (int j = 0; j < 3; j++)
                Cone(parent, r, "Pine crown", p + Vector3.up * (1.2f + j * .85f) * size, (1.2f - j * .22f) * size, 1.8f * size, leaves, 7);
        }

        private static void Rock(Transform parent, WorldResources r, Vector3 p, float scale, int seed)
        {
            GameObject rock = Primitive(parent, "Weathered rock", PrimitiveType.Cube, p + Vector3.up * scale * .35f,
                new Vector3(scale * 1.3f, scale, scale * .9f), r.Material(seed % 2 == 0 ? new Color(.28f,.34f,.37f) : new Color(.32f,.4f,.39f)));
            rock.transform.rotation = Quaternion.Euler(seed % 27, seed * 67 % 360, seed % 18);
        }

        private static void Pillar(Transform parent, WorldResources r, Vector3 p, float height, bool dungeon)
        {
            Material stone = r.Material(dungeon ? new Color(.24f,.25f,.35f) : new Color(.44f,.48f,.43f));
            Primitive(parent, "Column base", PrimitiveType.Cube, p + Vector3.up * .25f, new Vector3(1.4f,.5f,1.4f), stone);
            Primitive(parent, "Column", PrimitiveType.Cylinder, p + Vector3.up * height * .5f, new Vector3(.85f,height*.5f,.85f), stone);
            Primitive(parent, "Capital", PrimitiveType.Cube, p + Vector3.up * height, new Vector3(1.3f,.32f,1.3f), stone);
            Material band = r.Material(dungeon ? new Color(.31f,.57f,.8f) : new Color(.67f,.59f,.39f), dungeon);
            Primitive(parent, "Column band", PrimitiveType.Cylinder, p + Vector3.up * (height-.5f), new Vector3(.94f,.1f,.94f), band);
        }

        private static void Portal(Transform parent, WorldResources r, Vector3 p, Material glow)
        {
            Material stone = r.Material(new Color(.27f,.34f,.38f));
            Primitive(parent, "Gate plinth", PrimitiveType.Cylinder, p + Vector3.up * .05f, new Vector3(5,.08f,4), stone);
            Ring(parent, r, "Gate frame", p + Vector3.up * 2.1f, 1.85f, .2f, stone, true);
            GameObject inner = Ring(parent, r, "Gate light", p + new Vector3(0,2.1f,-.05f), 1.63f, .055f, glow, true);
            inner.AddComponent<WorldMotion>().spin = new Vector3(0,0,16);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                Vector3 point = p + new Vector3(Mathf.Cos(angle)*1.85f, 2.1f+Mathf.Sin(angle)*1.85f, -.09f);
                Crystal(parent,r,point,.23f,glow);
            }
            Crystal(parent,r,p+new Vector3(0,2.2f,0),.6f,glow);
            PointLight(parent, p + new Vector3(0,2,0), glow.color, 2.5f, 9);
            for (int i=0;i<11;i++)
            {
                float angle=i*2.3999f;
                GameObject mote=Primitive(parent,"Starlight",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(angle)*1.2f,.5f+(i%5)*.65f,Mathf.Sin(angle)*.3f),Vector3.one*.055f,glow);
                WorldMotion motion=mote.AddComponent<WorldMotion>(); motion.bob=.18f; motion.speed=1.5f+i*.1f;
            }
        }

        private static void Crystal(Transform parent, WorldResources r, Vector3 p, float size, Material material)
        {
            GameObject crystal = new GameObject("Aether crystal"); crystal.transform.SetParent(parent); crystal.transform.position=p;
            Cone(crystal.transform,r,"Crystal upper",Vector3.zero,.42f*size,1.1f*size,material,5,true);
            GameObject bottom=Cone(crystal.transform,r,"Crystal lower",Vector3.zero,.42f*size,.7f*size,material,5,true);
            bottom.transform.localRotation=Quaternion.Euler(180,0,0);
            WorldMotion motion=crystal.AddComponent<WorldMotion>(); motion.bob=.12f; motion.spin=new Vector3(0,30,0);
        }

        private static void BuildCampfire(Transform parent, WorldResources r, Vector3 p)
        {
            Material wood=r.Material(new Color(.3f,.22f,.17f));
            for(int i=0;i<3;i++) { GameObject log=Primitive(parent,"Firewood",PrimitiveType.Cylinder,p+new Vector3(0,.17f,0),new Vector3(.23f,.8f,.23f),wood); log.transform.rotation=Quaternion.Euler(90,i*60,0); }
            Cone(parent,r,"Amber flame",p+Vector3.up*.24f,.4f,1.1f,r.Material(new Color(1,.38f,.12f),true),6);
            Cone(parent,r,"Golden flame",p+Vector3.up*.25f,.25f,.72f,r.Material(new Color(1,.82f,.28f),true),5);
            PointLight(parent,p+Vector3.up*1.3f,new Color(1,.52f,.19f),2,8);
            for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Rock(parent,r,p+new Vector3(Mathf.Cos(a)*.7f,0,Mathf.Sin(a)*.7f),.28f,i); }
        }

        private static void Tent(Transform parent, WorldResources r, Vector3 p)
        {
            Material cloth=r.Material(new Color(.29f,.47f,.48f));
            for(int i=0;i<2;i++) { GameObject slope=Primitive(parent,"Camp tent",PrimitiveType.Cube,p+new Vector3(i==0?-.62f:.62f,1,0),new Vector3(.08f,2.5f,2.5f),cloth); slope.transform.rotation=Quaternion.Euler(0,0,i==0?-30:30); }
            Primitive(parent,"Supply crate",PrimitiveType.Cube,p+new Vector3(2,.45f,0),new Vector3(.8f,.9f,.9f),r.Material(new Color(.44f,.31f,.19f)));
        }

        private static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material)
        {
            GameObject go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent); go.transform.localPosition=p; go.transform.localScale=scale;
            Collider collider=go.GetComponent<Collider>(); if(collider!=null) { collider.enabled=false; Object.Destroy(collider); }
            go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }

        private static GameObject Cone(Transform parent,WorldResources resources,string name,Vector3 p,float radius,float height,Material material,int sides,bool local=false)
        {
            List<Vector3> vertices=new List<Vector3>(); List<int> triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                int start=vertices.Count;
                vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); vertices.Add(Vector3.up*height); vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
                start=vertices.Count; vertices.Add(Vector3.zero); vertices.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); vertices.Add(new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            Mesh mesh=new Mesh(); mesh.name=name; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); resources.Own(mesh);
            GameObject go=new GameObject(name); go.transform.SetParent(parent); if(local)go.transform.localPosition=p;else go.transform.position=p;
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material; return go;
        }

        private static GameObject Ring(Transform parent,WorldResources r,string name,Vector3 center,float radius,float thickness,Material material,bool vertical)
        {
            GameObject go=new GameObject(name); go.transform.SetParent(parent); go.transform.position=center;
            LineRenderer line=go.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.loop=true; line.positionCount=72; line.widthMultiplier=thickness; line.sharedMaterial=material;
            line.numCornerVertices=2; line.numCapVertices=2;
            for(int i=0;i<72;i++) { float a=i*Mathf.PI*2/72; line.SetPosition(i,vertical?new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0):new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); }
            return go;
        }

        private static void PointLight(Transform parent,Vector3 p,Color color,float intensity,float range)
        { GameObject go=new GameObject("Magic light"); go.transform.SetParent(parent); go.transform.position=p; Light light=go.AddComponent<Light>(); light.type=LightType.Point; light.color=color; light.intensity=intensity; light.range=range; }

        private static void Label(Transform parent,string value,Vector3 p,float size,Color color,bool floor)
        {
            GameObject go=new GameObject(value); go.transform.SetParent(parent); go.transform.position=p;
            TextMesh text=go.AddComponent<TextMesh>(); text.text=value; text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; text.fontSize=64; text.characterSize=size; text.color=color;
            go.transform.rotation=Quaternion.Euler(floor?90:18,0,0);
        }

        public static GameObject MakeLootBeacon(Vector3 position,Color color)
        {
            GameObject root=new GameObject("Loot acquired"); root.transform.position=position;
            WorldResources r=root.AddComponent<WorldResources>(); Material glow=r.Material(color,true);
            Primitive(root.transform,"Loot beam",PrimitiveType.Cylinder,new Vector3(0,1.3f,0),new Vector3(.035f,1.3f,.035f),glow);
            Crystal(root.transform,r,position+Vector3.up*.6f,.35f,glow);
            Ring(root.transform,r,"Loot ring",position+Vector3.up*.06f,.65f,.055f,glow,false);
            return root;
        }
    }

    public sealed class WorldResources : MonoBehaviour
    {
        private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        public Material Material(Color color,bool emissive=false)
        {
            string key=ColorUtility.ToHtmlStringRGBA(color)+(emissive?"E":"S");
            Material material;
            if(materials.TryGetValue(key,out material))return material;
            Shader shader=Shader.Find(emissive?"Unlit/Color":"Standard");
            if(shader==null) shader=Shader.Find("Sprites/Default");
            material=new Material(shader); material.color=color;
            if(material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",.12f);
            materials.Add(key,material); return material;
        }
        public void Own(Mesh mesh) { meshes.Add(mesh); }
        private void OnDestroy() { foreach(Material material in materials.Values)Destroy(material);foreach(Mesh mesh in meshes)Destroy(mesh); }
    }

    public sealed class WorldMotion : MonoBehaviour
    {
        public Vector3 spin;
        public float bob;
        public float speed=1;
        private Vector3 origin;
        private void Start() { origin=transform.localPosition; }
        private void Update()
        {
            transform.Rotate(spin*Time.unscaledDeltaTime,Space.Self);
            if(bob>0)transform.localPosition=origin+Vector3.up*Mathf.Sin(Time.unscaledTime*speed+origin.x)*bob;
        }
    }
}
