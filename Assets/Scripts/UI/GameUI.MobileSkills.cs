using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileSkillListScroll, mobileSkillDetailScroll;
        private ProgressionService mobileSkillsService;
        private string mobileSkillStatus;
        private bool mobileSkillStatusFailed;

        private void DrawMobileSkills()
        {
            var progression = session.Progression;
            if (mobileSkillsService != progression)
            {
                mobileSkillsService = progression;
                mobileSkillListScroll = mobileSkillDetailScroll = Vector2.zero;
                mobileSkillStatus = null;
            }
            GameProfile profile = progression.Profile;
            selectedSkill = Mathf.Clamp(selectedSkill, 0, GameBalance.SkillCount - 1);
            var layout = MobilePanelGeometry();
            if (DrawMobilePanelChrome(layout, GameBalance.ClassName(profile.heroClass) + " · 技能",
                "Lv." + profile.level + " · 可用技能点 " + profile.skillPoints + " · 上下滑动查看全部10项")) return;

            float u = TouchRatio, listWidth = layout.BodyLeft.Width - 16, detailWidth = layout.BodyRight.Width - 16;
            float listHeight = DrawMobileSkillRows(listWidth, false);
            mobileSkillListScroll = BeginTouchScroll("mobile-skill-list", MobilePanelRect(layout.BodyLeft), mobileSkillListScroll,
                new Rect(0, 0, listWidth * u, Mathf.Max(layout.BodyLeft.Height, listHeight) * u));
            DrawMobileSkillRows(listWidth, true);
            EndTouchScroll();

            float detailHeight = DrawMobileSkillDescription(detailWidth, false);
            mobileSkillDetailScroll = BeginTouchScroll("mobile-skill-detail", MobilePanelRect(layout.BodyRight), mobileSkillDetailScroll,
                new Rect(0, 0, detailWidth * u, Mathf.Max(layout.BodyRight.Height, detailHeight) * u));
            DrawMobileSkillDescription(detailWidth, true);
            EndTouchScroll();

            if (Button(MobilePanelRect(layout.FooterButton(0, 2)), "返回冒险", jade))
            { ClosePanel(); BlockUITransition(); return; }
            int rank = progression.Profile.skillRanks[selectedSkill];
            string reason = progression.SkillLockReason(selectedSkill);
            string caption = rank >= 3 ? "已完全觉醒" : (rank == 0 ? "学习初习" : "进阶" + GameBalance.SkillRankName(rank + 1)) + " · 1点";
            Rect learn = MobilePanelRect(layout.FooterButton(1, 2));
            if (Button(learn, caption, gold, string.IsNullOrEmpty(reason)))
            {
                bool saved = progression.LearnSkill(selectedSkill) && string.IsNullOrEmpty(progression.LastError);
                mobileSkillStatusFailed = !saved;
                mobileSkillStatus = saved ? GameBalance.SkillName(progression.Profile.heroClass, selectedSkill) + "已达到" +
                    GameBalance.SkillRankName(progression.Profile.skillRanks[selectedSkill]) : progression.LastError;
                CancelMobileScroll();
                mobileSkillDetailScroll = Vector2.zero;
                Feedback(saved, mobileSkillStatus);
                BlockUITransition();
            }
            Badge(learn, Attention.LearnableSkills.Contains(selectedSkill));
        }

        private float DrawMobileSkillRows(float width, bool draw)
        {
            var p = session.Progression.Profile;
            float y = 4;
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                int rank = p.skillRanks[skill];
                string name = (skill + 1) + ". " + GameBalance.SkillName(p.heroClass, skill);
                string state = GameBalance.SkillRankName(rank) + " · " + (GameBalance.IsPassive(skill) ? "被动" : "主动") +
                    "\nLv." + GameBalance.SkillRequiredLevels[skill] + (Attention.LearnableSkills.Contains(skill) ? " · 可学习 / 进阶" : "");
                float nameHeight = MeasureMobileParagraph(name, width - 20, 15, true);
                float stateHeight = MeasureMobileParagraph(state, width - 20, 14);
                float h = Mathf.Max(48, nameHeight + stateHeight + 24);
                if (draw)
                {
                    Rect row = TouchRect(0, y, width, h);
                    Fill(row, selectedSkill == skill ? new Color(.10f, .20f, .23f) : card);
                    Border(row, selectedSkill == skill ? gold : jade * .4f);
                    DrawMobileParagraph(10, y + 8, width - 20, name, 15, pale, true);
                    DrawMobileParagraph(10, y + 12 + nameHeight, width - 20, state, 14, rank > 0 ? jade : muted);
                    Badge(row, Attention.LearnableSkills.Contains(skill));
                    if (GUI.Button(row, GUIContent.none, invisibleButton) && selectedSkill != skill)
                    {
                        selectedSkill = skill;
                        mobileSkillDetailScroll = Vector2.zero;
                        mobileSkillStatus = null;
                        CancelMobileScroll();
                        BlockUITransition();
                    }
                }
                y += h + 8;
            }
            return y;
        }

        private float DrawMobileSkillDescription(float width, bool draw)
        {
            var p = session.Progression.Profile;
            int skill = selectedSkill, rank = p.skillRanks[skill];
            float y = 8;
            if (!string.IsNullOrEmpty(mobileSkillStatus))
                MobileSkillParagraph(ref y, width, mobileSkillStatus, 14, mobileSkillStatusFailed ? gold : jade, true, draw);
            MobileSkillParagraph(ref y, width, (GameBalance.IsPassive(skill) ? "被动" : "主动") + " · " +
                GameBalance.CategoryName(GameBalance.GetSkillCategory(p.heroClass, skill)), 16, jade, true, draw);
            MobileSkillParagraph(ref y, width, SkillTooltip(p, skill, rank), 14, pale, false, draw);
            MobileSkillParagraph(ref y, width, "学习前置：" + GameBalance.PrerequisiteDescription(p.heroClass, skill), 14,
                session.Progression.PrerequisitesMet(skill) ? muted : gold, false, draw);
            string reason = session.Progression.SkillLockReason(skill);
            MobileSkillParagraph(ref y, width, string.IsNullOrEmpty(reason) ? "可学习下一阶：消耗1技能点" : reason, 14, gold, true, draw);
            for (int stage = 1; stage <= 3; stage++)
            {
                MobileSkillParagraph(ref y, width, GameBalance.SkillRankName(stage) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, stage) +
                    (stage == rank ? " · 当前" : stage < rank ? " · 已学习" : ""), 16, stage <= rank ? jade : pale, true, draw);
                string evolution = skill == 2 && p.heroClass != HeroClass.Summoner ? SkillBudgetHint(p.heroClass, skill, stage) : GameBalance.SkillEvolution(p.heroClass, skill, stage);
                MobileSkillParagraph(ref y, width, evolution, 14, muted, false, draw);
            }
            MobileSkillParagraph(ref y, width, GameBalance.IsPassive(skill) ? "被动学习后自动生效，无须施放。" :
                "学会后直接显示在战斗界面；轻点自动瞄准施放，无须配置或翻页。", 14, jade, false, draw);
            return y;
        }

        private void MobileSkillParagraph(ref float y, float width, string text, int size, Color color, bool bold, bool draw)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }
    }
}
