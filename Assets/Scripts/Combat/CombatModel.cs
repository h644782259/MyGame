using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Each model owns its palette. Materials are reused across all of its parts.
    public sealed class CombatModel : MonoBehaviour
    {
        private readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        private Transform leftLeg, rightLeg, leftArm, rightArm, body, decoration;
        private Transform spine, pelvis, headRig, leftElbow, rightElbow, leftKnee, rightKnee, cloak;
        private Transform swordRig, staffRig, bowRig, arrowRig, castingOrb;
        private LineRenderer bowstring;
        private HeroClass heroClass;
        private bool isHero, actionBasic;
        private int actionSkill, swingCount;
        private float actionAge, actionDuration, gaitPhase;
        private bool slime, floating;
        private float phase;

        public static CombatModel Hero(Transform parent, HeroClass hero)
        {
            CombatModel model = Create(parent);
            model.BuildHero(hero);
            model.EnhanceHero(hero);
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
            obj.transform.localPosition = new Vector3(0, 1.66f, -.23f);
            cloak = obj.transform;
            Mesh mesh = new Mesh();
            mesh.name = "Cloak Mesh";
            mesh.vertices = new[] {
                new Vector3(-.32f,0,0), new Vector3(.32f,0,0),
                new Vector3(-.51f,-1.24f,-.22f), new Vector3(.51f,-1.24f,-.22f),
                new Vector3(-.32f,0,-.07f), new Vector3(.32f,0,-.07f),
                new Vector3(-.51f,-1.24f,-.29f), new Vector3(.51f,-1.24f,-.29f)
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

        // Original, self-contained character art: articulated costume pieces share one palette.
        // The spine and limbs are separate so locomotion never cancels an attack's upper-body pose.
        private void EnhanceHero(HeroClass hero)
        {
            isHero = true;
            heroClass = hero;
            Color accent = GameBalance.ClassColor(hero);
            Color gold = new Color(.94f, .73f, .32f);
            Color steel = new Color(.73f, .83f, .91f);
            Color leather = new Color(.24f, .16f, .12f);
            Color hair = hero == HeroClass.Arcanist ? new Color(.81f, .73f, .61f) : new Color(.24f, .12f, .075f);
            for (int i = -1; i <= 1; i += 2)
            {
                Transform lockOfHair = Part("Hair Side Lock", PrimitiveType.Capsule,
                    new Vector3(i * .23f, 1.98f, -.025f), new Vector3(.13f, .22f, .16f), hair);
                lockOfHair.localRotation = Quaternion.Euler(-12f, 0, -i * 11f);
                Part("Ear", PrimitiveType.Sphere, new Vector3(i * .26f, 1.97f, .015f),
                    new Vector3(.085f, .15f, .1f), new Color(.94f, .76f, .59f));
            }
            Part("Nose", PrimitiveType.Sphere, new Vector3(0, 1.96f, .243f),
                new Vector3(.075f, .09f, .075f), new Color(.94f, .76f, .59f));
            Part("Collar", PrimitiveType.Cylinder, new Vector3(0, 1.7f, -.01f), new Vector3(.38f, .055f, .35f), gold);
            if (hero == HeroClass.Vanguard)
            {
                Transform chest = Part("Cuirass", PrimitiveType.Cube, new Vector3(0, 1.35f, .235f),
                    new Vector3(.64f, .44f, .15f), steel);
                chest.localRotation = Quaternion.Euler(-8f, 0, 0);
                for (int i = 0; i < 3; i++)
                    Part("Overlapping Armor", PrimitiveType.Cube, new Vector3(0, 1.13f - i * .11f, .255f),
                        new Vector3(.57f - i * .045f, .13f, .11f), i % 2 == 0 ? steel * .85f : steel);
                Part("Chest Crest", PrimitiveType.Cube, new Vector3(0, 1.38f, .33f), new Vector3(.2f, .2f, .045f), gold)
                    .localRotation = Quaternion.Euler(0, 0, 45);
                for (int i = -1; i <= 1; i += 2)
                {
                    Part("Armor Tasset", PrimitiveType.Cube, new Vector3(i * .29f, .77f, .15f),
                        new Vector3(.23f, .32f, .13f), steel).localRotation = Quaternion.Euler(-8f, 0, i * 12f);
                    Part("Helmet Cheek Guard", PrimitiveType.Cube, new Vector3(i * .25f, 1.96f, .13f),
                        new Vector3(.085f, .23f, .14f), steel).localRotation = Quaternion.Euler(-12f, 0, i * 8f);
                }
            }
            else if (hero == HeroClass.Arcanist)
            {
                RemovePart(transform.Find("Wizard Hat"));
                Tapered("Pointed Wizard Hat", transform, new Vector3(0, 2.24f, -.02f),
                    .29f, .018f, .62f, new Vector3(-.14f, 0, -.11f), accent * .48f, 7);
                Tapered("Layered Robe", transform, new Vector3(0, .4f, 0), .49f, .31f, .62f,
                    Vector3.zero, accent * .68f, 10);
                for (int i = -1; i <= 1; i += 2)
                {
                    Transform stole = Part("Embroidered Stole", PrimitiveType.Cube, new Vector3(i * .22f, 1.3f, .246f),
                        new Vector3(.12f, .67f, .065f), gold);
                    stole.localRotation = Quaternion.Euler(0, 0, i * 10f);
                    Part("Robe Hem", PrimitiveType.Cube, new Vector3(i * .25f, .53f, .36f),
                        new Vector3(.1f, .27f, .06f), gold).localRotation = Quaternion.Euler(-10f, 0, i * 15f);
                }
                Part("Arcane Brooch", PrimitiveType.Sphere, new Vector3(0, 1.48f, .285f), new Vector3(.17f, .22f, .09f),
                    new Color(.4f, .91f, 1f));
            }
            else
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Part("Leather Vest", PrimitiveType.Cube, new Vector3(i * .2f, 1.31f, .18f),
                        new Vector3(.28f, .57f, .16f), leather).localRotation = Quaternion.Euler(0, 0, i * -9f);
                    Part("Vest Buckle", PrimitiveType.Cube, new Vector3(i * .18f, 1.34f, .285f),
                        new Vector3(.075f, .075f, .035f), gold);
                }
                Part("Scarf", PrimitiveType.Cube, new Vector3(0, 1.68f, .16f), new Vector3(.48f, .18f, .25f), accent);
                Part("Scarf Tail", PrimitiveType.Cube, new Vector3(.3f, 1.47f, .23f), new Vector3(.17f, .38f, .045f), accent)
                    .localRotation = Quaternion.Euler(-8f, 0, -15f);
                Part("Cross Body Strap", PrimitiveType.Cube, new Vector3(.015f, 1.29f, .29f),
                    new Vector3(.095f, .78f, .055f), gold * .65f).localRotation = Quaternion.Euler(0, 0, -37f);
            }

            spine = Joint("Spine", new Vector3(0, 1.12f, 0));
            pelvis = Joint("Pelvis", new Vector3(0, .83f, 0));
            var rootParts = new List<Transform>();
            foreach (Transform part in transform) if (part != spine && part != pelvis) rootParts.Add(part);
            foreach (Transform part in rootParts) part.SetParent(part == leftLeg || part == rightLeg ? pelvis : spine, true);
            headRig = NewJoint("Neck", spine, new Vector3(0, .68f, 0));
            foreach (Transform part in rootParts)
            {
                string n = part.name;
                if (n == "Head" || n == "Ear" || n == "Nose" || n.StartsWith("Eyes") || n.StartsWith("Hair") ||
                    n.StartsWith("Helmet") || n.StartsWith("Hat") || n == "Nose Guard" ||
                    n == "Pointed Wizard Hat" || n == "Forest Hood" || n == "Feather") part.SetParent(headRig, true);
            }
            if (hero == HeroClass.Ranger)
            {
                leftArm.localPosition = new Vector3(-.36f, .45f, 0);
                rightArm.localPosition = new Vector3(.36f, .45f, 0);
            }
            leftElbow = ArticulateArm(leftArm, hero == HeroClass.Vanguard ? steel : leather);
            rightElbow = ArticulateArm(rightArm, hero == HeroClass.Vanguard ? steel : leather);
            leftKnee = ArticulateLeg(leftLeg, hero == HeroClass.Vanguard ? steel : leather);
            rightKnee = ArticulateLeg(rightLeg, hero == HeroClass.Vanguard ? steel : leather);

            if (hero == HeroClass.Vanguard)
            {
                foreach (string name in new[] { "Sword Handle", "Sword Guard", "Silver Blade", "Blade Tip" })
                    RemovePart(rightElbow.Find(name));
                swordRig = NewJoint("Sword Wrist", rightElbow, new Vector3(0, -.23f, .04f));
                Part("Wrapped Hilt", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.105f, .15f, .105f), leather, swordRig);
                Part("Gold Pommel", PrimitiveType.Sphere, new Vector3(0, -.17f, 0), Vector3.one * .15f, gold, swordRig);
                Part("Crossguard", PrimitiveType.Cube, new Vector3(0, .16f, 0), new Vector3(.42f, .08f, .13f), gold, swordRig);
                Blade("Forged Sword", swordRig, new Vector3(0, .2f, 0), .18f, 1.1f, .065f, steel);
                Part("Blade Fuller", PrimitiveType.Cube, new Vector3(0, .66f, .037f), new Vector3(.035f, .84f, .012f),
                    new Color(.35f, .58f, .73f), swordRig);
                for (int i = -1; i <= 1; i += 2)
                    Part("Shield Inlay", PrimitiveType.Cube, new Vector3(-.14f + i * .17f, -.09f, .254f),
                        new Vector3(.065f, .43f, .055f), gold, leftElbow).localRotation = Quaternion.Euler(0, 0, i * -20f);
            }
            else if (hero == HeroClass.Arcanist)
            {
                staffRig = NewJoint("Staff Wrist", rightElbow, new Vector3(0, -.23f, .12f));
                foreach (string name in new[] { "Staff", "Staff Gold", "Arcane Crystal" })
                {
                    Transform part = rightElbow.Find(name);
                    if (part != null) part.SetParent(staffRig, true);
                }
                for (int i = -1; i <= 1; i += 2)
                {
                    Transform prong = Part("Staff Crystal Crown", PrimitiveType.Cube,
                        new Vector3(i * .14f, 1.26f, .03f), new Vector3(.065f, .37f, .07f), gold, staffRig);
                    prong.localRotation = Quaternion.Euler(0, 0, i * -18f);
                }
                castingOrb = leftElbow.Find("Orb");
            }
            else
            {
                bowRig = spine.Find("Bow");
                bowRig.SetParent(leftElbow, false);
                bowRig.localPosition = new Vector3(0, -.23f, -.20f);
                RemovePart(bowRig.Find("Bowstring"));
                RemovePart(bowRig.Find("Nocked Arrow"));
                Part("Bow Grip", PrimitiveType.Cube, new Vector3(0, 0, .275f), new Vector3(.1f, .21f, .095f), leather, bowRig);
                GameObject stringObject = new GameObject("Drawn Bowstring");
                stringObject.transform.SetParent(bowRig, false);
                bowstring = stringObject.AddComponent<LineRenderer>();
                bowstring.useWorldSpace = false;
                bowstring.positionCount = 3;
                bowstring.startWidth = bowstring.endWidth = .012f;
                bowstring.sharedMaterial = Mat(new Color(.85f, .82f, .67f));
                bowstring.SetPosition(0, new Vector3(0, .58f, .05f));
                bowstring.SetPosition(1, new Vector3(0, 0, .05f));
                bowstring.SetPosition(2, new Vector3(0, -.58f, .05f));
                arrowRig = NewJoint("Arrow Nock", bowRig, new Vector3(0, 0, .05f));
                Part("Arrow Shaft", PrimitiveType.Cube, new Vector3(0, 0, .42f), new Vector3(.025f, .025f, .84f), gold, arrowRig);
                Transform tip = Blade("Arrow Head", arrowRig, new Vector3(0, 0, .82f), .09f, .17f, .025f, steel);
                tip.localRotation = Quaternion.Euler(90f, 0, 0);
                for (int i = 0; i < 3; i++)
                    Part("Arrow Fletching", PrimitiveType.Cube, new Vector3(0, 0, .095f), new Vector3(.11f, .012f, .16f), accent, arrowRig)
                        .localRotation = Quaternion.Euler(0, 0, i * 120f);
            }
        }

        private static Transform NewJoint(string name, Transform parent, Vector3 at)
        {
            Transform joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = at;
            return joint;
        }

        private static void RemovePart(Transform part)
        {
            if (part == null) return;
            part.gameObject.SetActive(false);
            Destroy(part.gameObject);
        }

        private Transform ArticulateArm(Transform arm, Color cuff)
        {
            var moving = new List<Transform>();
            foreach (Transform part in arm) if (part.name != "Pauldrons" && part.name != "Sleeve") moving.Add(part);
            Transform sleeve = arm.Find("Sleeve");
            sleeve.localPosition = new Vector3(0, -.14f, 0);
            sleeve.localScale = new Vector3(.235f, .17f, .24f);
            Transform elbow = NewJoint("Elbow", arm, new Vector3(0, -.30f, 0));
            foreach (Transform part in moving) part.SetParent(elbow, true);
            Part("Forearm Bracer", PrimitiveType.Capsule, new Vector3(0, -.1f, 0), new Vector3(.245f, .14f, .25f), cuff, elbow);
            Part("Cuff Trim", PrimitiveType.Cylinder, new Vector3(0, -.18f, 0), new Vector3(.25f, .026f, .25f),
                new Color(.83f, .66f, .3f), elbow);
            return elbow;
        }

        private Transform ArticulateLeg(Transform leg, Color greave)
        {
            Transform trouser = leg.Find("Trouser");
            trouser.localPosition = new Vector3(0, -.16f, 0);
            trouser.localScale = new Vector3(.24f, .20f, .25f);
            Transform knee = NewJoint("Knee", leg, new Vector3(0, -.37f, 0));
            leg.Find("Boot").SetParent(knee, true);
            Part("Shin Guard", PrimitiveType.Capsule, new Vector3(0, -.11f, .015f), new Vector3(.25f, .17f, .26f), greave, knee);
            Part("Knee Guard", PrimitiveType.Sphere, new Vector3(0, 0, .105f), new Vector3(.27f, .25f, .16f), greave, knee);
            return knee;
        }

        private Transform MeshPart(string name, Transform parent, Vector3 at, Vector3[] vertices, int[] triangles, Color color)
        {
            Transform part = NewJoint(name, parent, at);
            Mesh mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
            part.gameObject.AddComponent<OwnedCombatMesh>().Value = mesh;
            return part;
        }

        private Transform Blade(string name, Transform parent, Vector3 at, float width, float length, float depth, Color color)
        {
            float w = width * .5f, d = depth * .5f;
            return MeshPart(name, parent, at, new[] {
                new Vector3(-w,0,0), new Vector3(w,0,0), new Vector3(-w,length*.79f,0),
                new Vector3(w,length*.79f,0), new Vector3(0,length,0),
                new Vector3(0,0,d),new Vector3(0,length*.78f,d),
                new Vector3(0,0,-d),new Vector3(0,length*.78f,-d)
            }, new[] { 0,6,5,0,2,6,2,4,6,4,3,6,3,1,6,1,5,6,
                0,7,8,0,8,2,2,8,4,4,8,3,3,8,1,1,8,7,0,5,1,0,1,7 }, color);
        }

        private Transform Tapered(string name, Transform parent, Vector3 at, float bottom, float top, float height,
            Vector3 offset, Color color, int count)
        {
            Vector3[] vertices = new Vector3[count * 2 + 2];
            var indices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i] = radial * bottom;
                vertices[i + count] = radial * top + Vector3.up * height + offset;
                int next = (i + 1) % count;
                indices.AddRange(new[] { i, i + count, next, next, i + count, next + count,
                    count * 2, i, next, count * 2 + 1, next + count, i + count });
            }
            vertices[count * 2 + 1] = Vector3.up * height + offset;
            return MeshPart(name, parent, at, vertices, indices.ToArray(), color);
        }

        // Called only after the corresponding attack has successfully started / consumed resources.
        public void PlayAction(int skill, bool basic)
        {
            if (!isHero) return;
            actionSkill = skill;
            actionBasic = basic;
            actionAge = 0;
            swingCount++;
            actionDuration = basic ? (heroClass == HeroClass.Vanguard ? .46f : heroClass == HeroClass.Arcanist ? .5f : .38f)
                : (skill == 9 ? 1.12f : skill >= 4 ? .84f : .68f);
        }

        private static Quaternion Pose(Vector3 idle, Vector3 windup, Vector3 release, float normalizedTime)
        {
            // A readable anticipation, a fast release, and a longer settling phase.
            if (normalizedTime < .30f)
                return Quaternion.Slerp(Quaternion.Euler(idle), Quaternion.Euler(windup), Mathf.SmoothStep(0, 1, normalizedTime / .30f));
            if (normalizedTime < .52f)
                return Quaternion.Slerp(Quaternion.Euler(windup), Quaternion.Euler(release), Mathf.SmoothStep(0, 1, (normalizedTime - .30f) / .22f));
            return Quaternion.Slerp(Quaternion.Euler(release), Quaternion.Euler(idle), Mathf.SmoothStep(0, 1, (normalizedTime - .52f) / .48f));
        }

        private void AnimateHero(float speed, float attack, bool hurt)
        {
            float dt = Time.deltaTime;
            speed = Mathf.Clamp01(speed);
            gaitPhase += dt * (speed > .03f ? 10.5f : 2f);
            if (actionDuration > 0) actionAge = Mathf.Min(actionAge + dt, actionDuration);
            float t = actionDuration > 0 ? actionAge / actionDuration : 1f;
            bool acting = t < 1f;
            float stride = Mathf.Sin(gaitPhase) * speed;
            float lift = Mathf.Abs(Mathf.Sin(gaitPhase));
            float breathing = Mathf.Sin(Time.time * 2f + phase);
            transform.localPosition = Vector3.up * (lift * .035f * speed);
            if (hurt) transform.localPosition += Vector3.right * (Mathf.Sin(Time.time * 90f) * .04f);
            pelvis.localPosition = new Vector3(stride * .024f, .83f, 0);
            pelvis.localRotation = Quaternion.Euler(0, stride * -4f, stride * 2f);
            leftLeg.localRotation = Quaternion.Euler(stride * 30f, 0, -2f);
            rightLeg.localRotation = Quaternion.Euler(-stride * 30f, 0, 2f);
            leftKnee.localRotation = Quaternion.Euler(Mathf.Max(0, -stride) * 42f, 0, 0);
            rightKnee.localRotation = Quaternion.Euler(Mathf.Max(0, stride) * 42f, 0, 0);
            spine.localPosition = new Vector3(0, 1.12f + breathing * .009f, 0);
            spine.localRotation = Quaternion.Euler(speed * 5f, stride * 4f, -stride * 1.5f);
            headRig.localRotation = Quaternion.Euler(-speed * 3f, -stride * 3f, stride);
            leftArm.localRotation = Quaternion.Euler(-stride * 18f, 0, -8f);
            rightArm.localRotation = Quaternion.Euler(stride * 18f, 0, 8f);
            leftElbow.localRotation = Quaternion.Euler(-12f, 0, 0);
            rightElbow.localRotation = Quaternion.Euler(-16f, 0, 0);
            cloak.localRotation = Quaternion.Euler(8f + speed * 13f + Mathf.Sin(gaitPhase * .5f) * 4f, stride * -5f, stride * 3f);

            if (heroClass == HeroClass.Vanguard)
            {
                Vector3 idleR = new Vector3(-18f + stride * 13f, 0, 12f);
                swordRig.localRotation = Quaternion.Euler(24f, 0, -8f);
                if (acting)
                {
                    bool overhead = !actionBasic && (actionSkill == 2 || actionSkill == 7 || actionSkill == 9);
                    float direction = swingCount % 2 == 0 ? 1 : -1;
                    spine.localRotation *= Pose(Vector3.zero, new Vector3(-8, direction * -29f, -7),
                        new Vector3(overhead ? 21f : 10f, direction * 34f, 9), t);
                    rightArm.localRotation = Pose(idleR,
                        overhead ? new Vector3(-158f, 12f, 18f) : new Vector3(-72f, -52f * direction, 67f * direction),
                        overhead ? new Vector3(-37f, -12f, -8f) : new Vector3(-48f, 65f * direction, -48f * direction), t);
                    rightElbow.localRotation = Pose(new Vector3(-16, 0, 0), new Vector3(-66, 0, 0), new Vector3(-4, 0, 0), t);
                    swordRig.localRotation = Pose(new Vector3(24, 0, -8), new Vector3(-32, 0, -18), new Vector3(65, 0, 10), t);
                    leftArm.localRotation = Pose(new Vector3(-10, 0, -8), new Vector3(-47, 0, -19), new Vector3(-34, 0, -22), t);
                    leftElbow.localRotation = Quaternion.Euler(-35f, 0, 0);
                }
                else rightArm.localRotation = Quaternion.Euler(idleR);
            }
            else if (heroClass == HeroClass.Arcanist)
            {
                staffRig.localRotation = Quaternion.Euler(12, 0, -12);
                if (acting)
                {
                    spine.localRotation *= Pose(Vector3.zero, new Vector3(-9, -13, -3), new Vector3(14, 16, 3), t);
                    rightArm.localRotation = Pose(new Vector3(-10, 0, 10), new Vector3(-115, -12, 32),
                        new Vector3(-64, 22, 17), t);
                    rightElbow.localRotation = Pose(new Vector3(-12, 0, 0), new Vector3(-42, 0, 0), new Vector3(-9, 0, 0), t);
                    staffRig.localRotation = Pose(new Vector3(12, 0, -12), new Vector3(85, 0, -20), new Vector3(118, 0, -5), t);
                    leftArm.localRotation = Pose(new Vector3(-12, 0, -10), new Vector3(-58, 12, -38), new Vector3(-84, 0, -17), t);
                    leftElbow.localRotation = Pose(new Vector3(-20, 0, 0), new Vector3(-78, 0, 0), new Vector3(-12, 0, 0), t);
                }
                float charge = acting ? Mathf.Sin(Mathf.Clamp01(t / .52f) * Mathf.PI) : .1f + breathing * .04f;
                if (castingOrb != null) castingOrb.localScale = Vector3.one * (.21f + charge * .18f);
            }
            else
            {
                float draw = acting ? (t < .32f ? Mathf.SmoothStep(0, 1, t / .32f) :
                    t < .48f ? 1f - Mathf.SmoothStep(0, 1, (t - .32f) / .16f) : 0) : 0;
                float ready = acting ? Mathf.Min(1f, Mathf.Min(t / .16f, (1 - t) / .28f)) : .3f;
                spine.localRotation *= Quaternion.Euler(0, -13f * ready, -3f * ready);
                Vector3 bowHand = Vector3.Lerp(new Vector3(-.32f, .1f, .28f), new Vector3(-.035f, .45f, .415f), ready);
                AimArm(leftArm, leftElbow, bowHand, new Vector3(-1, -.5f, 0));
                bowRig.rotation = spine.rotation * Quaternion.Euler(0, 10f, -7f);
                bowRig.localPosition = new Vector3(0, -.23f, .04f) -
                    bowRig.localRotation * new Vector3(0, 0, .275f);
                Vector3 nock = new Vector3(0, 0, .05f - draw * .30f);
                bowstring.SetPosition(1, nock);
                arrowRig.localPosition = nock;
                Vector3 stringHand = Vector3.Lerp(new Vector3(.30f, .04f, .22f),
                    spine.InverseTransformPoint(bowRig.TransformPoint(nock)), ready);
                AimArm(rightArm, rightElbow, stringHand, new Vector3(1, -.2f, -.2f));
                arrowRig.gameObject.SetActive(!acting || t < .44f || t > .83f);
            }
            if (decoration != null) decoration.Rotate(0, dt * (acting ? 145f : 42f), 0, Space.Self);
        }

        private static void AimArm(Transform shoulder, Transform elbow, Vector3 target, Vector3 bendHint)
        {
            const float upper = .30f, lower = .23f;
            Vector3 delta = target - shoulder.localPosition;
            float distance = Mathf.Clamp(delta.magnitude, .08f, upper + lower - .005f);
            Vector3 direction = delta.sqrMagnitude > .00001f ? delta.normalized : Vector3.forward;
            Vector3 bend = Vector3.ProjectOnPlane(bendHint, direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.down;
            float along = (upper * upper - lower * lower + distance * distance) / (2f * distance);
            Vector3 upperVector = direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Vector3 lowerVector = direction * distance - upperVector;
            shoulder.localRotation = Quaternion.FromToRotation(Vector3.down, upperVector);
            elbow.localRotation = Quaternion.Inverse(shoulder.localRotation) * Quaternion.FromToRotation(Vector3.down, lowerVector);
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
            if (isHero) { AnimateHero(speed, attack, hurt); return; }
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
