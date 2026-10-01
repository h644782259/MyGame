using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 equipmentMechanismScroll;
        private string equipmentMechanismItem;

        // All inventory comparisons use the attributes the item will have after
        // equipping. A stored per-item rank is not the persistent slot rank.
        private ItemData EquipmentPreview(ItemData item)
        {
            return session.Progression.PreviewEquippedItem(item);
        }

        private float EquipmentPreviewScore(ItemData item)
        {
            return ProgressionService.EquipmentScore(EquipmentPreview(item));
        }

        private void DrawPersistentMechanismDetail(Rect area,ItemData current,ItemData candidate)
        {
            if(equipmentMechanismItem!=candidate.id){equipmentMechanismItem=candidate.id;equipmentMechanismScroll=Vector2.zero;}
            var hero=session.Progression.Profile.heroClass;
            string text="换装后 · "+EquipmentComparisonPresentation.Description(candidate,hero);
            if(!EquipmentComparisonPresentation.SameMechanism(current,candidate,hero))text+="\n原机制 · "+EquipmentComparisonPresentation.Description(current,hero);
            float contentWidth=area.width-16;
            float h=Style(12,false,true,TextAnchor.UpperLeft).CalcHeight(new GUIContent(text),contentWidth);
            equipmentMechanismScroll=BeginTouchScroll("equipment-mechanism-detail",area,equipmentMechanismScroll,new Rect(0,0,contentWidth,Mathf.Max(area.height,h+4)));
            Text(new Rect(0,0,contentWidth,h),text,12,muted,false,true);EndTouchScroll();
        }

        private void DrawEquipmentComparison(Rect r, ItemData equipped, ItemData candidate)
        {
            float current=ProgressionService.EquipmentScore(equipped);
            ItemData preview=EquipmentPreview(candidate);
            float next=ProgressionService.EquipmentScore(preview);
            float diff=next-current;
            float half=(r.width-12)*.5f;
            Fill(new Rect(r.x,r.y,half,r.height),new Color(.035f,.075f,.1f));
            Fill(new Rect(r.x+half+12,r.y,half,r.height),new Color(.065f,.115f,.14f));
            Text(new Rect(r.x+10,r.y+5,half-20,17),equipped==null?"当前装备 · 空槽":"当前装备评分",12,muted);
            Text(new Rect(r.x+10,r.y+25,half-20,31),current.ToString("0.#"),25,pale,true);
            Text(new Rect(r.x+half+22,r.y+5,half-20,17),"换装后评分",12,jade);
            Text(new Rect(r.x+half+22,r.y+25,half-80,31),next.ToString("0.#"),25,pale,true);
            string delta=Mathf.Approximately(diff,0)?"±0":(diff>0?"+":"")+diff.ToString("0.#");
            Text(new Rect(r.xMax-83,r.y+28,73,27),delta,18,diff<0?new Color(1,.48f,.42f):diff>0?jade:muted,true,false,TextAnchor.MiddleRight);
            if(r.Contains(Mouse))tooltip="同部位：攻击 ×5 + 防御 ×3 + 生命 ×0.2\n当前："+(equipped==null?"空槽":ItemTitle(equipped))+"\n换装后："+ItemTitle(preview)+"\n自动继承部位强化 +"+session.Progression.SlotUpgradeRank(candidate.slot)+"，加成按该装备自身基础属性计算。机制效果不计入分数。";
        }
    }
}
