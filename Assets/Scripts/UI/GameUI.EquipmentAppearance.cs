using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool equipmentAppearanceOpen,equipmentAppearanceCandidate,equipmentAppearanceDetail;
        private string equipmentAppearanceItem;
        private bool DrawEquipmentAppearanceDetail(Rect area,ItemData item,float u)
        {
            if(!equipmentAppearanceOpen||item==null)return false;
            if(equipmentAppearanceItem!=item.id){equipmentAppearanceItem=item.id;equipmentAppearanceCandidate=false;}
            float gap=8*u,half=(area.width-gap)*.5f;
            if(Button(new Rect(area.x,area.y,half,44*u),"当前装备",equipmentAppearanceCandidate?jade:gold)){equipmentAppearanceCandidate=false;BlockUITransition();}
            if(Button(new Rect(area.x+half+gap,area.y,half,44*u),"候选装备",equipmentAppearanceCandidate?gold:jade)){equipmentAppearanceCandidate=true;BlockUITransition();}
            if(Button(new Rect(area.x,area.y+50*u,half,40*u),equipmentAppearanceDetail?"细节视距":"战斗视距",jade)){equipmentAppearanceDetail=!equipmentAppearanceDetail;BlockUITransition();}
            if(Button(new Rect(area.x+half+gap,area.y+50*u,half,40*u),"返回属性",jade)){equipmentAppearanceOpen=false;ReleaseCollectionModel();BlockUITransition();return true;}
            var p=session.Progression;
            ItemData weapon=p.Equipped(ItemSlot.Weapon),armor=p.Equipped(ItemSlot.Armor),relic=p.Equipped(ItemSlot.Relic);
            // PreviewEquippedItem returns a detached copy with persistent slot upgrades.
            ItemData candidate=p.PreviewEquippedItem(item);
            if(equipmentAppearanceCandidate)
            {
                if(item.slot==ItemSlot.Weapon)weapon=candidate;
                else if(item.slot==ItemSlot.Armor)armor=candidate;
                else if(item.slot==ItemSlot.Relic)relic=candidate;
            }
            var wings=p.EquippedFashion(FashionSlot.Wings);var fashionWeapon=p.EquippedFashion(FashionSlot.Weapon);
            if(collectionModel==null)collectionModel=new CollectionModelPreview();
            collectionModel.SetComposition(CollectionPreviewComposition.Full);collectionModel.SetYaw(20);
            collectionModel.SetEquipmentFraming(true,equipmentAppearanceDetail);
            Rect viewport=new Rect(area.x,area.y+96*u,area.width,Mathf.Max(48*u,area.height-174*u));
            collectionModel.SetViewport(viewport.width*Mathf.Abs(GUI.matrix.m00),viewport.height*Mathf.Abs(GUI.matrix.m11),MobileControls.Active);
            Texture image=collectionModel.Render(p.Profile.heroClass,weapon,armor,relic,wings,fashionWeapon);
            Fill(viewport,new Color(.035f,.06f,.09f));if(image!=null)GUI.DrawTexture(viewport,image,ScaleMode.ScaleToFit,false);
            string note=(equipmentAppearanceCandidate?"候选":"当前")+" · 固定角度与视距 · 仅预览\n"+
                (fashionWeapon!=null?"保留已穿兵装外观：会覆盖装备武器轮廓。":"保留实际时装；候选继承部位强化 +"+p.SlotUpgradeRank(item.slot))+"\n装备/时装使用兼容模型；不改变穿戴或存档。";
            Text(new Rect(area.x,viewport.yMax+4*u,area.width,74*u),note,Mathf.RoundToInt(11*u),muted,false,true);
            return true;
        }
    }
}
