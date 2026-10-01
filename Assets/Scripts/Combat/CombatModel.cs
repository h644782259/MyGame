using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Each model owns its palette. Materials are reused across all of its parts.
    public sealed class CombatModel : MonoBehaviour
    {
        private readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        private Transform leftLeg, rightLeg, leftArm, rightArm, body, decoration;
        private bool slime, floating;
        private float phase;

        public static CombatModel Hero(Transform parent, HeroClass hero)
        {
            CombatModel model = Create(parent);
            model.BuildHero(hero);
            return model;
        }

        public static CombatModel Enemy(Transform parent, EnemyKind kind, bool boss)
        {
            CombatModel model = Create(parent);
            model.BuildEnemy(kind, boss);
            return model;
        }

        private static CombatModel Create(Transform parent)
        {
            GameObject obj = new GameObject("Character Model");
            obj.transform.SetParent(parent, false);
            CombatModel model = obj.AddComponent<CombatModel>();
            model.phase = Random.value * 6.28f;
            return model;
        }

        private Material Mat(Color color)
        {
            Material material;
            if (palette.TryGetValue(color, out material)) return material;
            material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", .28f);
            palette.Add(color, material);
            return material;
        }

        private Transform Part(string name, PrimitiveType shape, Vector3 position, Vector3 size, Color color, Transform parent = null)
        {
            GameObject obj = GameObject.CreatePrimitive(shape);
            obj.name = name;
            obj.transform.SetParent(parent == null ? transform : parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return obj.transform;
        }

        private Transform Joint(string name, Vector3 at)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = at;
            return obj.transform;
        }

        private void Humanoid(Color skin, Color cloth, Color armor, float bulk)
        {
            Color boot = new Color(.12f, .15f, .21f);
            body = Part("Breastplate", PrimitiveType.Capsule, new Vector3(0, 1.22f, 0), new Vector3(.75f * bulk, .49f, .48f * bulk), cloth);
            Part("Belt", PrimitiveType.Cube, new Vector3(0, .88f, .02f), new Vector3(.66f * bulk, .13f, .49f), boot);
            Part("Buckle", PrimitiveType.Cube, new Vector3(0, .88f, .29f), new Vector3(.16f, .14f, .06f), armor);
            Part("Head", PrimitiveType.Sphere, new Vector3(0, 1.97f, 0), new Vector3(.51f, .56f, .49f), skin);
            Part("Eyes L", PrimitiveType.Sphere, new Vector3(-.105f, 2f, .221f), new Vector3(.06f, .075f, .035f), boot);
            Part("Eyes R", PrimitiveType.Sphere, new Vector3(.105f, 2f, .221f), new Vector3(.06f, .075f, .035f), boot);
            leftLeg = Joint("Left Hip", new Vector3(-.21f, .83f, 0));
            rightLeg = Joint("Right Hip", new Vector3(.21f, .83f, 0));
            foreach (Transform leg in new[] { leftLeg, rightLeg })
            {
                Part("Trouser", PrimitiveType.Capsule, new Vector3(0, -.29f, 0), new Vector3(.23f, .29f, .24f), cloth, leg);
                Part("Boot", PrimitiveType.Cube, new Vector3(0, -.66f, .075f), new Vector3(.28f, .25f, .42f), boot, leg);
            }
            leftArm = Joint("Left Shoulder", new Vector3(-.47f * bulk, 1.57f, 0));
            rightArm = Joint("Right Shoulder", new Vector3(.47f * bulk, 1.57f, 0));
            foreach (Transform arm in new[] { leftArm, rightArm })
            {
                Part("Pauldrons", PrimitiveType.Sphere, Vector3.zero, new Vector3(.42f, .32f, .45f), armor, arm);
                Part("Sleeve", PrimitiveType.Capsule, new Vector3(0, -.28f, 0), new Vector3(.23f, .26f, .24f), cloth, arm);
                Part("Glove", PrimitiveType.Sphere, new Vector3(0, -.52f, .04f), new Vector3(.24f, .26f, .25f), skin, arm);
            }
        }

        private void Cape(Color color)
        {
            GameObject obj = new GameObject("Tailored Cloak");
            obj.transform.SetParent(transform, false);
            Mesh mesh = new Mesh();
            mesh.name = "Cloak Mesh";
            mesh.vertices = new[] {
                new Vector3(-.32f,1.66f,-.23f), new Vector3(.32f,1.66f,-.23f),
                new Vector3(-.51f,.42f,-.45f), new Vector3(.51f,.42f,-.45f),
                new Vector3(-.32f,1.66f,-.30f), new Vector3(.32f,1.66f,-.30f),
                new Vector3(-.51f,.42f,-.52f), new Vector3(.51f,.42f,-.52f)
            };
            mesh.triangles = new[] { 0,2,1,1,2,3,4,5,6,5,7,6,0,4,2,4,6,2,1,3,5,5,3,7,2,6,3,3,6,7,0,1,4,1,5,4 };
            mesh.RecalculateNormals();
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
            obj.AddComponent<OwnedCombatMesh>().Value = mesh;
        }

        private void BuildHero(HeroClass hero)
        {
            Color accent = GameBalance.ClassColor(hero);
            Color skin = new Color(.94f,.76f,.59f);
            Color steel = new Color(.64f,.75f,.85f);
            Humanoid(skin, accent * .62f, hero == HeroClass.Vanguard ? steel : accent, hero == HeroClass.Vanguard ? 1.15f : 1f);
            Cape(accent * .48f);
            if (hero == HeroClass.Vanguard)
            {
                Part("Helmet", PrimitiveType.Sphere, new Vector3(0,2.14f,-.03f), new Vector3(.57f,.4f,.52f), steel);
                Part("Helmet Crest", PrimitiveType.Cube, new Vector3(0,2.38f,-.04f), new Vector3(.1f,.31f,.43f), accent);
                Part("Nose Guard", PrimitiveType.Cube, new Vector3(0,2.05f,.265f), new Vector3(.06f,.24f,.055f), steel);
                Transform shield = Part("Round Shield", PrimitiveType.Cylinder, new Vector3(-.14f,-.39f,.16f), new Vector3(.72f,.075f,.72f), accent * .6f, leftArm);
                shield.localRotation = Quaternion.Euler(90,0,0);
                Part("Shield Boss", PrimitiveType.Sphere, new Vector3(-.14f,-.39f,.27f), new Vector3(.24f,.24f,.12f), steel, leftArm);
                Part("Sword Handle", PrimitiveType.Cylinder, new Vector3(0,-.53f,.11f), new Vector3(.1f,.2f,.1f), new Color(.19f,.16f,.15f), rightArm);
                Part("Sword Guard", PrimitiveType.Cube, new Vector3(0,-.32f,.11f), new Vector3(.4f,.085f,.12f), accent, rightArm);
                Part("Silver Blade", PrimitiveType.Cube, new Vector3(0,.2f,.11f), new Vector3(.14f,.95f,.055f), steel, rightArm);
                Part("Blade Tip", PrimitiveType.Sphere, new Vector3(0,.68f,.11f), new Vector3(.14f,.2f,.055f), steel, rightArm);
            }
            else if (hero == HeroClass.Arcanist)
            {
                Part("Hat Brim", PrimitiveType.Cylinder, new Vector3(0,2.21f,0), new Vector3(.88f,.05f,.88f), accent * .6f);
                Part("Wizard Hat", PrimitiveType.Capsule, new Vector3(0,2.38f,-.045f), new Vector3(.43f,.3f,.43f), accent * .48f);
                Part("Hat Gem", PrimitiveType.Sphere, new Vector3(0,2.3f,.24f), new Vector3(.15f,.2f,.1f), new Color(.55f,.94f,1f));
                Part("Staff", PrimitiveType.Cylinder, new Vector3(0,-.22f,.15f), new Vector3(.085f,.98f,.085f), new Color(.46f,.29f,.19f), rightArm);
                Part("Staff Gold", PrimitiveType.Sphere, new Vector3(0,.63f,.15f), new Vector3(.31f,.21f,.31f), new Color(.95f,.76f,.3f), rightArm);
                decoration = Part("Arcane Crystal", PrimitiveType.Cube, new Vector3(0,.86f,.15f), new Vector3(.22f,.34f,.22f), new Color(.4f,.92f,1f), rightArm);
                decoration.localRotation = Quaternion.Euler(15,0,45);
                Part("Orb", PrimitiveType.Sphere, new Vector3(0,-.42f,.29f), new Vector3(.29f,.29f,.29f), accent, leftArm);
            }
            else
            {
                Part("Forest Hood", PrimitiveType.Sphere, new Vector3(0,2.15f,-.1f), new Vector3(.62f,.4f,.56f), accent * .48f);
                Part("Feather", PrimitiveType.Capsule, new Vector3(.22f,2.35f,-.09f), new Vector3(.085f,.25f,.05f), new Color(1f,.81f,.36f));
                Part("Quiver", PrimitiveType.Cylinder, new Vector3(.34f,1.4f,-.36f), new Vector3(.25f,.4f,.25f), new Color(.39f,.23f,.13f));
                for (int i=0; i<3; i++) Part("Spare Arrow", PrimitiveType.Cylinder, new Vector3(.27f+i*.07f,1.91f,-.36f), new Vector3(.035f,.3f,.035f), steel);
                Transform bow = Joint("Bow", new Vector3(-.52f,1.1f,.23f));
                for (int i=0; i<9; i++)
                {
                    float a = (-80f+i*20f)*Mathf.Deg2Rad;
                    Transform wood = Part("Bow Limb", PrimitiveType.Capsule, new Vector3(0,Mathf.Sin(a)*.59f,Mathf.Cos(a)*.28f), new Vector3(.075f,.115f,.07f), new Color(.73f,.47f,.22f), bow);
                    wood.localRotation=Quaternion.Euler(i*20-80,0,0);
                }
                Part("Bowstring",PrimitiveType.Cylinder,new Vector3(0,0,.05f),new Vector3(.014f,.6f,.014f),steel,bow);
                Part("Nocked Arrow",PrimitiveType.Cube,new Vector3(0,0,.45f),new Vector3(.035f,.035f,.85f),steel,bow);
            }
        }

        private void BuildEnemy(EnemyKind kind, bool boss)
        {
            Color dark = new Color(.18f,.16f,.25f);
            if (kind == EnemyKind.Slime)
            {
                slime=true;
                body=Part("Slime Body",PrimitiveType.Sphere,new Vector3(0,.51f,0),new Vector3(1.16f,.98f,1.03f),new Color(.42f,.74f,.39f));
                Part("Slime Crown",PrimitiveType.Sphere,new Vector3(.15f,1f,-.06f),new Vector3(.35f,.3f,.3f),new Color(.62f,.9f,.48f));
                for(int i=-1;i<=1;i+=2)
                {
                    Part("Eye White",PrimitiveType.Sphere,new Vector3(i*.22f,.64f,.44f),new Vector3(.23f,.28f,.1f),Color.white);
                    Part("Eye Pupil",PrimitiveType.Sphere,new Vector3(i*.22f,.64f,.5f),new Vector3(.1f,.15f,.06f),dark);
                }
                Part("Smile",PrimitiveType.Cube,new Vector3(0,.4f,.5f),new Vector3(.24f,.045f,.035f),dark);
            }
            else if(kind == EnemyKind.Wisp)
            {
                floating=true;
                body=Part("Spirit Core",PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(.74f,.87f,.74f),new Color(.57f,.42f,.93f));
                decoration=Part("Spirit Crown",PrimitiveType.Cube,new Vector3(0,1.96f,0),new Vector3(.24f,.24f,.24f),new Color(.91f,.7f,1f));
                for(int i=-1;i<=1;i+=2)
                    Part("Spirit Eyes",PrimitiveType.Sphere,new Vector3(i*.17f,1.47f,.33f),new Vector3(.1f,.13f,.07f),new Color(1f,.85f,.98f));
                for(int i=0;i<3;i++) Part("Spirit Tail",PrimitiveType.Sphere,new Vector3(0,.88f-i*.2f,-i*.12f),Vector3.one*(.4f-i*.09f),new Color(.39f,.29f,.62f));
            }
            else
            {
                bool brute=kind==EnemyKind.Guardian;
                Color skin=brute?new Color(.57f,.39f,.31f):new Color(.4f,.62f,.28f);
                Humanoid(skin,dark,brute?new Color(.43f,.48f,.57f):new Color(.49f,.31f,.18f),brute?1.45f:1f);
                for(int i=-1;i<=1;i+=2)
                {
                    Transform horn=Part(brute?"Stone Horn":"Long Ear",PrimitiveType.Capsule,new Vector3(i*.31f,2.12f,0),new Vector3(.13f,.22f,.13f),brute?new Color(.93f,.84f,.66f):skin);
                    horn.localRotation=Quaternion.Euler(0,0,-i*40f);
                }
                if(brute)
                {
                    Part("Iron Crown",PrimitiveType.Cube,new Vector3(0,2.15f,0),new Vector3(.62f,.24f,.48f),new Color(.3f,.32f,.41f));
                    Part("Crown Crystal",PrimitiveType.Sphere,new Vector3(0,2.24f,.26f),new Vector3(.19f,.23f,.12f),new Color(1f,.34f,.22f));
                    Part("Hammer Grip",PrimitiveType.Cylinder,new Vector3(0,-.11f,.1f),new Vector3(.14f,.65f,.14f),new Color(.37f,.22f,.14f),rightArm);
                    Part("Great Hammer",PrimitiveType.Cube,new Vector3(0,.55f,.1f),new Vector3(.85f,.42f,.42f),new Color(.56f,.57f,.64f),rightArm);
                }
                else
                {
                    Part("Goblin Knife",PrimitiveType.Cube,new Vector3(0,-.2f,.13f),new Vector3(.14f,.6f,.065f),new Color(.72f,.75f,.73f),rightArm);
                    Part("Leather Cap",PrimitiveType.Sphere,new Vector3(0,2.15f,-.03f),new Vector3(.55f,.3f,.5f),new Color(.45f,.27f,.17f));
                }
                transform.localScale=Vector3.one*(brute?(boss?1.48f:1.15f):.8f);
            }
        }

        public void Animate(float speed, float attack, bool hurt)
        {
            float walk=Mathf.Sin(Time.time*11f+phase)*Mathf.Min(speed,1f)*27f;
            if(slime)
            {
                float bounce=Mathf.Sin(Time.time*(speed>.1f?9f:3f)+phase);
                body.localScale=new Vector3(1.16f-bounce*.06f,.98f+bounce*.08f,1.03f-bounce*.06f);
                transform.localPosition=new Vector3(0,Mathf.Max(0,bounce)*.11f,0);
            }
            else if(floating)
            {
                transform.localPosition=Vector3.up*(Mathf.Sin(Time.time*3f+phase)*.15f);
            }
            else
            {
                if(leftLeg!=null) leftLeg.localRotation=Quaternion.Euler(walk,0,0);
                if(rightLeg!=null) rightLeg.localRotation=Quaternion.Euler(-walk,0,0);
                if(leftArm!=null) leftArm.localRotation=Quaternion.Euler(-walk*.6f,0,-8f);
                if(rightArm!=null) rightArm.localRotation=Quaternion.Euler(attack>0?-75f+attack*160f:walk*.6f,attack>0?-40f:0,8f);
                transform.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(Time.time*11f+phase))*.035f*Mathf.Min(speed,1f));
            }
            if(decoration!=null) decoration.Rotate(0,Time.deltaTime*80f,0,Space.Self);
            if(hurt) transform.localPosition+=new Vector3(Mathf.Sin(Time.time*90f)*.05f,0,0);
        }

        private void OnDestroy()
        {
            foreach(Material material in palette.Values) if(material!=null) Destroy(material);
        }
    }

    internal sealed class OwnedCombatMesh : MonoBehaviour
    {
        public Mesh Value;
        private void OnDestroy() { if(Value!=null) Destroy(Value); }
    }
}
