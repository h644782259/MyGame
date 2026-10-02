using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private Transform CostumeMesh(string name,WingSilhouette recipe,Transform parent,Vector3 at,Vector3 scale,Color color,VisualSurface surface)
        {
            Transform root=NewJoint(name,parent,at);root.localScale=scale;
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=CostumeMeshLibrary.Get(recipe);
            root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(color,surface);
            return root;
        }
        private void BuildFashionWingShape(FashionData wings,Color color)
        {
            WingSilhouette style=CostumeRecipes.WingStyle(wings.rarity);
            if(style==WingSilhouette.Mechanical)
            {
                Transform orbit=NewJoint("Mechanical star-ring orbit",fashionWings,Vector3.zero);
                CostumeMesh("Bronze outer astrolabe",style,orbit,Vector3.zero,Vector3.one*1.2f,new Color(.71f,.48f,.23f),VisualSurface.Metal);
                CostumeMesh("Inclined inner astrolabe",style,orbit,Vector3.zero,new Vector3(.91f,.91f,.91f),color,VisualSurface.Metal).localRotation=Quaternion.Euler(28,15,0);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;Vector3 axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    Part("Star-ring radial vane",PrimitiveType.Cube,axis*.96f,new Vector3(.15f,.3f,.075f),color,orbit,VisualSurface.Metal).localRotation=Quaternion.Euler(0,0,i*45-90);
                    Part("Star-ring focus crystal",PrimitiveType.Sphere,axis*.78f,Vector3.one*.09f,Color.white,orbit,VisualSurface.Crystal);
                }
                orbit.gameObject.AddComponent<FashionOrbit>();
            }
            else for(int side=-1;side<=1;side+=2)
            {
                int count=style==WingSilhouette.Crystal?5:5+(wings.rarity==Rarity.Rare?1:0);
                for(int i=0;i<count;i++)
                {
                    var feather=CostumeMesh(style==WingSilhouette.Crystal?"Faceted wing crystal":"Swept flight feather",style,fashionWings,
                        new Vector3(side*(.18f+i*.18f),.18f-i*.12f,-i*.025f),
                        new Vector3(1,1.05f-i*.065f,1),i%2==0?color:Color.Lerp(color,Color.white,.28f),style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Cloth);
                    feather.localRotation=Quaternion.Euler(12,side*8,-side*(30+i*12));
                }
                Part("Wing scapular support",PrimitiveType.Capsule,new Vector3(side*.3f,.08f,0),new Vector3(.14f,.45f,.13f),color,fashionWings,style==WingSilhouette.Crystal?VisualSurface.Crystal:VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-side*52);
            }
            Part("Wing clasp",PrimitiveType.Sphere,Vector3.zero,new Vector3(.2f,.23f,.12f),Color.white,fashionWings,VisualSurface.Crystal);
        }
        private void BuildClassCostume()
        {
            Color accent=GameBalance.ClassColor(heroClass),leather=new Color(.25f,.17f,.10f);
            if(body!=null)body.GetComponent<Renderer>().sharedMaterial=Mat(accent*.62f,heroClass==HeroClass.Vanguard?VisualSurface.Metal:VisualSurface.Cloth);
            if(heroClass==HeroClass.Vanguard)return;
            RemovePart(leftArm.Find("Pauldrons"));RemovePart(rightArm.Find("Pauldrons"));
            if(heroClass==HeroClass.Arcanist)
            {
                for(int side=-1;side<=1;side+=2)
                    Part("Long robe front panel",PrimitiveType.Cube,new Vector3(side*.18f,-.1f,.31f),new Vector3(.18f,.92f,.055f),accent*.8f,spine,VisualSurface.Cloth).localRotation=Quaternion.Euler(-5,0,side*4);
            }
            else if(heroClass==HeroClass.Ranger)
            {
                Part("Single leather shoulder",PrimitiveType.Sphere,new Vector3(0,.025f,0),new Vector3(.42f,.18f,.40f),leather,leftArm,VisualSurface.Cloth);
                Part("Diagonal ranger sash",PrimitiveType.Cube,new Vector3(.03f,.21f,.32f),new Vector3(.16f,.64f,.05f),accent*.68f,spine,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-34);
            }
            else
            {
                // Keep the existing antler crown but remove the mage's ankle-length robe.
                RemovePart(spine.Find("Layered Robe"));
                foreach(Transform part in spine)if(part.name=="Embroidered Stole"||part.name=="Robe Hem")RemovePart(part);
                for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
                    CostumeMesh("Leaf ritual mantle",WingSilhouette.Feather,spine,new Vector3(side*(.24f+i*.12f),.56f-i*.07f,.05f),new Vector3(1.2f,.42f,1.2f),accent*.65f,VisualSurface.Foliage).localRotation=Quaternion.Euler(15,0,side*(125+i*15));
                Part("Bound spirit totem",PrimitiveType.Cube,new Vector3(0,.14f,.35f),new Vector3(.18f,.38f,.12f),leather,spine,VisualSurface.Wood);
                Part("Totem moonstone",PrimitiveType.Sphere,new Vector3(0,.23f,.43f),Vector3.one*.15f,accent,spine,VisualSurface.Crystal);
            }
        }
        private void BuildClassEquipmentArmor(EquipmentAppearance look)
        {
            equipmentArmor=GearRoot("Equipped class costume",spine);equipmentArmor.localPosition=Vector3.down*1.12f;
            float width=CostumeRecipes.ChestWidth(heroClass,look.Tier);
            Color cloth=Color.Lerp(GameBalance.ClassColor(heroClass)*.65f,look.Accent,.2f);
            if(heroClass==HeroClass.Arcanist)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Part("Embroidered robe panel",PrimitiveType.Cube,new Vector3(side*width*.30f,.99f,.35f),new Vector3(width*.38f,.99f,.06f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(-5,0,side*4);
                    Part("Robe metal seam",PrimitiveType.Cube,new Vector3(side*width*.47f,1.05f,.39f),new Vector3(.028f,.9f,.02f),look.Accent,equipmentArmor,VisualSurface.Metal);
                }
            }
            else if(heroClass==HeroClass.Ranger)
                Part("Asymmetric leather chest wrap",PrimitiveType.Cube,new Vector3(-.08f,1.33f,.33f),new Vector3(width,.42f,.08f),cloth,equipmentArmor,VisualSurface.Cloth).localRotation=Quaternion.Euler(0,0,-17);
            else
            {
                Part("Spirit ritual bib",PrimitiveType.Cube,new Vector3(0,1.34f,.35f),new Vector3(width,.33f,.09f),cloth,equipmentArmor,VisualSurface.Cloth);
                CostumeMesh("Contract chest crystal",WingSilhouette.Crystal,equipmentArmor,new Vector3(0,1.1f,.44f),new Vector3(.65f,.37f,.6f),look.Glow,VisualSurface.Crystal);
            }
            for(int side=-1;side<=1;side+=2)
            {
                if(heroClass==HeroClass.Ranger&&side==1)continue;
                Transform shoulder=GearRoot("Equipped class shoulder",side<0?leftArm:rightArm);
                if(side<0)equipmentLeftShoulder=shoulder;else equipmentRightShoulder=shoulder;
                if(heroClass==HeroClass.Summoner)
                    for(int i=0;i<3;i++)CostumeMesh("Tiered leaf shoulder",WingSilhouette.Feather,shoulder,new Vector3(side*i*.09f,.08f,0),new Vector3(1,.38f+look.Tier*.025f,1),cloth,VisualSurface.Foliage).localRotation=Quaternion.Euler(0,0,side*(105+i*14));
                else Part("Tailored shoulder mantle",PrimitiveType.Sphere,new Vector3(0,0,.04f),new Vector3(heroClass==HeroClass.Ranger?.4f:.25f,.15f,.35f),cloth,shoulder,VisualSurface.Cloth);
            }
            Part("Costume fastening",PrimitiveType.Sphere,new Vector3(0,1.49f,.42f),Vector3.one*(.1f+look.Tier*.017f),look.Accent,equipmentArmor,VisualSurface.Metal);
            if(look.HasRunes)GlowingPart("Costume rune",PrimitiveType.Sphere,new Vector3(0,1.37f,.44f),Vector3.one*.08f,look.Glow,equipmentArmor);
        }
    }
}
