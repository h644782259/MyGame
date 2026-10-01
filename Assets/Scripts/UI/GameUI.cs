using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Resolution-independent runtime interface; no scene or package dependencies.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        private enum Panel { None, Inventory, Skills, Bindings, SaveLocation }
        private GameSession session;
        private Panel panel;
        private HeroClass selectedClass;
        private string selectedItem;
        private bool confirmNewGame;
        private Vector2 inventoryScroll;
        private Vector2 skillScroll;
        private int selectedSkill;
        private int rebindingSlot = -1;
        private Panel bindingReturnPanel;
        private bool bindingReturnPause;
        private bool saveReturnPause;
        private Font font;
        private float scale = 1f;
        private float width = 1280f;
        private float height = 720f;
        private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        private readonly List<Rect> blockedRects = new List<Rect>();
        private GUIStyle invisibleButton;
        private GUIStyle scrollBar;
        private GUIStyle scrollThumb;
        private Texture2D thumbTexture;
        private Texture2D trackTexture;
        private string tooltip;
        private readonly Color ink = new Color(.035f, .065f, .10f, .97f);
        private readonly Color card = new Color(.06f, .105f, .15f, .96f);
        private readonly Color jade = new Color(.32f, .91f, .77f);
        private readonly Color gold = new Color(1f, .76f, .37f);
        private readonly Color muted = new Color(.57f, .67f, .75f);
        private readonly Color pale = new Color(.88f, .94f, .98f);

        public bool IsPointerOverUI
        {
            get
            {
                if (session == null) return false;
                if (!session.HasStarted || session.Paused || session.IsDead || panel != Panel.None) return true;
                Vector2 mouse = Mouse;
                for (int i = 0; i < blockedRects.Count; i++)
                    if (blockedRects[i].Contains(mouse)) return true;
                return false;
            }
        }

        private Vector2 Mouse { get { return new Vector2(Input.mousePosition.x / scale, (Screen.height - Input.mousePosition.y) / scale); } }

        public void Initialize(GameSession gameSession)
        {
            session = gameSession;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "Arial" }, 18);
        }

        private void Update()
        {
            scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            scale = Mathf.Max(.3f, scale);
            width = Screen.width / scale;
            height = Screen.height / scale;
            if (session == null) return;
            if (!session.HasStarted)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) confirmNewGame = false;
                return;
            }
            if (session.IsDead)
            {
                if (panel != Panel.None)
                {
                    panel = Panel.None;
                    rebindingSlot = -1;
                    session.SetUIBlocking(false);
                }
                return;
            }
            if (rebindingSlot >= 0) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (panel != Panel.None) ClosePanel();
                else session.SetPaused(!session.Paused);
            }
            if (session.Paused) return;
            if (Input.GetKeyDown(KeyCode.I)) TogglePanel(Panel.Inventory);
            if (Input.GetKeyDown(KeyCode.K)) TogglePanel(Panel.Skills);
            if (panel != Panel.Bindings)
            {
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightBracket)) ChangePage(1);
                if (Input.GetKeyDown(KeyCode.LeftBracket)) ChangePage(-1);
            }
        }

        private void OnDestroy()
        {
            if (thumbTexture != null) Destroy(thumbTexture);
            if (trackTexture != null) Destroy(trackTexture);
            if (font != null) Destroy(font);
        }

        private void OnGUI()
        {
            if (session == null || session.Progression == null) return;
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 18);
            if (invisibleButton == null) BuildStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            blockedRects.Clear();
            tooltip = null;
            HandleBindingInput();

            if (!session.HasStarted) DrawTitle();
            else
            {
                bool priorEnabled = GUI.enabled;
                GUI.enabled = priorEnabled && panel == Panel.None && !session.Paused && !session.IsDead;
                DrawHUD();
                GUI.enabled = priorEnabled;
                if (session.IsDead) DrawDeath();
                else if (session.Paused) DrawPause();
                else if (panel == Panel.Inventory) DrawInventory();
                else if (panel == Panel.Skills) DrawSkills();
                else if (panel == Panel.Bindings) DrawBindings();
                else if (panel == Panel.SaveLocation) DrawSaveLocation();
                DrawNotification();
            }
            DrawTooltip();
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.contentColor = oldContentColor;
        }

        private void BuildStyles()
        {
            invisibleButton = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter };
            invisibleButton.normal.textColor = Color.clear;
            invisibleButton.hover.textColor = Color.clear;
            invisibleButton.active.textColor = Color.clear;
            trackTexture = SolidTexture(new Color(.1f, .17f, .22f));
            thumbTexture = SolidTexture(new Color(.27f, .48f, .5f));
            scrollBar = new GUIStyle(GUI.skin.verticalScrollbar) { fixedWidth = 7 };
            scrollBar.normal.background = trackTexture;
            scrollThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollThumb.normal.background = thumbTexture;
            scrollThumb.hover.background = thumbTexture;
            scrollThumb.active.background = thumbTexture;
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private GUIStyle Style(int size, bool bold = false, bool wrap = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            int key = size + (bold ? 100 : 0) + (wrap ? 200 : 0) + (int)align * 1000;
            GUIStyle result;
            if (styles.TryGetValue(key, out result)) return result;
            result = new GUIStyle { font = font, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = wrap, alignment = align, clipping = TextClipping.Clip };
            result.normal.textColor = Color.white;
            styles.Add(key, result);
            return result;
        }

        private void Text(Rect rect, string value, int size, Color color, bool bold = false, bool wrap = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, value ?? "", Style(size, bold, wrap, align));
            GUI.contentColor = previous;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Border(Rect rect, Color color, float thickness = 1f)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Box(Rect rect, Color accent, bool shadow = true)
        {
            if (shadow) Fill(new Rect(rect.x + 5, rect.y + 6, rect.width, rect.height), new Color(0, 0, 0, .20f));
            Fill(rect, ink);
            Border(rect, new Color(accent.r, accent.g, accent.b, .28f));
        }

        private bool Button(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        {
            enabled = enabled && GUI.enabled;
            bool hover = rect.Contains(Mouse);
            Color background = primary ? new Color(accent.r * .32f, accent.g * .36f, accent.b * .39f, 1) : card;
            if (hover && enabled) background = new Color(accent.r * .28f, accent.g * .32f, accent.b * .35f, 1);
            if (!enabled) background = new Color(.065f, .085f, .105f, 1);
            Fill(rect, background);
            Border(rect, new Color(accent.r, accent.g, accent.b, enabled ? (hover ? .85f : .45f) : .12f));
            Text(new Rect(rect.x + 4, rect.y, rect.width - 8, rect.height), caption, 15, enabled ? pale : muted * .7f, true, false, TextAnchor.MiddleCenter);
            if (hover && GUI.enabled && !string.IsNullOrEmpty(hint)) tooltip = hint;
            bool prior = GUI.enabled;
            GUI.enabled = enabled && prior;
            bool clicked = GUI.Button(rect, GUIContent.none, invisibleButton);
            GUI.enabled = prior;
            return clicked;
        }

        private void Rule(float x, float y, float length, Color color)
        {
            Fill(new Rect(x, y, length, 1), new Color(color.r, color.g, color.b, .25f));
        }

        private void DrawTitle()
        {
            bool priorEnabled = GUI.enabled;
            if (confirmNewGame) GUI.enabled = false;
            Fill(new Rect(0, 0, width, height), new Color(.02f, .045f, .075f, .44f));
            float x = (width - 1040) * .5f;
            float y = (height - 590) * .5f;
            Box(new Rect(x, y, 1040, 590), jade);
            Fill(new Rect(x, y, 4, 590), jade);
            Text(new Rect(x + 32, y + 23, 660, 22), "E M B E R F A L L   /   3D 动作角色扮演", 12, jade, true);
            Text(new Rect(x + 30, y + 48, 660, 60), "星烬纪元", 42, pale, true);
            Text(new Rect(x + 33, y + 113, 740, 24), "选择你的职业，踏入荒野。战斗、探秘，与装备一同成长。", 16, muted);
            Text(new Rect(x + 789, y + 43, 217, 30), "单人冒险  /  本地存档", 14, gold, false, false, TextAnchor.MiddleRight);
            Rule(x + 32, y + 155, 976, jade);
            string[] mottos = { "以利刃守望黎明", "令群星回应召唤", "让疾风追随箭矢" };
            string[] roles = { "近战 / 范围斩击 / 耐久", "远程 / 控制 / 法术爆发", "远程 / 灵活 / 群体射击" };
            for (int i = 0; i < 3; i++)
            {
                HeroClass hero = (HeroClass)i;
                Color accent = GameBalance.ClassColor(hero);
                Rect choice = new Rect(x + 32 + i * 328, y + 179, 312, 258);
                bool selected = selectedClass == hero;
                Fill(choice, selected ? new Color(accent.r * .13f, accent.g * .17f, accent.b * .2f, 1) : card);
                Border(choice, new Color(accent.r, accent.g, accent.b, selected ? .9f : .22f), selected ? 2 : 1);
                Fill(new Rect(choice.x, choice.y, choice.width, 3), selected ? accent : muted * .4f);
                Text(new Rect(choice.x + 21, choice.y + 18, 140, 22), "0" + (i + 1) + "   /   " + (selected ? "已选择" : "选择职业"), 12, selected ? accent : muted, true);
                DrawCrest(new Rect(choice.x + 22, choice.y + 55, 70, 75), hero, accent);
                Text(new Rect(choice.x + 116, choice.y + 59, 170, 40), GameBalance.ClassName(hero), 29, pale, true);
                Text(new Rect(choice.x + 116, choice.y + 105, 174, 26), mottos[i], 13, accent);
                Rule(choice.x + 21, choice.y + 150, 270, accent);
                Text(new Rect(choice.x + 21, choice.y + 166, 274, 25), roles[i], 15, pale);
                Text(new Rect(choice.x + 21, choice.y + 201, 274, 42), GameBalance.ClassDescriptions[i], 13, muted, false, true);
                if (GUI.Button(choice, GUIContent.none, invisibleButton)) selectedClass = hero;
            }
            bool canContinue = session.Progression.HasSave;
            Text(new Rect(x + 32, y + 460, 535, 26), "每职业 8 主动 + 2 被动  ·  初习 / 强化 / 觉醒", 16, gold, true);
            Text(new Rect(x + 32, y + 494, 535, 47), "WASD 移动  ·  鼠标瞄准  ·  左键 / J 攻击\nZXCVB / 12345 技能  ·  Tab 换页  ·  I 装备  ·  K 技能", 13, muted, false, true);
            if (Button(new Rect(x + 596, y + 463, 186, 50), "继续冒险", jade, canContinue, canContinue ? "读取本机最近的冒险存档。" : "开始一次冒险后可读取本地存档。"))
            {
                panel = Panel.None;
                session.ContinueGame();
            }
            if (Button(new Rect(x + 802, y + 463, 206, 50), "启程 · " + GameBalance.ClassName(selectedClass), gold, true, "开始新冒险并保存；会替换当前本地存档。", true))
            {
                if (canContinue) confirmNewGame = true;
                else StartSelectedHero();
            }
            Text(new Rect(x + 596, y + 525, 412, 24), "营地补给  →  荒野历练  →  三波副本挑战", 12, muted, false, false, TextAnchor.MiddleRight);
            Text(new Rect(x + 32, y + 560, 976, 18), "原创奇幻冒险原型  /  游戏进度自动保存在本机", 11, muted, false, false, TextAnchor.MiddleCenter);
            string titleMessage = !string.IsNullOrEmpty(session.Notification) ? session.Notification : session.Progression.LastError;
            if (!string.IsNullOrEmpty(titleMessage))
            {
                float toastHeight = Mathf.Clamp(Style(13, false, true).CalcHeight(new GUIContent(titleMessage), 620) + 14, 36, 82);
                Rect toast = new Rect((width - 648) * .5f, height - toastHeight - 12, 648, toastHeight);
                Box(toast, gold, false);
                Text(new Rect(toast.x + 14, toast.y + 7, toast.width - 28, toast.height - 14), titleMessage, 13, pale, false, true, TextAnchor.MiddleCenter);
            }
            GUI.enabled = priorEnabled;
            if (confirmNewGame)
            {
                Rect confirm = Modal(490, 303, "开始新的冒险", "本机已有一份角色存档。");
                Text(new Rect(confirm.x + 29, confirm.y + 118, 432, 52), "创建" + GameBalance.ClassName(selectedClass) + "会替换当前角色进度。\n也可以返回标题，继续原有的冒险。", 16, pale, false, true);
                if (Button(new Rect(confirm.x + 28, confirm.y + 214, 192, 46), "返回", jade)) confirmNewGame = false;
                if (Button(new Rect(confirm.x + 237, confirm.y + 214, 225, 46), "确认创建新角色", gold, true, null, true)) StartSelectedHero();
            }
        }

        private void StartSelectedHero()
        {
            confirmNewGame = false;
            panel = Panel.None;
            selectedItem = null;
            inventoryScroll = Vector2.zero;
            selectedSkill = 0;
            skillScroll = Vector2.zero;
            rebindingSlot = -1;
            session.StartNew(selectedClass);
        }

        private static void Line(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Matrix4x4 previous = GUI.matrix;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            Fill(new Rect(a.x, a.y - thickness * .5f, Vector2.Distance(a, b), thickness), color);
            GUI.matrix = previous;
        }

        private void DrawCrest(Rect r, HeroClass hero, Color color)
        {
            Vector2 c = r.center;
            Color dim = new Color(color.r, color.g, color.b, .32f);
            Line(new Vector2(c.x, r.y), new Vector2(r.xMax, c.y), dim, 1);
            Line(new Vector2(r.xMax, c.y), new Vector2(c.x, r.yMax), dim, 1);
            Line(new Vector2(c.x, r.yMax), new Vector2(r.x, c.y), dim, 1);
            Line(new Vector2(r.x, c.y), new Vector2(c.x, r.y), dim, 1);
            if (hero == HeroClass.Vanguard)
            {
                Line(new Vector2(c.x, r.y + 11), new Vector2(c.x, r.yMax - 14), color, 5);
                Line(new Vector2(c.x - 15, c.y + 9), new Vector2(c.x + 15, c.y + 9), color, 4);
                Line(new Vector2(c.x - 6, r.y + 23), new Vector2(c.x, r.y + 11), color, 3);
                Line(new Vector2(c.x + 6, r.y + 23), new Vector2(c.x, r.y + 11), color, 3);
            }
            else if (hero == HeroClass.Arcanist)
            {
                Line(new Vector2(c.x, r.y + 11), new Vector2(c.x - 18, c.y + 15), color, 3);
                Line(new Vector2(c.x - 18, c.y + 15), new Vector2(c.x + 18, c.y + 15), color, 3);
                Line(new Vector2(c.x + 18, c.y + 15), new Vector2(c.x, r.y + 11), color, 3);
                Fill(new Rect(c.x - 4, c.y - 2, 8, 8), Color.white);
            }
            else
            {
                Line(new Vector2(c.x + 10, r.y + 12), new Vector2(c.x - 14, c.y), color, 3);
                Line(new Vector2(c.x - 14, c.y), new Vector2(c.x + 10, r.yMax - 12), color, 3);
                Line(new Vector2(c.x + 10, r.y + 12), new Vector2(c.x + 10, r.yMax - 12), dim, 1);
                Line(new Vector2(c.x - 15, c.y), new Vector2(c.x + 22, c.y), color, 3);
                Line(new Vector2(c.x + 16, c.y - 6), new Vector2(c.x + 23, c.y), color, 3);
                Line(new Vector2(c.x + 16, c.y + 6), new Vector2(c.x + 23, c.y), color, 3);
            }
        }

        private void DrawHUD()
        {
            GameProfile p = session.Progression.Profile;
            Color accent = GameBalance.ClassColor(p.heroClass);
            Rect playerRect = new Rect(24, 24, 312, 152);
            blockedRects.Add(playerRect);
            Box(playerRect, accent);
            Fill(new Rect(24, 24, 3, 152), accent);
            Text(new Rect(40, 35, 191, 26), GameBalance.ClassName(p.heroClass) + "  /  Lv." + p.level.ToString("00"), 21, pale, true);
            Text(new Rect(230, 40, 88, 24), Money(p.gold) + " 金", 15, gold, true, false, TextAnchor.UpperRight);
            float hp = session.Player == null ? 0 : session.Player.Health;
            float maxHp = session.Player == null ? 1 : session.Player.MaxHealth;
            Bar(new Rect(40, 77, 280, 15), hp / Mathf.Max(1, maxHp), new Color(.26f, .77f, .61f));
            Text(new Rect(40, 76, 280, 17), Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp), 11, Color.white, true, false, TextAnchor.MiddleCenter);
            float energy = session.Player == null ? 0 : session.Player.Energy;
            float maxEnergy = session.Player == null ? 100 : session.Player.MaxEnergy;
            Bar(new Rect(40, 104, 280, 13), energy / Mathf.Max(1, maxEnergy), new Color(.28f, .57f, .91f));
            Text(new Rect(40, 101, 280, 18), GameBalance.EnergyName(p.heroClass) + "  " + Mathf.FloorToInt(energy) + " / " + Mathf.RoundToInt(maxEnergy), 10, Color.white, true, false, TextAnchor.MiddleCenter);
            bool maxLevel = p.level >= ProgressionService.MaximumLevel;
            Bar(new Rect(40, 134, 280, 5), maxLevel ? 1 : p.xp / (float)GameBalance.XpToNext(p.level), gold);
            Text(new Rect(40, 147, 280, 19), (maxLevel ? "已达最高等级" : "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)) + "    ·    药剂 " + p.potions, 11, muted);

            Rect objective = new Rect(24, 189, 312, 86);
            blockedRects.Add(objective);
            Box(objective, jade, false);
            Text(new Rect(40, 201, 282, 19), session.InDungeon ? "副本目标" : "冒险指引", 12, jade, true);
            Text(new Rect(40, 226, 278, 42), session.Objective, 15, pale, false, true);
            if (p.skillPoints > 0)
            {
                Rect learn = new Rect(24, 286, 312, 37);
                blockedRects.Add(learn);
                if (Button(learn, "K  学习技能 · 可用技能点 " + p.skillPoints, gold, true, "每升一级获得 1 点技能点。打开技能页，学习或强化技能。")) TogglePanel(Panel.Skills);
            }
            DrawMinimap();
            DrawHotbar();
            DrawDungeonStatus();
            Rect equipment = new Rect(width - 176, height - 112, 152, 37);
            Rect skills = new Rect(width - 176, height - 66, 152, 37);
            blockedRects.Add(equipment);
            blockedRects.Add(skills);
            if (Button(equipment, "I  装备与补给", jade)) TogglePanel(Panel.Inventory);
            if (Button(skills, "K  技能" + (p.skillPoints > 0 ? "  +" + p.skillPoints : ""), gold)) TogglePanel(Panel.Skills);
            Text(new Rect(26, height - 78, 275, 45), "WASD 移动  ·  鼠标瞄准\n左键 / J 攻击  ·  ESC 暂停", 12, new Color(.72f, .80f, .84f), false, true);
        }

        private void Bar(Rect rect, float fraction, Color color)
        {
            Fill(rect, new Color(.11f, .15f, .18f));
            float filled = rect.width * Mathf.Clamp01(fraction);
            Fill(new Rect(rect.x, rect.y, filled, rect.height), color);
            Fill(new Rect(rect.x, rect.y, filled, Mathf.Min(2, rect.height)), new Color(1, 1, 1, .2f));
        }

        private void DrawMinimap()
        {
            float x = width - 208;
            Rect map = new Rect(x, 24, 184, 209);
            blockedRects.Add(map);
            Box(map, jade);
            Text(new Rect(x + 12, 34, 160, 23), session.ZoneName, 15, pale, true, false, TextAnchor.MiddleCenter);
            Rect field = new Rect(x + 18, 68, 148, 132);
            Fill(field, new Color(.055f, .11f, .14f));
            for (int i = 1; i < 4; i++)
            {
                Fill(new Rect(field.x + field.width * i / 4, field.y, 1, field.height), new Color(.15f, .23f, .25f, .4f));
                Fill(new Rect(field.x, field.y + field.height * i / 4, field.width, 1), new Color(.15f, .23f, .25f, .4f));
            }
            if (!session.InDungeon)
            {
                MapDot(field, new Vector3(0, 0, -10), gold, 7);
                MapDot(field, new Vector3(0, 0, 11), new Color(.78f, .5f, 1f), 7);
            }
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy != null && !enemy.IsDead) MapDot(field, enemy.transform.position, enemy.IsBoss ? gold : new Color(1, .38f, .32f), enemy.IsBoss ? 7 : 4);
            }
            if (session.Player != null) MapDot(field, session.Player.transform.position, jade, 7);
            Text(new Rect(x + 9, 209, 166, 16), "青：你   红：敌人   紫：入口", 10, muted, false, false, TextAnchor.MiddleCenter);
            Rect travel = new Rect(x, 245, 184, 38);
            blockedRects.Add(travel);
            string caption = session.InDungeon ? (session.DungeonCleared ? "T  返回营地" : "H  撤离副本") : "T  进入副本";
            if (Button(travel, caption, gold, true, session.InDungeon ? "通关后可返回营地；提前撤离需先远离敌人。" : "前往北面的紫色传送门，靠近后按 T 进入副本。"))
            {
                if (session.InDungeon) session.ReturnToCamp();
                else session.EnterDungeon();
            }
            if (session.InDungeon)
                Text(new Rect(x, 294, 184, 44), "难度 " + session.DungeonTier + " 阶  ·  波次 " + Mathf.Min(session.DungeonWave, session.TotalWaves) + " / " + session.TotalWaves, 13, gold, true, true, TextAnchor.UpperCenter);
            else
            {
                Rect home = new Rect(x, 292, 184, 33);
                blockedRects.Add(home);
                if (Button(home, "H  返回营地", jade, true, "附近没有敌人时可返回营地整备。")) session.ReturnToCamp();
            }
        }

        private void MapDot(Rect map, Vector3 position, Color color, float size)
        {
            float radius = Mathf.Max(1f, session.ArenaRadius);
            float x = map.x + map.width * Mathf.InverseLerp(-radius, radius, position.x);
            float y = map.yMax - map.height * Mathf.InverseLerp(-radius, radius, position.z);
            Fill(new Rect(x - size * .5f - 1, y - size * .5f - 1, size + 2, size + 2), ink);
            Fill(new Rect(x - size * .5f, y - size * .5f, size, size), color);
        }

        private void DrawDungeonStatus()
        {
            if (!session.InDungeon) return;
            if (session.DungeonCleared)
            {
                Rect victory = new Rect((width - 494) * .5f, 87, 494, 159);
                blockedRects.Add(victory);
                Box(victory, gold);
                Fill(new Rect(victory.x, victory.y, victory.width, 3), gold);
                Text(new Rect(victory.x + 18, victory.y + 14, 458, 37), "遗迹肃清", 29, gold, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(victory.x + 18, victory.y + 63, 458, 22), "三波挑战完成 · 通关奖励与战利品已收入行囊", 13, pale, false, false, TextAnchor.MiddleCenter);
                if (Button(new Rect(victory.x + 99, victory.y + 105, 296, 36), "返回营地整备  /  T", gold, true, "下一次遗迹挑战将提升难度。", true)) session.ReturnToCamp();
                return;
            }
            EnemyController boss = null;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy != null && enemy.IsBoss && !enemy.IsDead) { boss = enemy; break; }
            }
            if (boss == null) return;
            Rect bossBar = new Rect((width - 462) * .5f, 86, 462, 67);
            blockedRects.Add(bossBar);
            Box(bossBar, new Color(1f, .4f, .32f));
            Text(new Rect(bossBar.x + 14, bossBar.y + 9, 432, 25), boss.DisplayName + "  /  遗迹首领", 16, gold, true, false, TextAnchor.MiddleCenter);
            Bar(new Rect(bossBar.x + 17, bossBar.y + 44, 428, 8), boss.Health / Mathf.Max(1, boss.MaxHealth), new Color(.89f, .33f, .28f));
        }

        private void DrawHotbar()
        {
            GameProfile p = session.Progression.Profile;
            float x = (width - 558) * .5f;
            float y = height - 194;
            Rect bar = new Rect(x, y, 558, 179);
            blockedRects.Add(bar);
            Box(bar, jade);
            Text(new Rect(x + 13, y + 9, 118, 20), "技能栏  " + (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages, 12, jade, true);
            if (Button(new Rect(x + 130, y + 6, 31, 25), "‹", jade, true, "上一页技能栏  /  [")) ChangePage(-1);
            if (Button(new Rect(x + 168, y + 6, 31, 25), "›", jade, true, "下一页技能栏  /  Tab 或 ]")) ChangePage(1);
            Text(new Rect(x + 212, y + 10, 330, 17), "Tab / [ ] 换页  ·  K 学习、配置与改键", 11, muted, false, false, TextAnchor.MiddleRight);
            for (int slotIndex = 0; slotIndex < GameBalance.HotbarSize; slotIndex++)
            {
                int skill = SkillAtSlot(p, slotIndex);
                bool empty = skill < 0;
                int rank = empty ? 0 : p.skillRanks[skill];
                bool locked = empty || rank == 0;
                float cost = empty ? 0 : GameBalance.SkillEnergyCosts[skill];
                bool lacksEnergy = !locked && session.Player != null && session.Player.Energy < cost;
                float cooldown = empty || session.Player == null ? 0 : session.Player.CooldownRemaining(slotIndex);
                Color accent = lacksEnergy ? new Color(.5f, .61f, .91f) : jade;
                Rect slot = new Rect(x + 13 + (slotIndex % 5) * 108, y + 40 + (slotIndex / 5) * 67, 100, 61);
                Fill(slot, locked ? new Color(.06f, .085f, .11f) : lacksEnergy ? new Color(.07f, .1f, .19f) : card);
                Border(slot, new Color(accent.r, accent.g, accent.b, locked ? .12f : .4f));
                if (cooldown > .01f)
                {
                    float overlayHeight = slot.height * Mathf.Clamp01(cooldown / GameBalance.EffectiveCooldown(skill, rank));
                    Fill(new Rect(slot.x, slot.yMax - overlayHeight, slot.width, overlayHeight), new Color(.24f, .42f, .47f, .48f));
                }
                string key = GameBalance.KeyName(p.hotbarKeys[slotIndex]);
                Text(new Rect(slot.x + 6, slot.y + 3, 44, 17), key, 11, locked ? muted : jade, true);
                if (!empty) Text(new Rect(slot.x + 47, slot.y + 4, 47, 16), cost.ToString("0") + " 点", 10, lacksEnergy ? new Color(.65f, .76f, 1f) : muted, false, false, TextAnchor.UpperRight);
                string name = empty ? "未配置" : GameBalance.SkillName(p.heroClass, skill);
                Text(new Rect(slot.x + 3, slot.y + 22, 94, 20), name, 12, locked ? muted : pale, true, false, TextAnchor.MiddleCenter);
                string status = empty ? "按 K 配置" : rank == 0 ? (p.level < GameBalance.SkillRequiredLevels[skill] ? GameBalance.SkillRequiredLevels[skill] + " 级解锁" : "点击学习") : cooldown > .01f ? cooldown.ToString("0.0") + " s" : lacksEnergy ? GameBalance.EnergyName(p.heroClass) + "不足" : "等级 " + rank + " / 3";
                Text(new Rect(slot.x + 3, slot.y + 44, 94, 15), status, 10, locked ? muted : lacksEnergy ? new Color(.65f, .76f, 1f) : gold, false, false, TextAnchor.MiddleCenter);
                if (slot.Contains(Mouse) && GUI.enabled)
                {
                    tooltip = empty ? "空技能槽。按 K 学习技能并配置到这一页；可自定义快捷键。" : GameBalance.SkillDescription(p.heroClass, skill) + "\n冷却 " + GameBalance.EffectiveCooldown(skill, rank).ToString("0.#") + " 秒 · 消耗 " + cost.ToString("0") + " " + GameBalance.EnergyName(p.heroClass) + (rank == 0 ? "\n点击或按 K 学习技能。" : "\n按 " + key + " 施放 · 普攻命中回复 8 点资源。");
                }
                if (locked && GUI.Button(slot, GUIContent.none, invisibleButton))
                {
                    if (!empty) SelectSkill(skill);
                    TogglePanel(Panel.Skills);
                }
            }
            DrawUtilityButtons();
        }

        private void DrawUtilityButtons()
        {
            GameProfile p = session.Progression.Profile;
            string[] keys = { "J / 左键", "SPACE", "F" };
            string[] names = { "普通攻击", "闪避", "生命药剂" };
            for (int i = 0; i < 3; i++)
            {
                Rect r = new Rect(24 + i * 102, height - 166, 94, 65);
                blockedRects.Add(r);
                Box(r, jade, false);
                Text(new Rect(r.x + 8, r.y + 6, 78, 17), keys[i], 10, jade, true);
                Text(new Rect(r.x + 4, r.y + 27, 86, 19), names[i], 13, pale, true, false, TextAnchor.MiddleCenter);
                float dodgeCd = session.Player == null ? 0 : session.Player.DodgeCooldown;
                string status = i == 0 ? "命中 +8 资源" : i == 1 ? dodgeCd > .01f ? dodgeCd.ToString("0.0") + " s" : "就绪" : "× " + p.potions;
                Text(new Rect(r.x + 4, r.y + 48, 86, 14), status, 10, muted, false, false, TextAnchor.MiddleCenter);
                if (r.Contains(Mouse) && GUI.enabled) tooltip = i == 0 ? "按住左键 / J 普通攻击；命中敌人回复 8 点资源。资源还会每秒自然回复 4 点。" : i == 1 ? "空格向移动方向闪避，躲开敌人的地面预警。" : "按 F 或点击此处消耗一瓶药剂，回复 50% 最大生命。";
                if (i == 2 && GUI.Button(r, GUIContent.none, invisibleButton)) session.DrinkPotion();
            }
        }

        private static int SkillAtSlot(GameProfile profile, int slot)
        {
            int index = profile.hotbarPage * GameBalance.HotbarSize + slot;
            if (profile.equippedSkills == null || index < 0 || index >= profile.equippedSkills.Length) return -1;
            int skill = profile.equippedSkills[index];
            return skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill) ? skill : -1;
        }

        private static int AssignedSlot(GameProfile profile, int skill)
        {
            for (int i = 0; i < GameBalance.HotbarSize; i++) if (SkillAtSlot(profile, i) == skill) return i;
            return -1;
        }

        private void ChangePage(int direction)
        {
            int page = (session.Progression.Profile.hotbarPage + direction + GameBalance.HotbarPages) % GameBalance.HotbarPages;
            session.SetHotbarPage(page);
        }

        private void SelectSkill(int skill)
        {
            selectedSkill = Mathf.Clamp(skill, 0, GameBalance.SkillCount - 1);
            skillScroll.y = Mathf.Clamp(selectedSkill * 72 - 144, 0, GameBalance.SkillCount * 72 + 6 - 400);
        }

        private Rect Modal(float modalWidth, float modalHeight, string title, string subtitle)
        {
            Fill(new Rect(0, 0, width, height), new Color(.012f, .025f, .04f, .72f));
            Rect window = new Rect((width - modalWidth) * .5f, (height - modalHeight) * .5f, modalWidth, modalHeight);
            Box(window, jade);
            Fill(new Rect(window.x, window.y, 4, window.height), jade);
            Text(new Rect(window.x + 24, window.y + 19, modalWidth - 105, 35), title, 27, pale, true);
            Text(new Rect(window.x + 24, window.y + 60, modalWidth - 100, 23), subtitle, 13, muted);
            Rule(window.x + 24, window.y + 94, modalWidth - 48, jade);
            return window;
        }

        private void DrawInventory()
        {
            ProgressionService progression = session.Progression;
            GameProfile p = progression.Profile;
            Rect w = Modal(1100, 594, "行囊与装备", "击败怪物收集装备 · 强化提升属性 · 整备时战斗暂停");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 772, w.y + 29, 235, 30), Money(p.gold) + " 金币    /    药剂 " + p.potions, 16, gold, true, false, TextAnchor.MiddleRight);
            float left = w.x + 22;
            Text(new Rect(left, w.y + 110, 240, 24), "当前装备", 15, jade, true);
            for (int i = 0; i < 3; i++)
            {
                ItemData item = progression.Equipped((ItemSlot)i);
                Rect row = new Rect(left, w.y + 145 + i * 78, 238, 67);
                Fill(row, card);
                Color color = item == null ? muted : GameBalance.RarityColor(item.rarity);
                Fill(new Rect(row.x, row.y, 3, row.height), color);
                Text(new Rect(row.x + 12, row.y + 8, 210, 18), GameBalance.SlotName((ItemSlot)i), 11, muted);
                Text(new Rect(row.x + 12, row.y + 31, 210, 25), item == null ? "暂无装备" : ItemTitle(item), 14, color, true);
                if (item != null && GUI.Button(row, GUIContent.none, invisibleButton)) selectedItem = item.id;
            }
            StatBlock stats = progression.GetStats();
            Text(new Rect(left, w.y + 396, 238, 24), "角色属性  /  Lv." + p.level, 15, jade, true);
            StatLine(left, w.y + 433, "攻击", Mathf.RoundToInt(stats.Damage).ToString(), gold);
            StatLine(left, w.y + 462, "防御", Mathf.RoundToInt(stats.Armor).ToString(), pale);
            StatLine(left, w.y + 491, "生命上限", Mathf.RoundToInt(stats.MaxHealth).ToString(), pale);
            StatLine(left, w.y + 520, "暴击几率", Mathf.RoundToInt(stats.CritChance * 100) + "%", pale);

            float middle = w.x + 280;
            Text(new Rect(middle, w.y + 110, 448, 23), "装备仓库  /  " + p.inventory.Count + " / " + ProgressionService.InventoryCapacity + " 件", 15, jade, true);
            Rect viewport = new Rect(middle, w.y + 146, 450, 340);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 434, Mathf.Max(338, p.inventory.Count * 66 + 4));
            GUIStyle priorThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, content, false, true, GUIStyle.none, scrollBar);
            ItemData picked = null;
            for (int rowIndex = 0; rowIndex < p.inventory.Count; rowIndex++)
            {
                ItemData item = p.inventory[p.inventory.Count - 1 - rowIndex];
                if (item == null) continue;
                Rect row = new Rect(3, 3 + rowIndex * 66, 424, 59);
                bool chosen = item.id == selectedItem;
                Fill(row, chosen ? new Color(.1f, .2f, .23f) : card);
                Color color = GameBalance.RarityColor(item.rarity);
                Fill(new Rect(row.x, row.y, 3, row.height), color);
                if (chosen) Border(row, jade * new Color(1, 1, 1, .55f));
                Text(new Rect(row.x + 12, row.y + 7, 301, 25), ItemTitle(item), 15, color, true);
                Text(new Rect(row.x + 12, row.y + 34, 307, 19), GameBalance.SlotName(item.slot) + "  ·  " + GameBalance.RarityName(item.rarity) + "  ·  " + item.level + " 级", 11, muted);
                Text(new Rect(row.x + 327, row.y + 18, 81, 25), IsEquipped(item) ? "已装备" : "查看", 12, IsEquipped(item) ? jade : muted, true, false, TextAnchor.MiddleRight);
                if (GUI.Button(row, GUIContent.none, invisibleButton)) selectedItem = item.id;
            }
            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            for (int i = 0; i < p.inventory.Count; i++)
                if (p.inventory[i] != null && p.inventory[i].id == selectedItem) { picked = p.inventory[i]; break; }
            if (picked == null && p.inventory.Count > 0) { picked = p.inventory[p.inventory.Count - 1]; selectedItem = picked.id; }
            if (p.inventory.Count == 0) Text(new Rect(middle + 20, w.y + 264, 408, 58), "行囊还是空的\n前往荒野战斗，收集第一件装备。", 16, muted, false, true, TextAnchor.MiddleCenter);
            Fill(new Rect(middle, w.y + 501, 450, 64), card);
            Text(new Rect(middle + 12, w.y + 513, 208, 19), "营地补给 · 生命药剂", 14, pale, true);
            Text(new Rect(middle + 12, w.y + 540, 208, 17), "战斗中按 F 快速使用", 11, muted);
            if (Button(new Rect(middle + 250, w.y + 513, 185, 39), "购买药剂 · " + ProgressionService.PotionPrice + " 金", gold, true, "购买一瓶药剂，在战斗中恢复生命。")) Feedback(progression.BuyPotion(), "已购买生命药剂");
            DrawItemDetail(new Rect(w.x + 750, w.y + 112, 328, 453), picked);
            Text(new Rect(left, w.y + 571, 1055, 17), "点击装备查看属性与对比  ·  I / ESC 返回冒险", 11, muted, false, false, TextAnchor.MiddleCenter);
        }

        private void StatLine(float x, float y, string label, string value, Color color)
        {
            Text(new Rect(x + 2, y, 151, 22), label, 13, muted);
            Text(new Rect(x + 157, y, 78, 22), value, 15, color, true, false, TextAnchor.UpperRight);
        }

        private void DrawItemDetail(Rect r, ItemData item)
        {
            Fill(r, card);
            if (item == null) return;
            ProgressionService progression = session.Progression;
            Color rarityColor = GameBalance.RarityColor(item.rarity);
            Fill(new Rect(r.x, r.y, r.width, 3), rarityColor);
            Text(new Rect(r.x + 18, r.y + 18, r.width - 36, 27), GameBalance.RarityName(item.rarity) + " / " + GameBalance.SlotName(item.slot), 13, rarityColor, true);
            Text(new Rect(r.x + 18, r.y + 55, r.width - 36, 63), ItemTitle(item), 24, pale, true, true);
            Text(new Rect(r.x + 18, r.y + 122, r.width - 36, 22), "装备等级 " + item.level + "    ·    强化 +" + item.upgradeLevel, 13, muted);
            Rule(r.x + 18, r.y + 154, r.width - 36, rarityColor);
            ItemData equipped = progression.Equipped(item.slot);
            ItemStat(r.x + 18, r.y + 170, "攻击", item.attack, equipped == null ? 0 : equipped.attack, IsEquipped(item));
            ItemStat(r.x + 18, r.y + 201, "防御", item.defense, equipped == null ? 0 : equipped.defense, IsEquipped(item));
            ItemStat(r.x + 18, r.y + 232, "生命", item.health, equipped == null ? 0 : equipped.health, IsEquipped(item));
            Text(new Rect(r.x + 18, r.y + 271, r.width - 36, 34), IsEquipped(item) ? "正在装备，属性已计入角色。" : "右侧数值为替换当前装备后的变化。", 11, muted, false, true);
            bool canEquip = item.level <= progression.Profile.level && !IsEquipped(item);
            string equipCaption = IsEquipped(item) ? "已装备" : !canEquip ? "需要角色等级 " + item.level : "装备此物品";
            if (Button(new Rect(r.x + 18, r.y + 318, r.width - 36, 36), equipCaption, jade, canEquip, null, true)) Feedback(progression.Equip(item.id), "已装备 " + item.name);
            bool maxUpgrade = item.upgradeLevel >= ProgressionService.MaximumUpgrade;
            if (Button(new Rect(r.x + 18, r.y + 363, r.width - 36, 36), maxUpgrade ? "强化已满 · +" + ProgressionService.MaximumUpgrade : "强化 · " + progression.UpgradeCost(item) + " 金币", gold, !maxUpgrade, "消耗金币，永久提升这件装备的属性。")) Feedback(progression.Upgrade(item.id), "强化成功");
            if (Button(new Rect(r.x + 18, r.y + 408, r.width - 36, 30), "出售 · " + progression.SellValue(item) + " 金币", muted, !IsEquipped(item), IsEquipped(item) ? "先替换当前装备，才能出售。" : "出售选中装备以获得金币。")) Feedback(progression.Sell(item.id), "装备已出售");
        }

        private void ItemStat(float x, float y, string name, int value, int previous, bool equipped)
        {
            Text(new Rect(x, y, 115, 24), name, 14, muted);
            Text(new Rect(x + 126, y, 68, 24), value.ToString(), 17, pale, true, false, TextAnchor.UpperRight);
            int diff = value - previous;
            string delta = equipped || diff == 0 ? "—" : (diff > 0 ? "+" : "") + diff;
            Text(new Rect(x + 206, y, 77, 24), delta, 13, diff >= 0 ? jade : new Color(1f, .49f, .42f), false, false, TextAnchor.UpperRight);
        }

        private bool IsEquipped(ItemData item)
        {
            GameProfile profile = session.Progression.Profile;
            return item != null && (profile.weaponId == item.id || profile.armorId == item.id || profile.relicId == item.id);
        }

        private static string ItemTitle(ItemData item) { return item.name + (item.upgradeLevel > 0 ? " +" + item.upgradeLevel : ""); }

        private static string Money(int amount)
        {
            if (amount >= 100000000) return (amount / 100000000f).ToString("0.#") + " 亿";
            if (amount >= 10000) return (amount / 10000f).ToString("0.#") + " 万";
            return amount.ToString();
        }

        private void DrawSkills()
        {
            GameProfile p = session.Progression.Profile;
            selectedSkill = Mathf.Clamp(selectedSkill, 0, GameBalance.SkillCount - 1);
            Rect w = Modal(1160, 630, GameBalance.ClassName(p.heroClass) + " · 技能研习", "每职业 8 主动 + 2 被动 · 初习 → 强化 → 觉醒 · 2～30 级初习，最高 48 级觉醒");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 763, w.y + 28, 296, 32), "技能点 " + p.skillPoints + "   /   角色 Lv." + p.level, 18, gold, true, false, TextAnchor.MiddleRight);
            Text(new Rect(w.x + 24, w.y + 108, 416, 24), "技能图鉴  /  滚动查看全部 " + GameBalance.SkillCount + " 种", 14, jade, true);
            Text(new Rect(w.x + 458, w.y + 110, 306, 23), "配置技能栏  /  第 " + (p.hotbarPage + 1) + " 页，共 " + GameBalance.HotbarPages + " 页", 14, jade, true);
            if (Button(new Rect(w.x + 792, w.y + 104, 36, 29), "‹", jade, true, "上一页技能栏")) ChangePage(-1);
            if (Button(new Rect(w.x + 838, w.y + 104, 36, 29), "›", jade, true, "下一页技能栏")) ChangePage(1);
            if (Button(new Rect(w.x + 895, w.y + 104, 241, 29), "自定义快捷键", gold)) OpenBindings();

            Rect viewport = new Rect(w.x + 24, w.y + 139, 416, 400);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 401, GameBalance.SkillCount * 72 + 6);
            GUIStyle priorThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            skillScroll = GUI.BeginScrollView(viewport, skillScroll, content, false, true, GUIStyle.none, scrollBar);
            for (int i = 0; i < GameBalance.SkillCount; i++)
            {
                int rank = p.skillRanks[i];
                int required = GameBalance.SkillRequiredLevels[i];
                bool unlocked = p.level >= required;
                bool passive = GameBalance.IsPassive(i);
                Rect row = new Rect(3, 3 + i * 72, 392, 66);
                Color accent = Color.Lerp(GameBalance.ClassColor(p.heroClass), gold, i / 9f);
                Fill(row, selectedSkill == i ? new Color(.1f, .2f, .23f) : card);
                Fill(new Rect(row.x, row.y, 3, row.height), new Color(accent.r, accent.g, accent.b, unlocked ? .95f : .3f));
                if (selectedSkill == i) Border(row, jade * new Color(1, 1, 1, .62f));
                Rect number = new Rect(row.x + 12, row.y + 13, 40, 40);
                Border(number, accent * new Color(1, 1, 1, .27f));
                Text(number, (i + 1).ToString("00"), 17, unlocked ? accent : muted, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(row.x + 65, row.y + 9, 224, 26), GameBalance.SkillName(p.heroClass, i), 18, unlocked ? pale : muted, true);
                string skillType = passive ? "被动" : "主动";
                string category = GameBalance.CategoryName(GameBalance.GetSkillCategory(p.heroClass, i));
                Text(new Rect(row.x + 65, row.y + 42, 249, 18), skillType + " / " + category + " · " + required + " 级解锁", 11, passive ? new Color(.72f, .64f, .92f) : muted);
                Text(new Rect(row.x + 290, row.y + 11, 91, 20), GameBalance.SkillRankName(rank), 12, rank > 0 ? gold : muted, true, false, TextAnchor.MiddleRight);
                int assigned = AssignedSlot(p, i);
                if (assigned >= 0) Text(new Rect(row.x + 316, row.y + 42, 65, 18), GameBalance.KeyName(p.hotbarKeys[assigned]) + (rank > 0 ? " 出战" : " 预置"), 10, jade, false, false, TextAnchor.MiddleRight);
                if (GUI.Button(row, GUIContent.none, invisibleButton)) selectedSkill = i;
            }
            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            DrawSkillDetail(new Rect(w.x + 458, w.y + 139, 678, 400), selectedSkill);
            Rule(w.x + 24, w.y + 555, 1112, jade);
            Text(new Rect(w.x + 28, w.y + 569, 1105, 21), "战斗搭配  /  低阶技能消耗少、冷却短；高阶技能场面更大、威力更强，也需要更多资源和等待时间。", 13, gold);
            Text(new Rect(w.x + 28, w.y + 599, 929, 18), "普攻命中回复 8 点资源 · 每秒自然回复 4 点 · 3 页共用技能冷却 · 升级保留原技能槽位", 11, muted);
            Text(new Rect(w.x + 981, w.y + 596, 150, 21), "K / ESC 返回", 11, muted, false, false, TextAnchor.MiddleRight);
        }

        private void DrawSkillDetail(Rect r, int skill)
        {
            GameProfile p = session.Progression.Profile;
            int rank = p.skillRanks[skill];
            int nextRank = Mathf.Min(3, rank + 1);
            bool passive = GameBalance.IsPassive(skill);
            SkillCategory category = GameBalance.GetSkillCategory(p.heroClass, skill);
            bool showOffenseScale = !passive && category != SkillCategory.Healing && category != SkillCategory.Defense;
            Color accent = Color.Lerp(GameBalance.ClassColor(p.heroClass), gold, skill / 9f);
            Fill(r, card);
            Fill(new Rect(r.x, r.y, r.width, 3), accent);
            Text(new Rect(r.x + 18, r.y + 15, 460, 35), GameBalance.SkillName(p.heroClass, skill), 27, pale, true);
            Text(new Rect(r.x + 430, r.y + 22, 230, 24), (passive ? "被动" : "主动") + " / " + GameBalance.CategoryName(category) + " / " + GameBalance.SkillRankName(rank), 13, accent, true, false, TextAnchor.MiddleRight);
            Text(new Rect(r.x + 18, r.y + 59, 642, 42), GameBalance.SkillDescription(p.heroClass, skill), 14, muted, false, true);
            string[] labels = { "当前冷却", "资源消耗", "初习解锁", "进阶成长" };
            string[] values = { passive ? "自动生效" : GameBalance.EffectiveCooldown(skill, rank).ToString("0.#") + " 秒", passive ? "无需消耗" : GameBalance.SkillEnergyCosts[skill].ToString("0") + " " + GameBalance.EnergyName(p.heroClass), "Lv." + GameBalance.SkillRequiredLevels[skill], "强化 → 觉醒" };
            for (int i = 0; i < 4; i++)
            {
                Rect stat = new Rect(r.x + 18 + i * 163, r.y + 107, 151, 40);
                Fill(stat, new Color(.035f, .075f, .11f));
                Text(new Rect(stat.x + 9, stat.y + 4, 134, 14), labels[i], 10, muted);
                Text(new Rect(stat.x + 9, stat.y + 21, 134, 18), values[i], 13, i == 1 ? jade : pale, true);
            }
            for (int stage = 1; stage <= 3; stage++)
            {
                Rect evolution = new Rect(r.x + 18, r.y + 158 + (stage - 1) * 78, 305, 72);
                bool current = stage == rank;
                Fill(evolution, current ? new Color(.13f, .2f, .2f) : new Color(.045f, .08f, .115f));
                Border(evolution, new Color(accent.r, accent.g, accent.b, current ? .75f : .18f));
                string stageName = GameBalance.SkillRankName(stage) + "  /  Lv." + GameBalance.SkillRankRequiredLevel(skill, stage);
                Text(new Rect(evolution.x + 10, evolution.y + 5, 220, 18), stageName, 13, stage <= rank ? gold : pale, true);
                Text(new Rect(evolution.x + 232, evolution.y + 7, 64, 15), current ? "当前" : stage == rank + 1 ? "下一阶段" : stage < rank ? "已达成" : "预览", 10, current ? gold : muted, false, false, TextAnchor.MiddleRight);
                int damagePercent = 100 + (stage - 1) * 30;
                int rangePercent = Mathf.RoundToInt(GameBalance.SkillRangeMultiplier(stage) * 100);
                string evolutionText = GameBalance.SkillEvolution(p.heroClass, skill, stage);
                if (showOffenseScale)
                {
                    Text(new Rect(evolution.x + 10, evolution.y + 25, 284, 16), "伤害 " + damagePercent + "%  ·  范围 " + rangePercent + "%", 11, jade);
                    Text(new Rect(evolution.x + 10, evolution.y + 45, 284, 24), evolutionText, 10, muted, false, true);
                }
                else Text(new Rect(evolution.x + 10, evolution.y + 28, 284, 39), evolutionText, 11, jade, false, true);
                if (evolution.Contains(Mouse)) tooltip = GameBalance.SkillRankName(stage) + "：角色达到 " + GameBalance.SkillRankRequiredLevel(skill, stage) + " 级，消耗 1 技能点。\n" + evolutionText + (passive ? "\n学习后自动生效，无需配置到快捷栏。" : "\n冷却 " + GameBalance.EffectiveCooldown(skill, stage).ToString("0.#") + " 秒。");
            }
            float actionX = r.x + 343;
            Text(new Rect(actionX, r.y + 158, 315, 24), rank == 3 ? "此技能已完全觉醒" : "下一阶段：" + GameBalance.SkillRankName(nextRank) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, nextRank), 14, gold, true);
            string reason = session.Progression.SkillLockReason(skill);
            bool canLearn = string.IsNullOrEmpty(reason);
            string caption = rank == 3 ? "已完全觉醒" : rank == 0 ? "学习初习 · 1 技能点" : "进阶" + GameBalance.SkillRankName(nextRank) + " · 1 技能点";
            if (Button(new Rect(actionX, r.y + 189, 317, 37), caption, gold, canLearn, reason, canLearn))
                Feedback(session.Progression.LearnSkill(skill), GameBalance.SkillName(p.heroClass, skill) + "已达到" + GameBalance.SkillRankName(p.skillRanks[skill]));
            Text(new Rect(actionX, r.y + 237, 317, 35), rank == 3 ? "同一技能已进阶至最高阶段。" : canLearn ? (passive ? "进阶会强化这项被动的效果，学习后持续生效。" : "提升技能效果与机制；原快捷栏位置保持不变。") : reason, 12, muted, false, true);
            if (passive)
            {
                Rect passiveInfo = new Rect(actionX, r.y + 284, 317, 101);
                Fill(passiveInfo, new Color(.085f, .075f, .15f));
                Border(passiveInfo, new Color(.55f, .42f, .8f, .35f));
                Text(new Rect(passiveInfo.x + 13, passiveInfo.y + 14, 291, 23), rank > 0 ? "被动已生效" : "被动技能", 17, new Color(.8f, .73f, 1f), true);
                Text(new Rect(passiveInfo.x + 13, passiveInfo.y + 48, 291, 42), "学习后自动生效，无需放入快捷栏。\n进阶继续强化效果，不占主动技能槽。", 13, pale, false, true);
                return;
            }
            Text(new Rect(actionX, r.y + 281, 317, 21), "配置到第 " + (p.hotbarPage + 1) + " 页  /  点击已配槽可卸下", 12, jade, true);
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int equipped = SkillAtSlot(p, slot);
                bool current = equipped == skill;
                Rect target = new Rect(actionX + (slot % 5) * 65, r.y + 308 + (slot / 5) * 39, 57, 31);
                string key = GameBalance.KeyName(p.hotbarKeys[slot]);
                string label = key + (current ? " ×" : " +");
                string hint = rank == 0 ? "先学习这项技能。" : current ? "点击从 " + key + " 槽卸下；不会清除技能冷却。" : "将" + GameBalance.SkillName(p.heroClass, skill) + "配置到 " + key + " 槽。当前：" + (equipped < 0 ? "空" : GameBalance.SkillName(p.heroClass, equipped)) + "。";
                if (Button(target, label, current ? gold : jade, rank > 0 && session.CanChangeLoadout, hint, current))
                    session.AssignSkill(slot, current ? -1 : skill);
            }
            float remaining = session.Player == null ? 0 : session.Player.SkillCooldownRemaining(skill);
            Text(new Rect(actionX, r.y + 384, 317, 15), remaining > .01f ? "剩余冷却 " + remaining.ToString("0.0") + " 秒 · 换页不会重置" : "“+” 配置 / 替换 · “×” 卸下 · 换页共用冷却", 10, muted);
        }

        private void OpenBindings()
        {
            bindingReturnPanel = panel;
            bindingReturnPause = session.Paused;
            panel = Panel.Bindings;
            rebindingSlot = -1;
            session.SetUIBlocking(true);
            session.SetPaused(false);
        }

        private void HandleBindingInput()
        {
            if (panel != Panel.Bindings || rebindingSlot < 0 || Event.current.type != EventType.KeyDown) return;
            KeyCode key = Event.current.keyCode;
            bool modified = Event.current.control || Event.current.alt || Event.current.command;
            Event.current.Use();
            if (key == KeyCode.Escape) { rebindingSlot = -1; return; }
            if (modified || !GameBalance.IsBindableKey((int)key))
            {
                session.Notify("此键保留给移动或界面操作。请选择字母、数字或 F1～F12；ESC 取消。");
                return;
            }
            int slot = rebindingSlot;
            bool changed = session.Progression.SetHotbarKey(slot, (int)key);
            Feedback(changed, "技能槽 " + (slot + 1) + " 已绑定 " + GameBalance.KeyName((int)key) + "；三页同步使用");
            if (changed) rebindingSlot = -1;
        }

        private void DrawBindings()
        {
            GameProfile p = session.Progression.Profile;
            Rect w = Modal(840, 590, "自定义快捷键", "三页共用 10 个按键 · 点击槽位，然后按下新的按键");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 27, w.y + 112, 787, 43), "可绑定字母、数字与 F1～F12。若新按键已占用，两个槽位会自动交换。\n技能栏支持 3 页，按 Tab / ] 下一页，按 [ 上一页。", 14, pale, false, true);
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect tile = new Rect(w.x + 24 + (i % 5) * 161, w.y + 181 + (i / 5) * 107, 148, 87);
                bool waiting = rebindingSlot == i;
                Fill(tile, waiting ? new Color(.2f, .18f, .12f) : card);
                Border(tile, waiting ? gold : jade * new Color(1, 1, 1, .3f));
                Text(new Rect(tile.x + 12, tile.y + 8, 125, 17), "技能槽 " + (i + 1).ToString("00"), 11, muted);
                Text(new Rect(tile.x + 8, tile.y + 31, 132, 35), waiting ? "按键…" : GameBalance.KeyName(p.hotbarKeys[i]), waiting ? 22 : 29, waiting ? gold : pale, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(tile.x + 9, tile.y + 68, 130, 14), waiting ? "ESC 取消" : "点击修改", 10, muted, false, false, TextAnchor.MiddleCenter);
                if (GUI.Button(tile, GUIContent.none, invisibleButton)) rebindingSlot = i;
            }
            Rect hint = new Rect(w.x + 24, w.y + 392, 792, 59);
            Fill(hint, card);
            Text(new Rect(hint.x + 15, hint.y + 11, 761, 38), rebindingSlot >= 0 ? "正在等待技能槽 " + (rebindingSlot + 1) + " 的新按键…\n按 ESC 取消本次修改；游戏已暂停。" : "默认布局：Z X C V B  /  1 2 3 4 5\n升级或切换技能栏不会改变你设置的按键。", 14, rebindingSlot >= 0 ? gold : jade, true, true);
            Text(new Rect(w.x + 27, w.y + 465, 786, 39), "保留键：W A S D 移动，J 普攻，F 药剂，I 装备，K 技能，H 回营，T 传送，空格闪避，ESC 返回，Tab / [ ] 换页。", 12, muted, false, true);
            if (Button(new Rect(w.x + 24, w.y + 524, 350, 39), "恢复默认 ZXCVB / 12345", gold, rebindingSlot < 0))
            {
                bool restored = true;
                for (int i = 0; i < GameBalance.HotbarSize; i++)
                    if (!session.Progression.SetHotbarKey(i, GameBalance.DefaultHotbarKeys[i])) { restored = false; break; }
                Feedback(restored, "已恢复默认技能按键");
            }
            if (Button(new Rect(w.x + 397, w.y + 524, 419, 39), bindingReturnPause ? "返回暂停菜单" : "返回技能研习", jade)) ClosePanel();
        }

        private void DrawPause()
        {
            Rect w = Modal(472, 566, "冒险暂停", "歇一口气，再继续前行。");
            Text(new Rect(w.x + 28, w.y + 116, 416, 31), session.ZoneName + "  ·  Lv." + session.Progression.Profile.level + " " + GameBalance.ClassName(session.Progression.Profile.heroClass), 17, jade, true, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(w.x + 40, w.y + 172, 392, 48), "继续冒险   /   ESC", jade, true, null, true)) session.SetPaused(false);
            if (Button(new Rect(w.x + 40, w.y + 234, 392, 44), "保存进度", gold))
            {
                session.Progression.Save();
                session.Notify(string.IsNullOrEmpty(session.Progression.LastError) ? "进度已保存在本机" : session.Progression.LastError);
            }
            if (Button(new Rect(w.x + 40, w.y + 292, 392, 44), "保存并返回标题", muted))
            {
                ClosePanel();
                session.QuitToTitle();
            }
            if (Button(new Rect(w.x + 40, w.y + 350, 188, 37), GameAudio.Muted ? "声音：已静音" : "声音：已开启", jade)) GameAudio.Muted = !GameAudio.Muted;
            if (Button(new Rect(w.x + 244, w.y + 350, 188, 37), "自定义快捷键", gold)) OpenBindings();
            if (Button(new Rect(w.x + 40, w.y + 401, 392, 37), "存档位置 / 迁移", jade))
            {
                saveReturnPause = session.Paused;
                panel = Panel.SaveLocation;
                session.SetUIBlocking(true);
                session.SetPaused(false);
            }
            Text(new Rect(w.x + 38, w.y + 460, 396, 77), "I 装备与补给  ·  K 学习技能  ·  F 生命药剂\nTab / [ ] 技能栏换页  ·  T 传送  ·  H 回营\n当前角色、装备、金币与技能自动保存在本机。", 12, muted, false, true, TextAnchor.MiddleCenter);
        }

        private void DrawSaveLocation()
        {
            Rect w = Modal(800, 500, "存档位置与迁移", "游戏安装目录与角色存档分开保存，重新安装游戏可继续原有冒险。");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            string path = session.Progression.SaveDirectory;
            Fill(new Rect(w.x + 24, w.y + 113, 752, 79), card);
            Text(new Rect(w.x + 40, w.y + 123, 720, 17), "当前存档文件夹", 11, jade, true);
            Text(new Rect(w.x + 40, w.y + 147, 720, 37), path, 13, pale, false, true);
            if (Button(new Rect(w.x + 24, w.y + 207, 367, 40), "打开存档文件夹", jade))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    string absolute = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                    Application.OpenURL(new System.Uri(absolute).AbsoluteUri);
                }
                catch (System.Exception exception) { session.Notify("无法打开存档目录：" + exception.Message); }
            }
            if (Button(new Rect(w.x + 409, w.y + 207, 367, 40), "复制目录路径", gold))
            {
                GUIUtility.systemCopyBuffer = path;
                session.Notify("已复制存档目录路径");
            }
            Text(new Rect(w.x + 28, w.y + 268, 744, 23), "角色文件：" + System.IO.Path.GetFileName(session.Progression.SaveFilePath), 14, jade, true);
            Text(new Rect(w.x + 28, w.y + 307, 744, 96), "同一台电脑可将游戏安装或移动到任意目录，存档仍从上面的固定位置读取。\n\n迁移到新电脑时，先退出两台电脑上的游戏，将 emberfall-save.json 及其 .bak 备份复制到新电脑的存档文件夹。启动游戏后选择「继续冒险」。", 14, pale, false, true);
            Text(new Rect(w.x + 28, w.y + 415, 744, 21), "建议迁移前保留一份备份；新电脑的存档目录也可从此页面打开。", 12, muted);
            if (Button(new Rect(w.x + 24, w.y + 451, 752, 31), "返回暂停菜单", jade)) ClosePanel();
        }

        private void DrawDeath()
        {
            Rect w = Modal(508, 365, "星火未熄", "这次倒下，不是冒险的终点。");
            Text(new Rect(w.x + 34, w.y + 119, 440, 63), "返回营地整备，再次挑战。\n装备与经验保留；本次倒下损失 10% 金币。", 16, pale, false, true, TextAnchor.MiddleCenter);
            if (Button(new Rect(w.x + 48, w.y + 213, 412, 50), "在营地重新出发", gold, true, null, true))
            {
                ClosePanel();
                session.Respawn();
            }
            Text(new Rect(w.x + 36, w.y + 289, 436, 41), "留意地面攻击预警，按空格闪避。\n升级装备、学习技能后，再去挑战更强的敌人。", 12, muted, false, true, TextAnchor.MiddleCenter);
        }

        private void DrawNotification()
        {
            if (string.IsNullOrEmpty(session.Notification)) return;
            bool overlay = panel != Panel.None || session.Paused || session.IsDead;
            Rect r = new Rect((width - 550) * .5f, overlay ? height - 49 : 26, 550, 40);
            Box(r, gold);
            Text(new Rect(r.x + 14, r.y + 3, r.width - 28, 34), session.Notification, 14, pale, true, true, TextAnchor.MiddleCenter);
        }

        private void DrawTooltip()
        {
            if (string.IsNullOrEmpty(tooltip)) return;
            float boxHeight = Style(13, false, true).CalcHeight(new GUIContent(tooltip), 288) + 22;
            Vector2 mouse = Mouse;
            Rect r = new Rect(Mathf.Clamp(mouse.x - 154, 12, width - 324), Mathf.Clamp(mouse.y - boxHeight - 14, 12, height - boxHeight - 12), 312, boxHeight);
            Box(r, jade);
            Text(new Rect(r.x + 12, r.y + 10, 288, boxHeight - 20), tooltip, 13, pale, false, true);
        }

        private void TogglePanel(Panel value)
        {
            if (session == null || !session.HasStarted || session.IsDead || session.Paused) return;
            panel = panel == value ? Panel.None : value;
            session.SetUIBlocking(panel != Panel.None);
        }

        private void ClosePanel()
        {
            rebindingSlot = -1;
            if (panel == Panel.Bindings)
            {
                panel = bindingReturnPanel;
                session.SetUIBlocking(panel != Panel.None);
                session.SetPaused(bindingReturnPause);
                bindingReturnPause = false;
                return;
            }
            if (panel == Panel.SaveLocation)
            {
                panel = Panel.None;
                session.SetUIBlocking(false);
                session.SetPaused(saveReturnPause);
                saveReturnPause = false;
                return;
            }
            panel = Panel.None;
            session.SetUIBlocking(false);
        }

        private void Feedback(bool success, string message)
        {
            string issue = session.Progression.LastError;
            session.Notify(success ? message + (string.IsNullOrEmpty(issue) ? "" : " · " + issue) : (string.IsNullOrEmpty(issue) ? "当前无法执行此操作" : issue));
        }
    }
}
