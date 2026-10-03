using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private enum BuildPlanAction { None, Reset, Save, Apply }
        private bool buildPlansOpen;
        private BuildPlanAction buildPlanAction;
        private int buildPlanSlot;
        private ProgressionService buildPlanOwner;
        private PlayerController buildPlanHero;
        private string buildPlanCharacterId;
        private GameProfile buildPlanSource;
        private string buildPlanPreview, buildPlanError;
        private Vector2 buildPlanScroll;

        private void OpenBuildPlans()
        {
            buildPlansOpen=true;buildPlanAction=BuildPlanAction.None;buildPlanOwner=session.Progression;
            buildPlanHero=session.Player;buildPlanCharacterId=session.Progression.CurrentSlotId;
            buildPlanError=null;buildPlanScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private void RequestBuildPlanAction(BuildPlanAction action,int slot=0)
        {
            var p=session.Progression;
            buildPlansOpen=true;buildPlanOwner=p;buildPlanSource=p.Profile;
            buildPlanHero=session.Player;buildPlanCharacterId=p.CurrentSlotId;
            buildPlanAction=action;buildPlanSlot=slot;
            buildPlanPreview=action==BuildPlanAction.Apply?p.BuildPresetSummary(slot):p.CurrentBuildSummary();
            buildPlanError=action==BuildPlanAction.Apply?p.BuildPresetLockReason(slot,session.IsInCamp):null;
            buildPlanScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();
        }
        private bool CloseBuildPlanSurface()
        {
            if(!buildPlansOpen||panel!=Panel.Camp)return false;
            if(allocationDraft!=null)CancelAllocationDraft();
            else if(buildPlanAction!=BuildPlanAction.None)buildPlanAction=BuildPlanAction.None;
            else buildPlansOpen=false;
            buildPlanError=null;buildPlanScroll=Vector2.zero;
            CancelMobileScroll();BlockUITransition();return true;
        }
        private void ResetBuildPlanSurface()
        {
            CancelAllocationDraft();
            buildPlansOpen=false;buildPlanAction=BuildPlanAction.None;
            buildPlanOwner=null;buildPlanSource=null;buildPlanHero=null;buildPlanCharacterId=null;
            buildPlanPreview=buildPlanError=null;buildPlanScroll=Vector2.zero;
        }
        private void ReconcileBuildPlanSurface()
        {
            if(buildPlansOpen&&(panel!=Panel.Camp||buildPlanOwner!=session.Progression||
                buildPlanHero!=session.Player||buildPlanCharacterId!=session.Progression.CurrentSlotId))ResetBuildPlanSurface();
        }
        private bool DrawBuildPlanSurface()
        {
            if(!buildPlansOpen)return false;
            ReconcileBuildPlanSurface();
            if(!buildPlansOpen)return false;
            if(allocationDraft!=null)return DrawAllocationDraftSurface();
            float unit=MobileControls.Active?TouchRatio:1;
            var layout=new MobileDialogLayout(width/unit,height/unit);
            bool confirm=buildPlanAction!=BuildPlanAction.None;
            string title=buildPlanAction==BuildPlanAction.Reset?"免费重置配点？":buildPlanAction==BuildPlanAction.Save?
                (session.Progression.HasBuildPreset(buildPlanSlot)?"覆盖配装方案 ":"记录配装方案 ")+(buildPlanSlot+1)+"？":
                buildPlanAction==BuildPlanAction.Apply?"应用配装方案 "+(buildPlanSlot+1)+"？":"配装方案 · 两套";
            Fill(new Rect(0,0,width,height),new Color(.008f,.018f,.03f,1));
            blockedRects.Add(new Rect(0,0,width,height));
            Box(BuildPlanRect(layout.Frame,unit),gold,false);
            Text(BuildPlanRect(layout.Header,unit),title,Mathf.RoundToInt(21*unit),pale,true);
            float contentWidth=layout.Body.Width-18;
            float contentHeight=DrawBuildPlanContent(contentWidth,unit,false);
            buildPlanScroll=BeginTouchScroll("build-plans",BuildPlanRect(layout.Body,unit),buildPlanScroll,
                new Rect(0,0,contentWidth*unit,Mathf.Max(layout.Body.Height,contentHeight)*unit));
            DrawBuildPlanContent(contentWidth,unit,true);
            EndTouchScroll();
            if(Button(BuildPlanRect(layout.FooterButton(0,2),unit),confirm?"取消":"返回营地工坊",jade))
            {CloseBuildPlanSurface();return true;}
            if(confirm)
            {
                bool fresh=buildPlanSource==session.Progression.Profile;
                string reason=buildPlanAction==BuildPlanAction.Apply?session.Progression.BuildPresetLockReason(buildPlanSlot,session.IsInCamp):null;
                string caption=!fresh?"重新核对":buildPlanAction==BuildPlanAction.Reset?"确认重置配点":buildPlanAction==BuildPlanAction.Save?"确认记录方案":"确认应用方案";
                if(Button(BuildPlanRect(layout.FooterButton(1,2),unit),caption,gold,!fresh||session.IsInCamp&&string.IsNullOrEmpty(reason)))
                {if(fresh)ConfirmBuildPlanAction();else RequestBuildPlanAction(buildPlanAction,buildPlanSlot);}
            }
            else if(Button(BuildPlanRect(layout.FooterButton(1,2),unit),"免费重置 · "+session.Progression.RefundableBuildPoints+"点",gold,
                session.IsInCamp&&(session.Progression.RefundableBuildPoints>0||session.Progression.Profile.masteryCore>=0)))
                RequestBuildPlanAction(BuildPlanAction.Reset);
            return true;
        }
        private static Rect BuildPlanRect(MobilePanelLayout.Area area,float unit)
        {return new Rect(area.X*unit,area.Y*unit,area.Width*unit,area.Height*unit);}
        private void BuildPlanParagraph(ref float y,float width,float unit,string value,Color color,bool draw,bool bold=false)
        {
            if(string.IsNullOrEmpty(value))return;
            int size=Mathf.RoundToInt(14*unit);
            float h=Mathf.Ceil(Style(size,bold,true).CalcHeight(new GUIContent(value),(width-16)*unit)/unit)+2;
            if(draw)Text(new Rect(8*unit,y*unit,(width-16)*unit,h*unit),value,size,color,bold,true);
            y+=h+10;
        }
        private float DrawBuildPlanContent(float width,float unit,bool draw)
        {
            var p=session.Progression;float y=4;
            BuildPlanParagraph(ref y,width,unit,buildPlanError,gold,draw,true);
            if(buildPlanAction!=BuildPlanAction.None)
            {
                if(buildPlanSource!=p.Profile)
                    BuildPlanParagraph(ref y,width,unit,"当前角色的配装或装备已变化。请点「重新核对」刷新预览，再确认；也可取消。",gold,draw,true);
                BuildPlanParagraph(ref y,width,unit,buildPlanPreview,pale,draw,true);
                if(buildPlanAction==BuildPlanAction.Reset)
                {
                    BuildPlanParagraph(ref y,width,unit,"返还技能进阶 "+p.RefundableSkillRanks+"点 + 精通 "+p.RefundableMasteryPoints+"点 = 共 "+p.RefundableBuildPoints+"点",gold,draw,true);
                    BuildPlanParagraph(ref y,width,unit,"保留已学1阶、技能前置、快捷栏、当前装备和配装方案；精通核心关闭。当前技能冷却不重置。",muted,draw);
                }
                else if(buildPlanAction==BuildPlanAction.Save)
                {
                    if(p.HasBuildPreset(buildPlanSlot))
                    {
                        BuildPlanParagraph(ref y,width,unit,"将覆盖下列原方案：",gold,draw,true);
                        BuildPlanParagraph(ref y,width,unit,p.BuildPresetSummary(buildPlanSlot),muted,draw);
                    }
                    BuildPlanParagraph(ref y,width,unit,"记录当前技能、精通 / 核心、职业路线、快捷栏和三件装备的编号。只记录配装，不创建新角色存档，也不复制装备。",muted,draw);
                }
                else
                {
                    string reason=p.BuildPresetLockReason(buildPlanSlot,session.IsInCamp);
                    if(reason!=buildPlanError)BuildPlanParagraph(ref y,width,unit,reason,gold,draw,true);
                    BuildPlanParagraph(ref y,width,unit,"将切换为上面的配装。之后新学的1阶技能保留；点数、前置、等级和装备编号均会重新校验。当前技能冷却不重置。",muted,draw);
                }
                if(!session.IsInCamp)BuildPlanParagraph(ref y,width,unit,"请先安全返回营地再操作。",gold,draw);
                return y;
            }
            DraftButton(ref y,width,unit,"局部调整配点 · 临时草稿",session.IsInCamp,draw,OpenAllocationDraft);
            BuildPlanParagraph(ref y,width,unit,"当前配装",gold,draw,true);
            BuildPlanParagraph(ref y,width,unit,p.CurrentBuildSummary(),pale,draw);
            BuildPlanParagraph(ref y,width,unit,"免费重置预览：技能进阶 "+p.RefundableSkillRanks+"点 + 精通 "+p.RefundableMasteryPoints+"点 = "+p.RefundableBuildPoints+"点。保留已学1阶，关闭精通核心。",muted,draw);
            for(int slot=0;slot<ProgressionService.BuildPresetCount;slot++)
            {
                bool occupied=p.HasBuildPreset(slot);
                BuildPlanParagraph(ref y,width,unit,"配装方案 "+(slot+1)+(occupied?"":" · 空位"),gold,draw,true);
                if(occupied)BuildPlanParagraph(ref y,width,unit,p.BuildPresetSummary(slot),pale,draw);
                string reason=p.BuildPresetLockReason(slot,session.IsInCamp);
                if(occupied&&!string.IsNullOrEmpty(reason))BuildPlanParagraph(ref y,width,unit,reason,gold,draw);
                if(draw)
                {
                    float buttonWidth=(width-24)*.5f;
                    if(Button(new Rect(8*unit,y*unit,buttonWidth*unit,48*unit),occupied?"覆盖为当前配装":"记录当前配装",jade,session.IsInCamp))
                        RequestBuildPlanAction(BuildPlanAction.Save,slot);
                    if(Button(new Rect((16+buttonWidth)*unit,y*unit,buttonWidth*unit,48*unit),"应用方案 "+(slot+1),gold,occupied&&string.IsNullOrEmpty(reason)))
                        RequestBuildPlanAction(BuildPlanAction.Apply,slot);
                }
                y+=64;
            }
            BuildPlanParagraph(ref y,width,unit,"方案属于当前角色。缺失或等级不足的装备会阻止应用；不会凭名称寻找替代装备或复制已出售物品。",muted,draw);
            return y;
        }
        private void ConfirmBuildPlanAction()
        {
            var p=session.Progression;
            if(buildPlanAction==BuildPlanAction.None||buildPlanOwner!=p||buildPlanSource!=p.Profile)return;
            bool accepted=buildPlanAction==BuildPlanAction.Reset?p.ResetBuild(session.IsInCamp):
                buildPlanAction==BuildPlanAction.Save?p.SaveBuildPreset(buildPlanSlot,session.IsInCamp):p.ApplyBuildPreset(buildPlanSlot,session.IsInCamp);
            if(!accepted)
                buildPlanError=string.IsNullOrEmpty(p.LastError)?"操作未保存，请重试或取消。":p.LastError;
            else
            {
                string result=buildPlanAction==BuildPlanAction.Reset?"配点已重置；技能进阶与精通点已返还":
                    buildPlanAction==BuildPlanAction.Save?"配装方案 "+(buildPlanSlot+1)+" 已记录":"已应用配装方案 "+(buildPlanSlot+1);
                buildPlanAction=BuildPlanAction.None;buildPlanError=null;session.Notify(result);
            }
            buildPlanScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();
        }
    }
}
