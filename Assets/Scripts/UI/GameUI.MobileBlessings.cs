using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileBlessingScroll;
        private RunBlessing[] mobileBlessingOffer;
        private PlayerController mobileBlessingOwner;
        private int mobileBlessingEpoch=-1,mobileBlessingWave=-1;
        private void DrawMobileBlessingChoice()
        {
            RunBlessing[] offer=session.RunChoices.Offer;
            bool changed=mobileBlessingOwner!=session.Player||mobileBlessingEpoch!=session.Player.CombatEpoch||
                mobileBlessingWave!=session.RunChoices.CompletedWave||mobileBlessingOffer==null||mobileBlessingOffer.Length!=offer.Length;
            if(!changed)for(int i=0;i<offer.Length;i++)if(offer[i]!=mobileBlessingOffer[i]){changed=true;break;}
            // Offer intentionally returns defensive copies; reference equality would
            // erase the selected card on every IMGUI event and prevent confirmation.
            if(changed)
            {mobileBlessingOffer=offer;mobileBlessingOwner=session.Player;mobileBlessingEpoch=session.Player.CombatEpoch;
             mobileBlessingWave=session.RunChoices.CompletedWave;mobileBlessingScroll=Vector2.zero;selectedBlessing=-1;}
            var layout=MobilePanelGeometry();
            if(DrawMobilePanelChrome(layout,"星烬祝福",session.RoomChainRun!=null?(session.RunChoices.CompletedWave==1?"首房 · 定打法 / 补资源与生存":"星泉 · 强化搭配 / 补短板"):"选择一项 · 仅本局生效",false,true))return;
            int count=offer.Length;if(count==0)return;
            float column=(layout.Body.Width-16-(count-1)*10)/count;
            float cardHeight=150;
            for(int i=0;i<count;i++)
            {
                float name=MeasureMobileParagraph(RunChoices.Name(offer[i]),column-20,17,true);
                float detail=MeasureMobileParagraph(RunChoices.Description(offer[i]),column-20,14);
                float association=MeasureMobileParagraph(RunChoices.Association(offer[i],session.Progression.Profile,true),column-20,12);
                cardHeight=Mathf.Max(cardHeight,name+detail+association+82);
            }
            Rect viewport=MobilePanelRect(layout.Body);
            mobileBlessingScroll=BeginTouchScroll("mobile-blessings",viewport,mobileBlessingScroll,
                new Rect(0,0,(layout.Body.Width-16)*TouchRatio,Mathf.Max(layout.Body.Height,cardHeight+8)*TouchRatio));
            for(int i=0;i<count;i++)
            {
                float x=i*(column+10);Rect cardRect=TouchRect(x,4,column,cardHeight);bool chosen=selectedBlessing==i;
                Fill(cardRect,chosen?new Color(.11f,.21f,.22f):card);Border(cardRect,chosen?gold:jade*.45f);
                float y=14;y+=DrawMobileParagraph(x+10,y,column-20,RunChoices.Name(offer[i]),17,chosen?gold:pale,true)+8;
                bool compatible=RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,true));
                y+=DrawMobileParagraph(x+10,y,column-20,RunChoices.Association(offer[i],session.Progression.Profile,true),12,compatible?jade:muted)+12;
                DrawMobileParagraph(x+10,y,column-20,RunChoices.Description(offer[i]),14,pale);
                Text(TouchRect(x+10,cardHeight-22,column-20,20),chosen?"已选择 ✓":"轻触选择",TouchFont(13),chosen?gold:muted,true,false,TextAnchor.MiddleCenter);
                if(GUI.Button(cardRect,GUIContent.none,invisibleButton))selectedBlessing=i;
            }
            EndTouchScroll();
            if(Button(MobilePanelRect(layout.FooterButton(0,2)),"暂停 / 存档",jade))
            {session.SetPaused(true);BlockUITransition();return;}
            if(Button(MobilePanelRect(layout.FooterButton(1,2)),"确认祝福并继续",gold,selectedBlessing>=0&&selectedBlessing<count))
            {
                if(session.ConfirmBlessing(selectedBlessing)){selectedBlessing=-1;CancelMobileScroll();BlockUITransition();}
            }
        }
    }
}
