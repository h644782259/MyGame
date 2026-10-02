using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly Texture2D[] rewardChestTextures = new Texture2D[13];
        private bool chestDetails, chestOpening;
        private int revealedChest = -1;
        private string chestRevealResult;
        private float chestRevealedAt;
        private bool rewardSoundPlayed;
        private string chestReceiptId;
        private float ChestDuration { get { var reward=session.Progression.LastChestReward; return EffectPreferences.ReducedEffects ? .15f : reward == null || !reward.Rarity.HasValue ? .95f : 1.1f + (int)reward.Rarity.Value * .28f; } }
        private bool ChestAnimationDone { get { return chestRevealResult != null && Time.unscaledTime-chestRevealedAt >= ChestDuration; } }

        private void ResetChestReveal()
        {
            chestDetails = false;
            chestOpening = false;
            revealedChest = -1;
            chestRevealResult = null;
            chestReceiptId = null;
            rewardSoundPlayed = false;
            desktopChestResultScroll=Vector2.zero;
            if (session.Progression.Profile.pendingChestReveal && session.Progression.LastChestReward != null)
            {
                var reward=session.Progression.LastChestReward;
                revealedChest=Mathf.Clamp(reward.choice,0,2); chestRevealResult=reward.summary; chestReceiptId=reward.Id;
                chestRevealedAt=Time.unscaledTime-ChestDuration; rewardSoundPlayed=true;
            }
        }

        private void ReleaseChestTextures()
        {
            foreach (Texture2D texture in rewardChestTextures) if (texture != null) Destroy(texture);
        }

        private Vector2 desktopChestResultScroll;
        private void DrawChests()
        {
            if(MobileControls.Active){DrawMobileChests();return;}
            var progression=session.Progression;var savedReward=progression.LastChestReward;
            if(progression.Profile.pendingChestReveal&&savedReward!=null&&chestReceiptId!=savedReward.Id)ResetChestReveal();
            if(!progression.Profile.pendingChestReveal&&chestRevealResult!=null)ResetChestReveal();
            bool revealed=chestRevealResult!=null&&progression.Profile.pendingChestReveal,complete=revealed&&ChestAnimationDone;
            var reward=revealed?savedReward:null;
            Color accent=reward!=null&&reward.Rarity.HasValue?GameBalance.RarityColor(reward.Rarity.Value):gold;
            if(revealed&&complete&&!rewardSoundPlayed)
            {rewardSoundPlayed=true;GameAudio.Play(reward==null||!reward.Rarity.HasValue?SoundCue.UI:reward.Rarity.Value==Rarity.Legendary?SoundCue.Victory:reward.Rarity.Value==Rarity.Epic?SoundCue.LevelUp:reward.Rarity.Value==Rarity.Rare?SoundCue.Loot:SoundCue.Cast);}
            Fill(new Rect(0,0,width,height),new Color(.018f,.025f,.045f,.9f));
            float ww=Mathf.Min(860,width-32),wh=Mathf.Min(550,height-24);
            Rect w=new Rect((width-ww)*.5f,(height-wh)*.5f,ww,wh);
            Fill(w,new Color(.045f,.064f,.095f,.99f));Border(w,new Color(.52f,.60f,.67f,.3f));
            Text(new Rect(w.x+28,w.y+20,w.width-56,18),"F A L L E N   S T A R",10,gold,true);
            Text(new Rect(w.x+28,w.y+45,w.width-248,42),revealed?(complete?"星光已归你所有":"封印正在苏醒"):"遗迹馈赠",28,pale,true);
            Text(new Rect(w.x+28,w.y+92,w.width-56,24),revealed?(complete?ChestRevealPresentation.Outcome(reward):"已保存奖励 · 可以跳过揭晓动画"):"三份机会相同 · 只开启你选中的一份",14,muted);
            if(Button(new Rect(w.xMax-200,w.y+43,78,36),"菜单",jade)){session.SetPaused(true);BlockUITransition();return;}
            if(Button(new Rect(w.xMax-110,w.y+43,82,36),chestDetails?"收起规则":"奖励规则",muted))chestDetails=!chestDetails;
            Rect body=new Rect(w.x+28,w.y+132,w.width-56,w.height-208);
            if(chestDetails)DrawDesktopChestRules(body);
            else if(complete)DrawDesktopChestResult(body,reward,accent);
            else if(revealed)DrawChestRevealTransition(body,reward);
            else
            {
                float cardWidth=(body.width-24)/3;
                for(int i=0;i<3;i++)
                {
                    Rect r=new Rect(body.x+i*(cardWidth+12),body.y,cardWidth,body.height);
                    bool hover=r.Contains(Mouse)&&GUI.enabled;
                    Fill(r,new Color(.07f,.09f,.125f));Border(r,hover?gold:new Color(.32f,.42f,.49f,.65f));
                    Text(new Rect(r.x+14,r.y+12,r.width-28,20),i==0?"I":i==1?"II":"III",12,muted);
                    DrawRewardChest(new Rect(r.x+12,r.y+30,r.width-24,r.height-98),false,1,0);
                    if(Button(new Rect(r.x+12,r.yMax-54,r.width-24,42),"开启",gold,!chestOpening&&progression.Profile.pendingFashionChest&&!progression.Profile.pendingChestReveal,null,hover))
                    {
                        chestOpening=true;string result=progression.OpenDungeonChest(i);
                        if(result==null){chestOpening=false;Feedback(false,"宝箱暂时无法开启");}
                        else {revealedChest=i;chestRevealResult=result;chestRevealedAt=Time.unscaledTime;chestDetails=false;rewardSoundPlayed=false;chestReceiptId=progression.LastChestReward.Id;desktopChestResultScroll=Vector2.zero;GameAudio.Play(SoundCue.Cast);}
                        BlockUITransition();return;
                    }
                }
            }
            if(revealed)
            {
                Text(new Rect(w.x+28,w.yMax-55,w.width-250,36),complete?"奖励已保存 · 收下后返回冒险":"未选宝箱逐渐封存，不再参与抽取",13,muted,false,true);
                if(Button(new Rect(w.xMax-208,w.yMax-58,180,42),complete?"收下":"跳过动画",jade,!chestDetails,null,true))
                {if(!complete)chestRevealedAt=Time.unscaledTime-ChestDuration;else FinishChestReveal();BlockUITransition();}
            }
            else Text(new Rect(w.x+28,w.yMax-50,w.width-56,36),string.IsNullOrEmpty(progression.LastError)?"开启后奖励先保存，再展示结果":progression.LastError,13,muted,false,true);
        }
        private void DrawDesktopChestRules(Rect r)
        {
            int minimum=TierRewardRules.ChestGoldMinimum(session.Progression.Profile.pendingChestTier);
            string rules="三份宝箱机会完全相同，每次只可开启一份。\n\n金币 "+minimum+"～"+(minimum+40)+"，另有机会获得时装。\n普通22% · 稀有12% · 史诗5% · 传说1% · 无时装60%\n\n重复时装转金币并额外增加星纹，每次开启均增加星纹。\n奖励先保存再展示；跳过动画不会重新抽取。";
            Text(new Rect(r.x+10,r.y+8,r.width-20,r.height-16),rules,15,pale,false,true);
        }
        private void DrawDesktopChestResult(Rect r,ChestReward reward,Color accent)
        {
            float size=ChestRevealPresentation.DesktopArtSize(r.height);
            Rect art=new Rect(r.x,r.y,size,size);Fill(art,new Color(.025f,.045f,.07f));Border(art,accent);
            if(!DrawChestRewardModel(art,reward))DrawChestGold(art,accent);
            Rect details=new Rect(art.xMax+24,r.y,r.width-size-24,r.height);
            string result=ChestRevealPresentation.Result(reward,session.Progression.Profile.fashionThreads);
            string error=session.Progression.LastError;
            string copy=(string.IsNullOrEmpty(error)?"":error+"\n\n")+result;
            float total=Mathf.Max(details.height,Style(18,true,true).CalcHeight(new GUIContent(copy),details.width-26)+20);
            desktopChestResultScroll=BeginTouchScroll("desktop-chest-result",details,desktopChestResultScroll,new Rect(0,0,details.width-16,total));
            Text(new Rect(4,8,details.width-26,total-16),copy,18,accent,true,true);EndTouchScroll();
        }
        private void DrawChestRevealTransition(Rect r,ChestReward reward)
        {
            float progress=ChestRevealPresentation.Progress(Time.unscaledTime-chestRevealedAt,ChestDuration);
            float cardWidth=(r.width-16)/3;
            Color accent=reward!=null&&reward.Rarity.HasValue?GameBalance.RarityColor(reward.Rarity.Value):gold;
            for(int i=0;i<3;i++)
            {
                bool chosen=i==revealedChest;float opacity=chosen?1:ChestRevealPresentation.UnselectedOpacity(progress);
                if(opacity<=0)continue;
                Rect cardRect=new Rect(r.x+i*(cardWidth+8),r.y,cardWidth,r.height);
                Color shade=chosen?new Color(.12f,.14f,.18f):new Color(.07f,.09f,.125f);shade.a*=opacity;Fill(cardRect,shade);
                DrawRewardChest(new Rect(cardRect.x+6,cardRect.y+8,cardRect.width-12,cardRect.height-38),chosen,opacity,chosen?progress:0);
                if(chosen&&progress>.35f)
                {Rect clip=new Rect(cardRect.x+6,cardRect.y+8,cardRect.width-12,cardRect.height-38);GUI.BeginGroup(clip);DrawRewardRadiance(new Rect(0,0,clip.width,clip.height),accent,progress);GUI.EndGroup();}
                Text(new Rect(cardRect.x+4,cardRect.yMax-28,cardRect.width-8,22),chosen?"正在揭晓":"未选 · 封存",12,new Color(muted.r,muted.g,muted.b,opacity),chosen,false,TextAnchor.MiddleCenter);
            }
        }
        private void DrawChestGold(Rect area,Color accent)
        {
            float size=Mathf.Min(area.width,area.height),unit=size/200f;
            for(int i=0;i<3;i++)
            {Rect bar=new Rect(area.center.x-52*unit+(i-1)*8*unit,area.center.y+(1-i)*24*unit,104*unit,28*unit);Fill(bar,accent*(.65f+i*.12f));Border(bar,gold);}
            Text(new Rect(area.x,area.yMax-38*unit,area.width,28*unit),"金币已入账",Mathf.RoundToInt(16*unit),accent,true,false,TextAnchor.MiddleCenter);
        }

        private void FinishChestReveal()
        {
            if (!session.Progression.AcknowledgeChestReward()) { Feedback(false,"无法保存奖励确认"); return; }
            session.LogSystem(chestRevealResult);
            panel=Panel.None; session.SetUIBlocking(false); ResetChestReveal();
        }

        private void DrawRewardRadiance(Rect r, Color tint, float progress)
        {
            float strength=Mathf.Sin(Mathf.Clamp01((progress-.35f)/.65f)*Mathf.PI)*EffectPreferences.EffectsScale;
            int count=EffectPreferences.ReducedEffects?4:12;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                float distance=23+progress*45;
                Vector2 p=r.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
                Color c=new Color(tint.r,tint.g,tint.b,strength*.72f);
                Fill(new Rect(p.x-1,p.y-4,2,8),c);Fill(new Rect(p.x-4,p.y-1,8,2),c);
            }

        }

        private void DrawRewardChest(Rect r, bool opened, float opacity, float progress)
        {
            // Interpolate cached poses at display rate instead of stepping through
            // thirteen hard frames. The cache stays fixed at thirteen 256px textures.
            float frame=opened?Mathf.Clamp01((progress-.12f)/.64f)*12:0;
            int lower=Mathf.Clamp(Mathf.FloorToInt(frame),0,12),upper=Mathf.Min(12,lower+1);
            if(rewardChestTextures[lower]==null)rewardChestTextures[lower]=BakeRewardChest(lower/12f);
            if(rewardChestTextures[upper]==null)rewardChestTextures[upper]=BakeRewardChest(upper/12f);
            Color before=GUI.color;
            float mix=frame-lower;
            GUI.color=new Color(1,1,1,opacity*(1-mix));GUI.DrawTexture(r,rewardChestTextures[lower],ScaleMode.ScaleToFit,true);
            if(mix>0){GUI.color=new Color(1,1,1,opacity*mix);GUI.DrawTexture(r,rewardChestTextures[upper],ScaleMode.ScaleToFit,true);}
            GUI.color=before;
        }

        private static Texture2D BakeRewardChest(float opening)
        {
            const int size = 256;
            bool opened = opening > .2f;
            Color[] pixels = new Color[size * size];
            // Soft radial light, rather than a flashing full-screen effect.
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * .5f) / (size * .43f);
                float dy = (y - size * .52f) / (size * .45f);
                float a = Mathf.Pow(Mathf.Max(0, 1 - dx * dx - dy * dy), 3) * (opened ? .32f : .18f);
                pixels[y * size + x] = new Color(opened ? 1f : .25f, opened ? .71f : .74f, opened ? .31f : .78f, a);
            }
            Color trim = new Color(.91f, .72f, .39f), light = new Color(1, .88f, .59f);
            Color front = new Color(.12f, .25f, .29f), side = new Color(.07f, .15f, .20f), top = new Color(.19f, .35f, .37f);
            ChestPolygon(pixels, size, side, new Vector2(27, 85), new Vector2(79, 97), new Vector2(103, 81), new Vector2(51, 70));
            ChestPolygon(pixels, size, front, new Vector2(27, 63), new Vector2(79, 76), new Vector2(79, 99), new Vector2(27, 85));
            ChestPolygon(pixels, size, side, new Vector2(79, 76), new Vector2(103, 61), new Vector2(103, 84), new Vector2(79, 99));
            ChestPolygon(pixels, size, new Color(.045f, .07f, .09f), new Vector2(28, 61), new Vector2(54, 47), new Vector2(102, 60), new Vector2(79, 75));
            float lift = Mathf.SmoothStep(0,1,opening) * 24;
            ChestPolygon(pixels, size, top, new Vector2(25, 59 - lift), new Vector2(50, 43 - lift), new Vector2(105, 57 - lift), new Vector2(79, 73 - lift));
            ChestPolygon(pixels, size, front, new Vector2(25, 59 - lift), new Vector2(79, 73 - lift), new Vector2(79, 81 - lift), new Vector2(25, 67 - lift));
            ChestPolygon(pixels, size, side, new Vector2(79, 73 - lift), new Vector2(105, 57 - lift), new Vector2(105, 65 - lift), new Vector2(79, 81 - lift));
            CrestStroke(pixels, size, 25, 59 - lift, 79, 73 - lift, light, 1.5f);
            CrestStroke(pixels, size, 79, 73 - lift, 105, 57 - lift, trim, 1.5f);
            CrestStroke(pixels, size, 25, 67 - lift, 79, 81 - lift, trim, 1.2f);
            CrestStroke(pixels, size, 79, 81 - lift, 105, 65 - lift, trim, 1.2f);
            CrestStroke(pixels, size, 27, 85, 79, 99, trim, 1.2f);
            CrestStroke(pixels, size, 79, 99, 103, 84, trim, 1.2f);
            CrestStroke(pixels, size, 34, 67, 34, 85, trim, 2.8f);
            CrestStroke(pixels, size, 71, 77, 71, 95, trim, 2.8f);
            CrestStroke(pixels, size, 95, 69, 95, 86, trim, 2.6f);
            CrestStroke(pixels, size, 37, 56 - lift, 91, 69 - lift, trim, 2.1f);
            CrestStroke(pixels, size, 45, 50 - lift, 99, 63 - lift, trim, 2.1f);
            if(opening < .5f)
            {
                float shift=opening*14;
                ChestPolygon(pixels, size, trim, new Vector2(49,72+shift),new Vector2(59,75+shift),new Vector2(59,87+shift),new Vector2(49,84+shift));
                ChestPolygon(pixels,size,new Color(.43f,1,.84f),new Vector2(54,75+shift),new Vector2(57,81+shift),new Vector2(54,85+shift),new Vector2(51,79+shift));
            }
            if (opened)
            {
                for (int i = 0; i < 9; i++)
                {
                    float x = 33 + (i * 17 % 62), y = 35 + (i * 19 % 29);
                    CrestStroke(pixels, size, x - 1, y, x + 1, y, light, 1);
                    CrestStroke(pixels, size, x, y - 2, x, y + 2, light, .8f);
                }
            }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels); texture.Apply(false, true); return texture;
        }

        private static void ChestPolygon(Color[] pixels, int size, Color color, params Vector2[] points)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + .5f) * 128 / size, (y + .5f) * 128 / size);
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    if ((points[i].y > p.y) != (points[j].y > p.y) && p.x < (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
                if (inside) pixels[(size - y - 1) * size + x] = color;
            }
        }
    }
}
