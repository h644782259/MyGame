using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Panel practiceReturnPanel;
        private bool practicePanelCaptured;
        internal void EnterPracticePanel(){if(practicePanelCaptured)return;practiceReturnPanel=panel;practicePanelCaptured=true;panel=Panel.None;CancelMobileScroll();CancelMobileCast();CancelHotbarPointer();}
        internal void LeavePracticePanel(){if(!practicePanelCaptured)return;panel=practiceReturnPanel;practicePanelCaptured=false;}
        private int practiceSeconds=10;
        private void DrawPracticeChoices(ref float y,float width,float unit,bool draw,bool enabled,ProgressionService.BuildDraft draft)
        {
            BuildPlanParagraph(ref y,width,unit,"营地试招：临时角色实际战斗，不保存、不获奖励。当前拥有装备；可使用尚未应用的合法草稿。旧三场为百万生命持续训练靶；新增受压场景使用当前等级真实生命并实际攻击。所有场景保留护甲与控制规则；准备后点击开始。",muted,draw);
            DraftButton(ref y,width,unit,"计时："+practiceSeconds+"秒 · 点击切换10/60秒",enabled,draw,()=>practiceSeconds=practiceSeconds==10?60:10);
            string[] names={"静止单目标","持续移动目标","前排 + 后排供能","守卫 + 魔灵 · 受压","前排 + 供能者 · 受压"};
            for(int i=0;i<5;i++){int index=i;DraftButton(ref y,width,unit,"试招 · "+names[i],enabled,draw,()=>session.BeginPractice((CampPracticeScenario)index,practiceSeconds,draft));}
            var record=session.PracticeRecord;if(record==null)return;
            BuildPlanParagraph(ref y,width,unit,PracticeSummary(record),pale,draw);
            if(session.PreviousPracticeRecord!=null)BuildPlanParagraph(ref y,width,unit,"上一轮 A：\n"+PracticeSummary(session.PreviousPracticeRecord)+"\n"+record.Comparison(session.PreviousPracticeRecord),gold,draw);
            BuildPlanParagraph(ref y,width,unit,"本轮 B 固定配装摘要：\n"+record.ConfigurationSummary,muted,draw);
            if(session.PreviousPracticeRecord!=null)BuildPlanParagraph(ref y,width,unit,"上一轮 A 固定配装摘要：\n"+session.PreviousPracticeRecord.ConfigurationSummary,muted,draw);
        }
        private string PracticeSummary(CampPracticeRecord record)
        {
            string text=new[]{"静止单目标","持续移动目标","前排 + 后排供能","守卫 + 魔灵 · 受压","前排 + 供能者 · 受压"}[(int)record.Scenario]+" · "+record.Elapsed.ToString("0.0")+" / "+record.Duration+"秒 · "+record.EndReason+"\n实际扣血 "+record.ActualDamage.ToString("0.0")+" · 耗能 "+record.EnergySpent.ToString("0.0")+" / 实际回复 "+record.EnergyRestored.ToString("0.0");
            foreach(var entry in record.SkillCasts){int hits;record.EffectiveSkillCasts.TryGetValue(entry.Key,out hits);text+="\n"+GameBalance.SkillName(session.Progression.Profile.heroClass,entry.Key)+"：有效命中施法 "+hits+" / "+entry.Value;}
            foreach(var entry in record.Mechanisms)text+="\n"+entry.Key+"：实际发生 "+entry.Value;
            return text;
        }
        private void DrawPracticeCombatHUD()
        {
            if(MobileControls.Active)DrawMobileHotbar();else DrawHotbar();
            DrawCompanionCommands();DrawChargeProgress();DrawTargetingHint();
            Text(new Rect(12,16,400,65),"临时角色 HP "+session.Player.Health.ToString("0")+" / "+session.Player.MaxHealth.ToString("0")+"\n能量 "+session.Player.Energy.ToString("0.0"),18,pale);
        }
        private void DrawPracticeOverlay()
        {
            var r=session.PracticeRecord;float w=Mathf.Min(width-24,620);
            Text(new Rect(12,100,w,160),"试招 "+r.Elapsed.ToString("0.0")+" / "+r.Duration+" 秒\n实际伤害 "+r.ActualDamage.ToString("0.0")+" · 耗能 "+r.EnergySpent.ToString("0.0")+" / 回复 "+r.EnergyRestored.ToString("0.0")+"\n无奖励 · H / Esc 离开；结束返回原草稿",18,pale);
            if(!r.Started){Rect start=new Rect(280,265,250,48);blockedRects.Add(start);if(Button(start,"准备完成 · 开始实战",jade))session.StartPractice();}
            Rect leave=new Rect(12,265,250,48);blockedRects.Add(leave);
            Rect refresh=new Rect(12,320,250,48);blockedRects.Add(refresh);
            if(Button(refresh,"刷新练习 · 恢复临时角色",jade))session.RestartPractice();
            if(Button(leave,"结束试招 · 返回原草稿",gold))session.EndPractice("主动离开 · 记录提前结束");
        }
    }
}
