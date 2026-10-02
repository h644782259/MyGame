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

        private void DrawChests()
        {
            if(MobileControls.Active){DrawMobileChests();return;}
            // Keep this modal independent of the victory panel and combat input.
            // The save-backed service consumes the offer before the reveal begins.
            bool revealed = chestRevealResult != null;
            bool complete = ChestAnimationDone;
            var reward = revealed ? session.Progression.LastChestReward : null;
            Color rewardColor = reward != null && reward.Rarity.HasValue ? GameBalance.RarityColor(reward.Rarity.Value) : gold;
            if (revealed && complete && !rewardSoundPlayed)
            {
                rewardSoundPlayed=true;
                GameAudio.Play(reward == null || !reward.Rarity.HasValue ? SoundCue.UI : reward.Rarity.Value == Rarity.Legendary ? SoundCue.Victory : reward.Rarity.Value == Rarity.Epic ? SoundCue.LevelUp : reward.Rarity.Value == Rarity.Rare ? SoundCue.Loot : SoundCue.Cast);
            }
            Fill(new Rect(0, 0, width, height), new Color(.018f, .025f, .045f, .84f));
            Rect w = new Rect((width - 790) * .5f, (height - 470) * .5f, 790, 470);
            Fill(new Rect(w.x + 8, w.y + 12, w.width, w.height), new Color(0, 0, 0, .3f));
            Fill(w, new Color(.045f, .064f, .095f, .99f));
            Border(w, new Color(.52f, .60f, .67f, .25f));
            Fill(new Rect(w.center.x - 32, w.y, 64, 2), gold);
            Text(new Rect(w.x + 36, w.y + 24, 610, 18), "F A L L E N   S T A R", 10, gold, true);
            Text(new Rect(w.x + 36, w.y + 49, 520, 41), revealed ? (complete ? "星光已归你所有" : "封印正在苏醒") : "遗迹馈赠", 29, pale, true);
            Text(new Rect(w.x + 37, w.y + 96, 620, 25), revealed ? (complete ? "已收入行囊" : "轻触跳过动画") : "选一个开启", 15, muted);
            if (Button(new Rect(w.xMax - 207, w.y + 39, 84, 34), "菜单", jade))
            { session.SetPaused(true); BlockUITransition(); return; }
            if (Button(new Rect(w.xMax - 111, w.y + 39, 73, 34), chestDetails ? "收起" : "ⓘ 详情", muted)) chestDetails = !chestDetails;

            for (int i = 0; i < 3; i++)
            {
                Rect r = new Rect(w.x + 35 + i * 244, w.y + 140, 232, 240);
                bool chosen = revealed && i == revealedChest;
                bool closed = revealed && !chosen;
                bool hover = !revealed && r.Contains(Mouse) && GUI.enabled;
                Color edge = chosen && complete ? rewardColor : hover ? gold : new Color(.32f, .42f, .49f, .65f);
                Fill(new Rect(r.x, r.y + (hover ? -3 : 0), r.width, r.height), chosen ? new Color(.13f, .14f, .16f) : new Color(.07f, .09f, .125f));
                Border(new Rect(r.x, r.y + (hover ? -3 : 0), r.width, r.height), edge);
                Fill(new Rect(r.x + 15, r.y + 16, 18, 1), closed ? muted * .35f : edge);
                Text(new Rect(r.x + 17, r.y + 21, 30, 17), new[] { "I", "II", "III" }[i], 11, closed ? muted * .35f : muted);
                float progress=chosen ? Mathf.Clamp01((Time.unscaledTime-chestRevealedAt)/ChestDuration) : 0;
                if(!chosen||progress<.76f||!DrawChestRewardModel(new Rect(r.x+18,r.y+16,196,167),reward))
                    DrawRewardChest(new Rect(r.x + 18, r.y + 16 + (hover ? -3 : 0), 196, 167), chosen, closed ? .22f : 1f, progress);
                if(chosen && progress>.35f) DrawRewardRadiance(new Rect(r.x+17,r.y+18,198,154), rewardColor, progress);
                if (chosen)
                {
                    float glow = Mathf.Clamp01((Time.unscaledTime - chestRevealedAt) * 3);
                    Text(new Rect(r.x + 20, r.y + 187, 192, 30), complete ? (reward != null && reward.Rarity.HasValue ? GameBalance.RarityName(reward.Rarity.Value) : "金币") : "开启中", 19, complete ? rewardColor : gold, true, false, TextAnchor.MiddleCenter);
                }
                else if (closed) Text(new Rect(r.x + 20, r.y + 187, 192, 30), "已封存", 13, muted * .55f, false, false, TextAnchor.MiddleCenter);
                else if (Button(new Rect(r.x + 22, r.y + 183, 188, 40), "开启", gold, !chestDetails && !chestOpening && session.Progression.Profile.pendingFashionChest, null, hover))
                {
                    chestOpening = true;
                    string result = session.Progression.OpenDungeonChest(i);
                    if (result == null) { chestOpening = false; Feedback(false, "宝箱暂时无法开启"); }
                    else
                    {
                        revealedChest = i;
                        chestRevealResult = result;
                        chestRevealedAt = Time.unscaledTime;
                        chestDetails = false;
                        rewardSoundPlayed=false;
                        chestReceiptId=session.Progression.LastChestReward.Id;
                        GameAudio.Play(SoundCue.Cast);
                    }
                    // Do not let this same GUI event operate another card or the world.
                    return;
                }
            }
            if (revealed)
            {
                Text(new Rect(w.x + 35, w.y + 393, 500, 57), complete && reward != null ? (reward.Rarity.HasValue ? reward.Name + (reward.Duplicate ? " · 重复已转金币" : "") + "\n" : "") + "+" + reward.Gold + " 金币" : "", 17, rewardColor, true, true);
                if (Button(new Rect(w.xMax - 206, w.y + 402, 170, 39), complete ? "收下" : "跳过动画", jade, !chestDetails, null, true))
                {
                    if (!complete) chestRevealedAt=Time.unscaledTime-ChestDuration;
                    else FinishChestReveal();
                }
            }
            else Text(new Rect(w.x + 35, w.y + 407, 720, 25), "仅选一份 · 开启后其余关闭", 12, muted, false, false, TextAnchor.MiddleCenter);

            if (chestDetails)
            {
                Rect details = new Rect(w.x + 30, w.y + 132, w.width - 60, 251);
                Fill(details, new Color(.055f, .078f, .11f, .995f));
                Border(details, new Color(.4f, .56f, .62f));
                Text(new Rect(details.x + 24, details.y + 21, details.width - 48, 24), "奖励规则", 18, gold, true);
                Text(new Rect(details.x + 24, details.y + 61, details.width - 48, 149),
                    "三份宝箱机会完全相同，每次只可开启一份。\n\n保底 "+TierRewardRules.ChestGoldMinimum(session.Progression.Profile.pendingChestTier)+"～"+(TierRewardRules.ChestGoldMinimum(session.Progression.Profile.pendingChestTier)+40)+" 金币，时装总概率 40%。\n普通 22% · 稀有 12% · 史诗 5% · 传说 1% · 无时装 60%\n以上均为每次开箱的绝对概率；重复时装转化金币。\n未开启的宝箱会随角色存档保留。", 14, pale, false, true);
                // Full overlay blocks the cards underneath, including touch events.
                if (GUI.Button(new Rect(details.x + details.width - 108, details.y + 204, 88, 31), "知道了", Style(14, true, false, TextAnchor.MiddleCenter))) chestDetails = false;
            }
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
