using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Resolution-independent runtime interface; no scene or package dependencies.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        private enum Panel { None, Inventory, Skills, Bindings, SaveLocation, Controls }
        private GameSession session;
        private Panel panel;
        private HeroClass selectedClass;
        private string selectedItem;
        private bool confirmNewGame;
        private Vector2 inventoryScroll;
        private int inventoryFilter = -1;
        private int inventorySort;
        private int unequippedCount;
        private Vector2 skillScroll;
        private int selectedSkill;
        private int rebindingSlot = -1;
        private Panel bindingReturnPanel;
        private bool bindingReturnPause;
        private bool saveReturnPause;
        private bool controlsReturnPause;
        private Font font;
        private float scale = 1f;
        private float width = 1280f;
        private float height = 720f;
        private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        private readonly List<Rect> blockedRects = new List<Rect>();
        private readonly List<ItemData> bagItems = new List<ItemData>();
        private GUIStyle invisibleButton;
        private GUIStyle scrollBar;
        private GUIStyle scrollThumb;
        private Texture2D thumbTexture;
        private Texture2D trackTexture;
        private readonly Texture2D[] crestTextures = new Texture2D[3];
        private string tooltip;
        private readonly Color ink = new Color(.035f, .065f, .10f, .97f);
        private readonly Color card = new Color(.06f, .105f, .15f, .96f);
        private readonly Color jade = new Color(.32f, .91f, .77f);
        private readonly Color gold = new Color(1f, .76f, .37f);
        private readonly Color muted = new Color(.76f, .82f, .89f);
        private readonly Color pale = new Color(.97f, .985f, 1f);

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
                SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
                if (targeting != null && (targeting.IsTargeting || targeting.CancelledThisFrame))
                {
                    targeting.Cancel();
                    return;
                }
                if (panel != Panel.None) ClosePanel();
                else session.SetPaused(!session.Paused);
            }
            if (session.Paused) return;
            if (panel == Panel.Controls || panel == Panel.SaveLocation || panel == Panel.Bindings) return;
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
            for (int i = 0; i < crestTextures.Length; i++) if (crestTextures[i] != null) Destroy(crestTextures[i]);
            UIIconAtlas.Clear();
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
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.enabled = true;
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
                if (panel == Panel.None && !session.Paused && !session.IsDead) DrawTargetingHint();
                if (session.IsDead) DrawDeath();
                else if (session.Paused) DrawPause();
                else if (panel == Panel.Inventory) DrawInventory();
                else if (panel == Panel.Skills) DrawSkills();
                else if (panel == Panel.Bindings) DrawBindings();
                else if (panel == Panel.SaveLocation) DrawSaveLocation();
                else if (panel == Panel.Controls) DrawControls();
                DrawNotification();
            }
            DrawTooltip();
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.contentColor = oldContentColor;
            GUI.enabled = oldEnabled;
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
            GUIStyle style = Style(size, bold, wrap, align);
            Color previousTextColor = style.normal.textColor;
            // Keep the global tint neutral; the style owns the intended text color.
            GUI.contentColor = Color.white;
            style.normal.textColor = color;
            GUI.Label(rect, value ?? "", style);
            style.normal.textColor = previousTextColor;
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
            // The title is a static, opaque composition; world geometry cannot leak through it.
            Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            float x = (width - 1040) * .5f;
            float y = (height - 590) * .5f;
            Fill(new Rect(x + 7, y + 9, 1040, 590), new Color(.006f, .012f, .021f, 1f));
            Fill(new Rect(x, y, 1040, 590), new Color(.048f, .074f, .112f, 1f));
            Border(new Rect(x, y, 1040, 590), new Color(.22f, .33f, .43f, 1f));
            Fill(new Rect(x + 32, y + 26, 36, 3), jade);
            Text(new Rect(x + 81, y + 17, 580, 24), "EMBERFALL  /  即时战斗 RPG", 12, new Color(.81f, .9f, .96f), true);
            Text(new Rect(x + 30, y + 49, 660, 60), "星烬纪元", 42, Color.white, true);
            Text(new Rect(x + 33, y + 113, 740, 26), "选择职业，开始你的冒险。打怪、探索副本，收集属于你的装备。", 16, new Color(.83f, .88f, .94f));
            Text(new Rect(x + 789, y + 45, 217, 30), "单人冒险  ·  本地存档", 13, muted, false, false, TextAnchor.MiddleRight);
            Fill(new Rect(x + 32, y + 155, 976, 1), new Color(.2f, .3f, .39f, 1f));
            string[] mottos = { "以利刃守望黎明", "令群星回应召唤", "让疾风追随箭矢" };
            string[] roles = { "近战 / 范围斩击 / 耐久", "远程 / 控制 / 法术爆发", "远程 / 灵活 / 群体射击" };
            for (int i = 0; i < 3; i++)
            {
                HeroClass hero = (HeroClass)i;
                Color accent = GameBalance.ClassColor(hero);
                Rect choice = new Rect(x + 32 + i * 328, y + 179, 312, 258);
                bool selected = selectedClass == hero;
                Fill(choice, selected ? Color.Lerp(new Color(.063f, .096f, .143f, 1f), accent, .12f) : new Color(.063f, .096f, .143f, 1f));
                Border(choice, selected ? accent : new Color(.22f, .32f, .42f, 1f), selected ? 2 : 1);
                Fill(new Rect(choice.x, choice.y, choice.width, 3), selected ? accent : new Color(.28f, .39f, .5f, 1f));
                Text(new Rect(choice.x + 21, choice.y + 18, 270, 22), "0" + (i + 1) + "   /   " + (selected ? "已选择" : "点击选择"), 12, selected ? accent : muted, true);
                Fill(new Rect(choice.x + 20, choice.y + 54, 84, 88), new Color(.035f, .059f, .09f, 1f));
                DrawCrest(new Rect(choice.x + 21, choice.y + 55, 82, 86), hero, accent);
                Text(new Rect(choice.x + 121, choice.y + 64, 170, 40), GameBalance.ClassName(hero), 29, Color.white, true);
                Text(new Rect(choice.x + 121, choice.y + 107, 174, 26), mottos[i], 13, new Color(.86f, .91f, .97f));
                Fill(new Rect(choice.x + 21, choice.y + 155, 270, 1), new Color(.25f, .35f, .45f, 1f));
                Text(new Rect(choice.x + 21, choice.y + 166, 274, 25), roles[i], 15, pale);
                Text(new Rect(choice.x + 21, choice.y + 201, 274, 42), GameBalance.ClassDescriptions[i], 13, muted, false, true);
                if (GUI.Button(choice, GUIContent.none, invisibleButton)) selectedClass = hero;
            }
            bool canContinue = session.Progression.HasSave;
            Text(new Rect(x + 32, y + 460, 535, 26), "每职业 8 主动 + 2 被动  ·  初习 / 强化 / 觉醒", 16, gold, true);
            Text(new Rect(x + 32, y + 494, 535, 47), "沿技能分支成长，组合主动与被动能力。\n升级装备，迎接更高阶的遗迹挑战。", 13, muted, false, true);
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
            Text(new Rect(x + 32, y + 560, 976, 18), "升级学习技能  ·  探索三波副本  ·  进度自动保存在本机", 11, muted, false, false, TextAnchor.MiddleCenter);
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

        private void DrawCrest(Rect r, HeroClass hero, Color color)
        {
            int index = (int)hero;
            if (crestTextures[index] == null) crestTextures[index] = CreateCrest(hero, color);
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(r, crestTextures[index], ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private static Texture2D CreateCrest(HeroClass hero, Color color)
        {
            const int size = 256;
            var pixels = new Color[size * size];
            Color dim = new Color(color.r, color.g, color.b, .46f);
            CrestStroke(pixels, size, 64, 9, 119, 64, dim, 1.5f);
            CrestStroke(pixels, size, 119, 64, 64, 119, dim, 1.5f);
            CrestStroke(pixels, size, 64, 119, 9, 64, dim, 1.5f);
            CrestStroke(pixels, size, 9, 64, 64, 9, dim, 1.5f);
            if (hero == HeroClass.Vanguard)
            {
                CrestStroke(pixels, size, 64, 24, 54, 41, color, 3.5f);
                CrestStroke(pixels, size, 54, 41, 58, 76, color, 3.5f);
                CrestStroke(pixels, size, 64, 24, 74, 41, color, 3.5f);
                CrestStroke(pixels, size, 74, 41, 70, 76, color, 3.5f);
                CrestStroke(pixels, size, 64, 32, 64, 75, Color.white, 2.5f);
                CrestStroke(pixels, size, 43, 78, 85, 78, color, 5);
                CrestStroke(pixels, size, 64, 79, 64, 99, color, 6);
                CrestStroke(pixels, size, 57, 102, 71, 102, color, 4);
            }
            else if (hero == HeroClass.Arcanist)
            {
                CrestStroke(pixels, size, 64, 23, 85, 64, color, 4);
                CrestStroke(pixels, size, 85, 64, 64, 105, color, 4);
                CrestStroke(pixels, size, 64, 105, 43, 64, color, 4);
                CrestStroke(pixels, size, 43, 64, 64, 23, color, 4);
                CrestStroke(pixels, size, 33, 64, 95, 64, color, 2.5f);
                CrestStroke(pixels, size, 64, 39, 64, 89, dim, 2.5f);
                CrestStroke(pixels, size, 64, 64, 64, 64, Color.white, 10);
            }
            else
            {
                CrestStroke(pixels, size, 80, 25, 57, 37, color, 4);
                CrestStroke(pixels, size, 57, 37, 46, 64, color, 4);
                CrestStroke(pixels, size, 46, 64, 57, 91, color, 4);
                CrestStroke(pixels, size, 57, 91, 80, 103, color, 4);
                CrestStroke(pixels, size, 80, 25, 69, 64, dim, 2.5f);
                CrestStroke(pixels, size, 69, 64, 80, 103, dim, 2.5f);
                CrestStroke(pixels, size, 31, 64, 103, 64, Color.white, 3);
                CrestStroke(pixels, size, 91, 53, 103, 64, color, 4);
                CrestStroke(pixels, size, 103, 64, 91, 75, color, 4);
                CrestStroke(pixels, size, 31, 55, 41, 64, color, 3);
                CrestStroke(pixels, size, 31, 73, 41, 64, color, 3);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Class crest " + hero,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // Bake antialiased strokes once. Drawing the finished texture never changes GUI.matrix.
        private static void CrestStroke(Color[] pixels, int size, float ax, float ay, float bx, float by, Color color, float thickness)
        {
            float factor = size / 128f;
            Vector2 a = new Vector2(ax, ay) * factor;
            Vector2 b = new Vector2(bx, by) * factor;
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;
            float radius = thickness * factor * .5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 point = new Vector2(x + .5f, y + .5f);
                float t = lengthSquared < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
                float alpha = color.a * Mathf.Clamp01(radius + .75f - Vector2.Distance(point, a + segment * t));
                if (alpha <= 0) continue;
                int index = (size - 1 - y) * size + x;
                Color previous = pixels[index];
                float outAlpha = alpha + previous.a * (1 - alpha);
                pixels[index] = new Color((color.r * alpha + previous.r * previous.a * (1 - alpha)) / outAlpha,
                    (color.g * alpha + previous.g * previous.a * (1 - alpha)) / outAlpha,
                    (color.b * alpha + previous.b * previous.a * (1 - alpha)) / outAlpha, outAlpha);
            }
        }

        private void DrawHUD()
        {
            GameProfile p = session.Progression.Profile;
            Color accent = GameBalance.ClassColor(p.heroClass);
            Rect playerRect = new Rect(16, 16, 240, 88);
            blockedRects.Add(playerRect);
            Box(playerRect, accent);
            Fill(new Rect(16, 16, 2, 88), accent);
            Text(new Rect(28, 24, 139, 22), GameBalance.ClassName(p.heroClass) + " · Lv." + p.level, 16, pale, true);
            Text(new Rect(169, 26, 74, 20), Money(p.gold) + " 金", 12, gold, true, false, TextAnchor.UpperRight);
            float hp = session.Player == null ? 0 : session.Player.Health;
            float maxHp = session.Player == null ? 1 : session.Player.MaxHealth;
            Bar(new Rect(28, 54, 216, 10), hp / Mathf.Max(1, maxHp), new Color(.26f, .77f, .61f));
            float energy = session.Player == null ? 0 : session.Player.Energy;
            float maxEnergy = session.Player == null ? 100 : session.Player.MaxEnergy;
            Bar(new Rect(28, 71, 216, 7), energy / Mathf.Max(1, maxEnergy), new Color(.28f, .57f, .91f));
            bool maxLevel = p.level >= ProgressionService.MaximumLevel;
            Bar(new Rect(28, 88, 216, 3), maxLevel ? 1 : p.xp / (float)GameBalance.XpToNext(p.level), gold);
            if (playerRect.Contains(Mouse) && GUI.enabled)
                tooltip = "生命 " + Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp) + "\n" + GameBalance.EnergyName(p.heroClass) + " " + Mathf.FloorToInt(energy) + " / " + Mathf.RoundToInt(maxEnergy) + "\n" + (maxLevel ? "已达最高等级" : "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)) + "\n金币 " + p.gold + " · 生命药剂 " + p.potions;
            Rect objective = new Rect(16, 112, 240, 23);
            blockedRects.Add(objective);
            string objectiveText = session.InDungeon
                ? session.DungeonCleared ? "副本 · 已通关，返回营地整备" : "副本 · 第 " + session.DungeonWave + "/" + session.TotalWaves + " 波 · 剩余 " + session.Enemies.Count + " 个敌人"
                : p.level < 2 ? "目标 · 击败原野怪物，升至 2 级"
                : p.skillRanks[0] == 0 ? "目标 · 学习第一个职业技能"
                : "目标 · 收集装备，挑战北方遗迹";
            Text(objective, objectiveText, 11, pale);
            if (objective.Contains(Mouse) && GUI.enabled)
                tooltip = session.Objective + (session.InDungeon ? "\n通关后按 T 返回营地。远离敌人后可按 H 提前撤离。" : "\n靠近紫色传送门按 T 进入副本。远离敌人后可按 H 回营。");
            DrawMinimap();
            DrawHotbar();
            DrawDungeonStatus();
            DrawEdgeActions();
            EnemyController target = session.Player == null ? null : session.Player.AimTarget;
            if (target != null && !target.IsDead)
            {
                bool neutral = target.Tier == EnemyController.ThreatTier.Normal && !target.IsAggro;
                string state = neutral ? "中立" : target.Tier == EnemyController.ThreatTier.Normal ? "反击中" : "主动敌人";
                Color tint = neutral ? jade : target.Tier == EnemyController.ThreatTier.Elite ? gold : new Color(1, .55f, .45f);
                string effects = target.StatusEffects == null ? "" : target.StatusEffects.Summary;
                Text(new Rect((width - 560) * .5f, 68, 560, 19), target.DisplayName + " · " + state + (string.IsNullOrEmpty(effects) ? "" : " · " + effects), 11, tint, true, false, TextAnchor.MiddleCenter);
            }
        }

        private void DrawTargetingHint()
        {
            SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
            if (targeting == null || !targeting.IsTargeting) return;
            Rect strip = new Rect((width - 490) * .5f, height - 197, 490, 43);
            blockedRects.Add(strip);
            Box(strip, jade, false);
            Fill(new Rect(strip.x, strip.y, 3, strip.height), jade);
            Text(new Rect(strip.x + 10, strip.y + 5, strip.width - 20, 17), "准备施放 · " + targeting.SkillName, 12, jade, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(strip.x + 10, strip.y + 25, strip.width - 20, 14), targeting.Hint, 10, pale, false, false, TextAnchor.MiddleCenter);
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
            float x = width - 166;
            Rect map = new Rect(x, 16, 150, 154);
            blockedRects.Add(map);
            Box(map, jade);
            Text(new Rect(x + 8, 23, 134, 19), session.ZoneName, 11, pale, true, false, TextAnchor.MiddleCenter);
            Rect field = new Rect(x + 11, 49, 128, 109);
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
                if (enemy != null && !enemy.IsDead)
                {
                    Color dot = enemy.Tier == EnemyController.ThreatTier.Boss ? new Color(1, .32f, .3f) : enemy.Tier == EnemyController.ThreatTier.Elite ? gold : enemy.IsAggro ? new Color(1, .58f, .35f) : new Color(.54f, .77f, .5f);
                    MapDot(field, enemy.transform.position, dot, enemy.IsBoss ? 6 : 3);
                }
            }
            if (session.Player != null) MapDot(field, session.Player.transform.position, jade, 6);
            if (map.Contains(Mouse) && GUI.enabled)
                tooltip = session.ZoneName + "\n青色：你 · 紫色：传送门 · 金色：营地 / 精英\n绿色：中立普通怪 · 橙色：反击中 · 红色：首领";
            if (session.InDungeon)
                Text(new Rect(x, 178, 150, 18), "波次 " + Mathf.Min(session.DungeonWave, session.TotalWaves) + " / " + session.TotalWaves, 11, gold, false, false, TextAnchor.MiddleRight);
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
            float x = (width - 282) * .5f;
            float y = height - 141;
            Rect bar = new Rect(x, y, 282, 129);
            blockedRects.Add(bar);
            Box(bar, jade);
            if (Button(new Rect(x + 10, y + 4, 22, 18), "‹", jade, true, "上一页技能栏 / [")) ChangePage(-1);
            Text(new Rect(x + 39, y + 5, 43, 17), (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages, 10, pale, true, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(x + 90, y + 4, 22, 18), "›", jade, true, "下一页技能栏 / Tab 或 ]")) ChangePage(1);
            for (int slotIndex = 0; slotIndex < GameBalance.HotbarSize; slotIndex++)
            {
                int skill = SkillAtSlot(p, slotIndex);
                bool empty = skill < 0;
                int rank = empty ? 0 : p.skillRanks[skill];
                bool locked = empty || rank == 0;
                float cost = empty ? 0 : GameBalance.SkillEnergyCosts[skill];
                bool lacksEnergy = !locked && session.Player != null && session.Player.Energy < cost;
                float cooldown = empty || session.Player == null ? 0 : session.Player.CooldownRemaining(slotIndex);
                Rect slot = new Rect(x + 10 + (slotIndex % 5) * 53, y + 27 + (slotIndex / 5) * 50, 48, 46);
                Color accent = empty ? muted : GameBalance.ClassColor(p.heroClass);
                Fill(slot, locked ? new Color(.04f, .06f, .085f) : card);
                Border(slot, new Color(accent.r, accent.g, accent.b, locked ? .23f : .55f));
                if (!empty)
                    DrawIcon(new Rect(slot.x + 8, slot.y + 10, 32, 32), UIIconAtlas.Skill(p.heroClass, skill), locked ? new Color(.53f, .57f, .63f) : lacksEnergy ? new Color(.55f, .68f, .85f) : Color.white);
                else Text(new Rect(slot.x, slot.y + 9, slot.width, 32), "+", 20, new Color(.34f, .44f, .53f), false, false, TextAnchor.MiddleCenter);
                if (cooldown > .01f)
                {
                    float cover = slot.height * Mathf.Clamp01(cooldown / GameBalance.EffectiveCooldown(skill, rank));
                    Fill(new Rect(slot.x + 1, slot.yMax - cover, slot.width - 2, cover), new Color(0, .025f, .04f, .76f));
                    Text(new Rect(slot.x, slot.y + 12, slot.width, 29), cooldown.ToString(cooldown >= 10 ? "0" : "0.0"), 15, pale, true, false, TextAnchor.MiddleCenter);
                }
                string key = GameBalance.KeyName(p.hotbarKeys[slotIndex]);
                Fill(new Rect(slot.x + 2, slot.y + 2, Mathf.Max(14, key.Length * 7 + 4), 13), new Color(.015f, .025f, .04f, .93f));
                Text(new Rect(slot.x + 4, slot.y + 1, 39, 15), key, 9, locked ? muted : pale, true);
                if (lacksEnergy) Fill(new Rect(slot.x + 2, slot.yMax - 3, slot.width - 4, 2), new Color(.45f, .64f, 1f));
                bool hover = slot.Contains(Mouse);
                if (hover && GUI.enabled)
                {
                    Border(slot, gold);
                    tooltip = empty ? "空技能槽 · 按 K 学习技能并配置。\n左键打开技能树 · Tab / [ ] 切换技能栏。" :
                        GameBalance.SkillName(p.heroClass, skill) + " · " + GameBalance.SkillRankName(rank) + "\n" +
                        GameBalance.SkillDescription(p.heroClass, skill) + "\n冷却 " + GameBalance.EffectiveCooldown(skill, rank).ToString("0.#") +
                        " 秒 · 消耗 " + cost.ToString("0") + " " + GameBalance.EnergyName(p.heroClass) +
                        (rank == 0 ? "\n尚未学习 · 点击查看前置与等级要求。" :
                        "\n按 " + key + " 或左键选择施法位置；再次左键确认。\n右键打开此技能配置 · 换页共用冷却。");
                }
                if (hover && GUI.enabled && Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    Event.current.Use();
                    if (!empty) SelectSkill(skill);
                    TogglePanel(Panel.Skills);
                }
                if (GUI.Button(slot, GUIContent.none, invisibleButton))
                {
                    if (locked)
                    {
                        if (!empty) SelectSkill(skill);
                        TogglePanel(Panel.Skills);
                    }
                    else
                    {
                        SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
                        if (targeting != null) targeting.Begin(skill);
                    }
                }
            }
        }

        private static void DrawIcon(Rect r, Texture2D texture, Color tint)
        {
            if (texture == null) return;
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private bool IconButton(Rect r, string icon, string key, string hint, Color accent, string badge = null)
        {
            blockedRects.Add(r);
            bool hover = r.Contains(Mouse) && GUI.enabled;
            Fill(r, hover ? new Color(.11f, .18f, .21f) : ink);
            Border(r, new Color(accent.r, accent.g, accent.b, hover ? .9f : .35f));
            DrawIcon(new Rect(r.x + 7, r.y + 8, r.width - 14, r.height - 13), UIIconAtlas.Utility(icon), Color.white);
            Text(new Rect(r.x + 3, r.y + 1, r.width - 6, 12), key, 8, pale, true);
            if (!string.IsNullOrEmpty(badge))
            {
                Rect label = new Rect(r.xMax - 21, r.yMax - 15, 20, 14);
                Fill(label, new Color(.06f, .08f, .10f));
                Text(label, badge, 9, gold, true, false, TextAnchor.MiddleCenter);
            }
            if (hover) tooltip = hint;
            return GUI.Button(r, GUIContent.none, invisibleButton);
        }

        private void DrawEdgeActions()
        {
            GameProfile p = session.Progression.Profile;
            float x = width - 238;
            float y = height - 54;
            if (IconButton(new Rect(x, y, 38, 38), "inventory", "I", "行囊与装备 · I\n查看属性、替换与强化装备，出售闲置物品，购买药剂。", jade))
                TogglePanel(Panel.Inventory);
            if (IconButton(new Rect(x + 46, y, 38, 38), "skills", "K", "技能树 · K\n按分支学习或进阶技能，配置三页快捷栏。\n可用技能点：" + p.skillPoints, gold, p.skillPoints > 0 ? "+" + p.skillPoints : null))
                TogglePanel(Panel.Skills);
            if (IconButton(new Rect(x + 92, y, 38, 38), "camp", "H", "返回营地 · H\n附近没有敌人时可以返回营地整备。", jade))
                session.ReturnToCamp();
            if (IconButton(new Rect(x + 138, y, 38, 38), "portal", "T", session.InDungeon ? "返回营地 · T\n通关后返回营地；提前撤离需要远离敌人。" : "进入副本 · T\n靠近北面的紫色传送门后进入副本。", gold))
            {
                if (session.InDungeon) session.ReturnToCamp();
                else session.EnterDungeon();
            }
            if (IconButton(new Rect(x + 184, y, 38, 38), "help", "", "操作指南\n查看移动、战斗、技能施法与自定义快捷键。", muted)) OpenControls();
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
            skillScroll.y = Mathf.Clamp(GameBalance.SkillTreeRow(selectedSkill) * 102 - 150, 0, 738 - 468);
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
            RebuildBagItems();
            ItemData picked = ResolveSelectedItem();
            Rect w = Modal(1160, 638, "行囊与装备", "选择物品查看属性与替换效果 · 背包中的闲置装备可直接出售");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 789, w.y + 28, 268, 30), Money(p.gold) + " 金币", 21, gold, true, false, TextAnchor.MiddleRight);
            float left = w.x + 24;
            Text(new Rect(left, w.y + 112, 232, 23), "身上装备", 16, jade, true);
            for (int i = 0; i < 3; i++)
            {
                ItemData item = progression.Equipped((ItemSlot)i);
                Rect row = new Rect(left, w.y + 148 + i * 78, 232, 66);
                bool chosen = item != null && item.id == selectedItem;
                Fill(row, chosen ? new Color(.10f, .19f, .23f) : card);
                Color color = item == null ? muted : GameBalance.RarityColor(item.rarity);
                Fill(new Rect(row.x, row.y, 3, row.height), color);
                if (chosen) Border(row, jade);
                Text(new Rect(row.x + 12, row.y + 9, 135, 16), GameBalance.SlotName((ItemSlot)i), 11, muted);
                Text(new Rect(row.x + 154, row.y + 9, 65, 16), "穿戴中", 10, jade, false, false, TextAnchor.MiddleRight);
                Text(new Rect(row.x + 12, row.y + 33, 208, 24), item == null ? "暂无装备" : ItemTitle(item), 14, color, true);
                if (item != null && GUI.Button(row, GUIContent.none, invisibleButton)) selectedItem = item.id;
            }
            StatBlock stats = progression.GetStats();
            Rule(left, w.y + 389, 232, jade);
            Text(new Rect(left, w.y + 402, 232, 22), "角色属性 · Lv." + p.level, 15, jade, true);
            StatLine(left, w.y + 433, "攻击", Mathf.RoundToInt(stats.Damage).ToString(), gold);
            StatLine(left, w.y + 461, "防御", Mathf.RoundToInt(stats.Armor).ToString(), pale);
            StatLine(left, w.y + 489, "生命上限", Mathf.RoundToInt(stats.MaxHealth).ToString(), pale);
            StatLine(left, w.y + 517, "暴击几率", Mathf.RoundToInt(stats.CritChance * 100) + "%", pale);

            float middle = w.x + 272;
            Text(new Rect(middle, w.y + 112, 280, 23), "背包 · " + bagItems.Count + " / " + unequippedCount + " 件", 16, jade, true);
            Text(new Rect(middle + 280, w.y + 117, 144, 17), "总容量 " + p.inventory.Count + " / " + ProgressionService.InventoryCapacity, 11, muted, false, false, TextAnchor.MiddleRight);
            bool changed = false;
            string[] filters = { "全部", "武器", "护甲", "饰品" };
            for (int i = 0; i < filters.Length; i++)
                if (Button(new Rect(middle + i * 108, w.y + 144, 100, 27), filters[i], inventoryFilter == i - 1 ? gold : jade, true, null, inventoryFilter == i - 1))
                {
                    inventoryFilter = i - 1;
                    inventoryScroll = Vector2.zero;
                    changed = true;
                }
            Text(new Rect(middle, w.y + 184, 33, 18), "排序", 10, muted);
            string[] sorts = { "综合属性 ↓", "等级 ↓", "稀有度 ↓" };
            for (int i = 0; i < sorts.Length; i++)
                if (Button(new Rect(middle + 38 + i * 129, w.y + 180, 121, 25), sorts[i], inventorySort == i ? gold : muted, true, "降序排列；相同数值依次按等级、稀有度与物品编号排序。", inventorySort == i))
                {
                    inventorySort = i;
                    inventoryScroll = Vector2.zero;
                    changed = true;
                }
            if (changed) { RebuildBagItems(); ResolveSelectedItem(); }
            Rect viewport = new Rect(middle, w.y + 213, 424, 330);
            Fill(viewport, new Color(.025f, .05f, .075f));
            float contentHeight = Mathf.Max(viewport.height - 2, bagItems.Count * 76 + 4);
            Rect content = new Rect(0, 0, 407, contentHeight);
            inventoryScroll.y = Mathf.Clamp(inventoryScroll.y, 0, Mathf.Max(0, contentHeight - viewport.height));
            GUIStyle priorThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, content, false, true, GUIStyle.none, scrollBar);
            string sellId = null;
            for (int rowIndex = 0; rowIndex < bagItems.Count; rowIndex++)
            {
                ItemData item = bagItems[rowIndex];
                Rect row = new Rect(4, 4 + rowIndex * 76, 398, 68);
                bool chosen = item.id == selectedItem;
                Fill(row, chosen ? new Color(.1f, .2f, .23f) : card);
                Color color = GameBalance.RarityColor(item.rarity);
                Fill(new Rect(row.x, row.y, 3, row.height), color);
                if (chosen) Border(row, jade * new Color(1, 1, 1, .55f));
                Text(new Rect(row.x + 12, row.y + 8, 260, 22), ItemTitle(item), 15, color, true);
                Text(new Rect(row.x + 12, row.y + 33, 78, 16), "等级 " + item.level, 11, muted);
                Text(new Rect(row.x + 93, row.y + 33, 74, 16), GameBalance.RarityName(item.rarity), 11, color);
                Text(new Rect(row.x + 175, row.y + 33, 84, 16), GameBalance.SlotName(item.slot), 11, muted);
                Text(new Rect(row.x + 12, row.y + 51, 250, 14), "综合评分 " + ProgressionService.EquipmentScore(item).ToString("0.#"), 10, muted);
                Rect selectRect = new Rect(row.x, row.y, 278, row.height);
                if (GUI.Button(selectRect, GUIContent.none, invisibleButton)) selectedItem = item.id;
                Rect sellRect = new Rect(row.x + 281, row.y + 13, 106, 40);
                // Keep the sale target separate from the selection target inside the scroll view.
                Fill(sellRect, new Color(.14f, .125f, .08f));
                Border(sellRect, new Color(gold.r, gold.g, gold.b, .42f));
                Text(new Rect(sellRect.x, sellRect.y + 4, sellRect.width, 15), "出售", 10, gold, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(sellRect.x, sellRect.y + 21, sellRect.width, 16), progression.SellValue(item) + " 金", 12, pale, true, false, TextAnchor.MiddleCenter);
                if (GUI.Button(sellRect, GUIContent.none, invisibleButton)) sellId = item.id;
                Rect visibleRow = new Rect(viewport.x + row.x, viewport.y + row.y - inventoryScroll.y, 277, row.height);
                if (viewport.Contains(Mouse) && visibleRow.Contains(Mouse))
                    tooltip = ItemTitle(item) + "\n攻击 " + item.attack + " · 防御 " + item.defense + " · 生命 " + item.health + "\n综合评分用于装备比较，不代表实际职业 DPS。\n点击查看替换属性；右侧价格按钮直接出售。";
            }
            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            if (sellId != null) SellInventoryItem(sellId);
            picked = ResolveSelectedItem();
            if (bagItems.Count == 0)
                Text(new Rect(middle + 22, w.y + 322, 380, 76), inventoryFilter < 0 ? "背包已整理完毕\n继续打怪或探索副本，收集新的战利品。" : "这个分类暂无闲置装备\n切换分类，或继续探索收集战利品。", 16, muted, false, true, TextAnchor.MiddleCenter);
            DrawItemDetail(new Rect(w.x + 712, w.y + 112, 424, 431), picked);
            Rect supply = new Rect(left, w.y + 558, 1112, 50);
            Fill(supply, card);
            Text(new Rect(supply.x + 15, supply.y + 8, 255, 21), "生命药剂  × " + p.potions, 15, pale, true);
            Text(new Rect(supply.x + 15, supply.y + 30, 530, 16), "按 F 使用 · 恢复 50% 最大生命 · 整备时战斗暂停", 11, muted);
            if (Button(new Rect(supply.x + 848, supply.y + 8, 248, 34), "购买药剂 · " + ProgressionService.PotionPrice + " 金", gold, p.gold >= ProgressionService.PotionPrice, "购买一瓶生命药剂。"))
                Feedback(progression.BuyPotion(), "已购买生命药剂 · -" + ProgressionService.PotionPrice + " 金币");
            Text(new Rect(left, w.y + 616, 1112, 16), "穿戴中装备不可出售 · 综合评分用于属性比较，并非职业 DPS", 11, muted, false, false, TextAnchor.MiddleCenter);
        }

        private void RebuildBagItems()
        {
            bagItems.Clear();
            unequippedCount = 0;
            List<ItemData> inventory = session.Progression.Profile.inventory;
            for (int i = inventory.Count - 1; i >= 0; i--)
                if (inventory[i] != null && !IsEquipped(inventory[i]))
                {
                    unequippedCount++;
                    if (inventoryFilter < 0 || (int)inventory[i].slot == inventoryFilter) bagItems.Add(inventory[i]);
                }
            bagItems.Sort(CompareInventoryItems);
        }

        private int CompareInventoryItems(ItemData a, ItemData b)
        {
            int comparison = inventorySort == 1 ? b.level.CompareTo(a.level) : inventorySort == 2 ? b.rarity.CompareTo(a.rarity) : ProgressionService.EquipmentScore(b).CompareTo(ProgressionService.EquipmentScore(a));
            if (comparison != 0) return comparison;
            comparison = b.level.CompareTo(a.level);
            if (comparison != 0) return comparison;
            comparison = b.rarity.CompareTo(a.rarity);
            return comparison != 0 ? comparison : string.CompareOrdinal(a.id, b.id);
        }

        private ItemData ResolveSelectedItem()
        {
            List<ItemData> inventory = session.Progression.Profile.inventory;
            for (int i = 0; i < inventory.Count; i++)
                if (inventory[i] != null && inventory[i].id == selectedItem && (IsEquipped(inventory[i]) || bagItems.Contains(inventory[i]))) return inventory[i];
            ItemData replacement = bagItems.Count > 0 ? bagItems[0] : session.Progression.Equipped(ItemSlot.Weapon);
            if (replacement == null)
                for (int i = inventory.Count - 1; i >= 0; i--)
                    if (inventory[i] != null) { replacement = inventory[i]; break; }
            selectedItem = replacement == null ? null : replacement.id;
            return replacement;
        }

        private void SellInventoryItem(string id)
        {
            int row = bagItems.FindIndex(item => item.id == id);
            if (row < 0) return;
            ItemData item = bagItems[row];
            if (IsEquipped(item)) return;
            int before = session.Progression.Profile.gold;
            bool sold = session.Progression.Sell(id);
            int gained = session.Progression.Profile.gold - before;
            Feedback(sold, "已出售 " + item.name + " · +" + gained + " 金币");
            if (!sold) return;
            bool replaceSelection = selectedItem == id;
            RebuildBagItems();
            if (replaceSelection)
                selectedItem = bagItems.Count == 0 ? null : bagItems[Mathf.Min(row, bagItems.Count - 1)].id;
            ResolveSelectedItem();
            inventoryScroll.y = Mathf.Clamp(inventoryScroll.y, 0, Mathf.Max(0, bagItems.Count * 76 + 4 - 330));
        }

        private void StatLine(float x, float y, string label, string value, Color color)
        {
            Text(new Rect(x + 2, y, 151, 22), label, 13, muted);
            Text(new Rect(x + 157, y, 78, 22), value, 15, color, true, false, TextAnchor.UpperRight);
        }

        private void DrawItemDetail(Rect r, ItemData item)
        {
            Fill(r, card);
            if (item == null)
            {
                Text(new Rect(r.x + 24, r.y + 175, r.width - 48, 70), "选择一件装备\n在这里查看属性、装备与强化。", 16, muted, false, true, TextAnchor.MiddleCenter);
                return;
            }
            ProgressionService progression = session.Progression;
            bool isEquipped = IsEquipped(item);
            Color rarityColor = GameBalance.RarityColor(item.rarity);
            Fill(new Rect(r.x, r.y, r.width, 3), rarityColor);
            Text(new Rect(r.x + 18, r.y + 17, r.width - 36, 23), GameBalance.RarityName(item.rarity) + " / " + GameBalance.SlotName(item.slot), 13, rarityColor, true);
            Text(new Rect(r.x + 18, r.y + 51, r.width - 36, 55), ItemTitle(item), 25, pale, true, true);
            Text(new Rect(r.x + 18, r.y + 112, r.width - 36, 22), "装备等级 " + item.level + "    ·    强化 +" + item.upgradeLevel + (isEquipped ? "    ·    穿戴中" : ""), 13, muted);
            Rule(r.x + 18, r.y + 146, r.width - 36, rarityColor);
            ItemData equipped = progression.Equipped(item.slot);
            Text(new Rect(r.x + 18, r.y + 160, 100, 18), "属性", 11, muted);
            Text(new Rect(r.x + 137, r.y + 160, 163, 18), isEquipped ? "当前数值" : "当前 → 选中", 11, muted, false, false, TextAnchor.MiddleRight);
            Text(new Rect(r.x + 320, r.y + 160, 86, 18), "替换变化", 11, muted, false, false, TextAnchor.MiddleRight);
            ItemStat(r.x + 18, r.y + 190, "攻击", item.attack, equipped == null ? 0 : equipped.attack, isEquipped);
            ItemStat(r.x + 18, r.y + 224, "防御", item.defense, equipped == null ? 0 : equipped.defense, isEquipped);
            ItemStat(r.x + 18, r.y + 258, "生命", item.health, equipped == null ? 0 : equipped.health, isEquipped);
            Text(new Rect(r.x + 18, r.y + 295, r.width - 36, 29), isEquipped ? "属性已计入角色，可继续强化。" : equipped == null ? "此部位尚未穿戴装备。" : "正在对比：" + ItemTitle(equipped), 12, muted, false, true);
            bool canEquip = item.level <= progression.Profile.level && !isEquipped;
            string equipCaption = isEquipped ? "已装备" : !canEquip ? "需要 Lv." + item.level : "装备此物品";
            if (Button(new Rect(r.x + 18, r.y + 338, 187, 40), equipCaption, jade, canEquip, null, true)) Feedback(progression.Equip(item.id), "已装备 " + item.name);
            bool maxUpgrade = item.upgradeLevel >= ProgressionService.MaximumUpgrade;
            int upgradeCost = progression.UpgradeCost(item);
            if (Button(new Rect(r.x + 219, r.y + 338, 187, 40), maxUpgrade ? "已强化至 +" + ProgressionService.MaximumUpgrade : "强化 · " + upgradeCost + " 金", gold, !maxUpgrade && progression.Profile.gold >= upgradeCost, maxUpgrade ? "此装备已达到强化上限。" : "消耗 " + upgradeCost + " 金币，永久提升这件装备的属性。"))
                Feedback(progression.Upgrade(item.id), "强化成功 · -" + upgradeCost + " 金币");
            Text(new Rect(r.x + 18, r.y + 394, r.width - 36, 22), isEquipped ? "穿戴中的装备不可出售" : "出售可得 " + progression.SellValue(item) + " 金币 · 点击背包列表中的「出售」", 11, isEquipped ? muted : gold, false, false, TextAnchor.MiddleCenter);
        }

        private void ItemStat(float x, float y, string name, int value, int previous, bool equipped)
        {
            Text(new Rect(x, y, 90, 24), name, 14, muted);
            Text(new Rect(x + 104, y, 177, 24), equipped ? value.ToString() : previous + " → " + value, 17, pale, true, false, TextAnchor.UpperRight);
            int diff = value - previous;
            string delta = equipped || diff == 0 ? "—" : (diff > 0 ? "+" : "") + diff;
            Text(new Rect(x + 296, y, 92, 24), delta, 14, diff >= 0 ? jade : new Color(1f, .49f, .42f), true, false, TextAnchor.UpperRight);
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
            Rect w = Modal(1160, 660, GameBalance.ClassName(p.heroClass) + " · 技能树", "沿分支由上向下学习 · 先掌握连线上的前置技能 · 每升一级获得 1 技能点");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 763, w.y + 28, 296, 32), "技能点 " + p.skillPoints + "   /   角色 Lv." + p.level, 18, gold, true, false, TextAnchor.MiddleRight);
            Text(new Rect(w.x + 24, w.y + 112, 267, 24), "职业分支 · 滚动查看高阶技能", 15, jade, true);
            if (Button(new Rect(w.x + 325, w.y + 108, 205, 29), "自定义快捷键", gold)) OpenBindings();
            Rect viewport = new Rect(w.x + 24, w.y + 147, 506, 468);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 490, 738);
            GUIStyle priorThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            skillScroll.y = Mathf.Clamp(skillScroll.y, 0, content.height - viewport.height);
            skillScroll = GUI.BeginScrollView(viewport, skillScroll, content, false, true, GUIStyle.none, scrollBar);
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                Rect node = SkillNodeRect(skill);
                int[] parents = GameBalance.SkillPrerequisites[skill];
                for (int i = 0; i < parents.Length; i++)
                {
                    Rect parent = SkillNodeRect(parents[i]);
                    Color connection = p.skillRanks[parents[i]] > 0 ? new Color(.25f, .61f, .53f) : new Color(.23f, .30f, .36f);
                    float bend = node.y - 13 - i * 5;
                    Fill(new Rect(parent.center.x - 1, parent.yMax, 2, bend - parent.yMax), connection);
                    Fill(new Rect(Mathf.Min(parent.center.x, node.center.x), bend, Mathf.Max(2, Mathf.Abs(parent.center.x - node.center.x)), 2), connection);
                    Fill(new Rect(node.center.x - 1, bend, 2, node.y - bend), connection);
                    Fill(new Rect(node.center.x - 3, node.y - 5, 6, 5), connection);
                }
            }
            for (int i = 0; i < GameBalance.SkillCount; i++)
            {
                int rank = p.skillRanks[i];
                int required = GameBalance.SkillRequiredLevels[i];
                bool passive = GameBalance.IsPassive(i);
                bool prerequisitesMet = session.Progression.PrerequisitesMet(i);
                bool canLearn = string.IsNullOrEmpty(session.Progression.SkillLockReason(i));
                Rect node = SkillNodeRect(i);
                Color accent = rank > 0 ? jade : canLearn ? gold : muted;
                Fill(node, selectedSkill == i ? new Color(.12f, .20f, .23f) : rank > 0 ? new Color(.06f, .145f, .15f) : card);
                Border(node, selectedSkill == i ? gold : new Color(accent.r, accent.g, accent.b, rank > 0 || canLearn ? .7f : .25f), selectedSkill == i ? 2 : 1);
                Fill(new Rect(node.x + 9, node.y + 8, 3, 15), accent);
                Text(new Rect(node.x + 17, node.y + 7, 117, 24), GameBalance.SkillName(p.heroClass, i), 14, rank > 0 || canLearn ? pale : muted, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(node.x + 5, node.y + 35, 134, 18), "Lv." + required + " / " + (passive ? "被动" : "主动"), 11, passive ? new Color(.82f, .74f, .98f) : muted, false, false, TextAnchor.MiddleCenter);
                string state = rank > 0 ? GameBalance.SkillRankName(rank) + (canLearn ? " · 可进阶" : " · 已学习") : canLearn ? "可学习" : !prerequisitesMet ? "需要前置" : p.level < required ? "等级未达" : "需要技能点";
                Text(new Rect(node.x + 5, node.y + 57, 134, 17), state, 11, accent, true, false, TextAnchor.MiddleCenter);
                Rect visibleNode = new Rect(viewport.x + node.x, viewport.y + node.y - skillScroll.y, node.width, node.height);
                if (viewport.Contains(Mouse) && visibleNode.Contains(Mouse))
                    tooltip = GameBalance.SkillName(p.heroClass, i) + " · " + GameBalance.PrerequisiteDescription(p.heroClass, i) + "\n点击查看技能效果、进阶和快捷栏配置。";
                if (GUI.Button(node, GUIContent.none, invisibleButton)) selectedSkill = i;
            }
            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            DrawSkillDetail(new Rect(w.x + 550, w.y + 112, 586, 510), selectedSkill);
            Text(new Rect(w.x + 25, w.y + 626, 505, 18), "青色：已学习   金色：可学习   灰色：条件未满足", 11, muted);
            Text(new Rect(w.x + 553, w.y + 635, 579, 17), "升级保留快捷栏位置 · 三页共用冷却", 11, muted, false, false, TextAnchor.MiddleRight);
        }

        private static Rect SkillNodeRect(int skill)
        {
            return new Rect(10 + GameBalance.SkillTreeColumn(skill) * 160, 18 + GameBalance.SkillTreeRow(skill) * 102, 144, 82);
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
            Text(new Rect(r.x + 18, r.y + 15, 550, 35), GameBalance.SkillName(p.heroClass, skill), 27, pale, true);
            Text(new Rect(r.x + 18, r.y + 54, 550, 20), (passive ? "被动" : "主动") + " / " + GameBalance.CategoryName(category) + " / " + GameBalance.SkillRankName(rank), 12, accent, true);
            Text(new Rect(r.x + 18, r.y + 80, 550, 33), GameBalance.SkillDescription(p.heroClass, skill), 13, muted, false, true);
            string[] labels = { "当前冷却", "资源消耗", "初习解锁", "进阶成长" };
            string[] values = { passive ? "自动生效" : GameBalance.EffectiveCooldown(skill, rank).ToString("0.#") + " 秒", passive ? "无需消耗" : GameBalance.SkillEnergyCosts[skill].ToString("0") + " " + GameBalance.EnergyName(p.heroClass), "Lv." + GameBalance.SkillRequiredLevels[skill], "强化 → 觉醒" };
            for (int i = 0; i < 4; i++)
            {
                Rect stat = new Rect(r.x + 18 + i * 140, r.y + 120, 130, 43);
                Fill(stat, new Color(.035f, .075f, .11f));
                Text(new Rect(stat.x + 9, stat.y + 4, 112, 14), labels[i], 10, muted);
                Text(new Rect(stat.x + 9, stat.y + 23, 112, 18), values[i], 12, i == 1 ? jade : pale, true);
            }
            Text(new Rect(r.x + 18, r.y + 177, 550, 18), "学习前置", 11, jade, true);
            Text(new Rect(r.x + 18, r.y + 198, 550, 27), GameBalance.PrerequisiteDescription(p.heroClass, skill), 12, session.Progression.PrerequisitesMet(skill) ? pale : gold, false, true);
            for (int stage = 1; stage <= 3; stage++)
            {
                Rect evolution = new Rect(r.x + 18 + (stage - 1) * 186, r.y + 235, 178, 91);
                bool current = stage == rank;
                Fill(evolution, current ? new Color(.13f, .2f, .2f) : new Color(.045f, .08f, .115f));
                Border(evolution, new Color(accent.r, accent.g, accent.b, current ? .75f : .18f));
                string stageName = GameBalance.SkillRankName(stage) + "  /  Lv." + GameBalance.SkillRankRequiredLevel(skill, stage);
                Text(new Rect(evolution.x + 9, evolution.y + 7, 160, 18), stageName + (current ? " ✓" : ""), 12, stage <= rank ? gold : pale, true);
                int damagePercent = 100 + (stage - 1) * 30;
                int rangePercent = Mathf.RoundToInt(GameBalance.SkillRangeMultiplier(stage) * 100);
                string evolutionText = GameBalance.SkillEvolution(p.heroClass, skill, stage);
                if (showOffenseScale)
                {
                    Text(new Rect(evolution.x + 9, evolution.y + 31, 160, 16), "伤害 " + damagePercent + "% · 范围 " + rangePercent + "%", 10, jade);
                    Text(new Rect(evolution.x + 9, evolution.y + 50, 160, 35), evolutionText, 10, muted, false, true);
                }
                else Text(new Rect(evolution.x + 9, evolution.y + 32, 160, 50), evolutionText, 11, jade, false, true);
                if (evolution.Contains(Mouse)) tooltip = GameBalance.SkillRankName(stage) + "：角色达到 " + GameBalance.SkillRankRequiredLevel(skill, stage) + " 级，消耗 1 技能点。\n" + evolutionText + (passive ? "\n学习后自动生效，无需配置到快捷栏。" : "\n冷却 " + GameBalance.EffectiveCooldown(skill, stage).ToString("0.#") + " 秒。");
            }
            float actionX = r.x + 18;
            string reason = session.Progression.SkillLockReason(skill);
            bool canLearn = string.IsNullOrEmpty(reason);
            string caption = rank == 3 ? "已完全觉醒" : rank == 0 ? "学习初习 · 1 技能点" : "进阶" + GameBalance.SkillRankName(nextRank) + " · 1 技能点";
            if (Button(new Rect(actionX, r.y + 341, 236, 39), caption, gold, canLearn, reason, canLearn))
                Feedback(session.Progression.LearnSkill(skill), GameBalance.SkillName(p.heroClass, skill) + "已达到" + GameBalance.SkillRankName(p.skillRanks[skill]));
            Text(new Rect(r.x + 271, r.y + 341, 296, 40), rank == 3 ? "此技能已完全觉醒。" : canLearn ? "下一阶段：" + GameBalance.SkillRankName(nextRank) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, nextRank) + "\n每次学习或进阶消耗 1 技能点。" : reason, 12, muted, false, true);
            if (passive)
            {
                Rect passiveInfo = new Rect(actionX, r.y + 398, 550, 94);
                Fill(passiveInfo, new Color(.085f, .075f, .15f));
                Border(passiveInfo, new Color(.55f, .42f, .8f, .35f));
                Text(new Rect(passiveInfo.x + 13, passiveInfo.y + 12, 524, 23), rank > 0 ? "被动已生效" : "被动技能", 17, new Color(.8f, .73f, 1f), true);
                Text(new Rect(passiveInfo.x + 13, passiveInfo.y + 46, 524, 38), "学习后自动生效，无需放入快捷栏。\n进阶继续强化效果，不占主动技能槽。", 13, pale, false, true);
                return;
            }
            Text(new Rect(actionX, r.y + 397, 365, 21), "配置到第 " + (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages + " 页 · “+” 配置，“×” 卸下", 12, jade, true);
            if (Button(new Rect(r.x + 436, r.y + 392, 58, 27), "‹ 页", jade, true, "上一页技能栏")) ChangePage(-1);
            if (Button(new Rect(r.x + 503, r.y + 392, 65, 27), "页 ›", jade, true, "下一页技能栏")) ChangePage(1);
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int equipped = SkillAtSlot(p, slot);
                bool current = equipped == skill;
                Rect target = new Rect(actionX + (slot % 5) * 112, r.y + 429 + (slot / 5) * 35, 102, 29);
                string key = GameBalance.KeyName(p.hotbarKeys[slot]);
                string label = key + (current ? " ×" : " +");
                string hint = rank == 0 ? "先学习这项技能。" : current ? "点击从 " + key + " 槽卸下；不会清除技能冷却。" : "将" + GameBalance.SkillName(p.heroClass, skill) + "配置到 " + key + " 槽。当前：" + (equipped < 0 ? "空" : GameBalance.SkillName(p.heroClass, equipped)) + "。";
                if (Button(target, label, current ? gold : jade, rank > 0 && session.CanChangeLoadout, hint, current))
                    session.AssignSkill(slot, current ? -1 : skill);
            }
            float remaining = session.Player == null ? 0 : session.Player.SkillCooldownRemaining(skill);
            if (remaining > .01f)
                Text(new Rect(actionX, r.y + 495, 550, 13), "剩余冷却 " + remaining.ToString("0.0") + " 秒 · 换页不会重置", 9, muted);
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
            if (Button(new Rect(w.x + 397, w.y + 524, 419, 39), bindingReturnPause ? "返回暂停菜单" : bindingReturnPanel == Panel.Controls ? "返回操作指南" : "返回技能研习", jade)) ClosePanel();
        }

        private void DrawPause()
        {
            Rect w = Modal(472, 534, "冒险暂停", "歇一口气，再继续前行。");
            Text(new Rect(w.x + 28, w.y + 116, 416, 31), session.ZoneName + "  ·  Lv." + session.Progression.Profile.level + " " + GameBalance.ClassName(session.Progression.Profile.heroClass), 17, jade, true, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(w.x + 40, w.y + 172, 392, 48), "继续冒险", jade, true, "按 ESC 也可继续冒险。", true)) session.SetPaused(false);
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
            if (Button(new Rect(w.x + 40, w.y + 453, 392, 42), "操作指南", jade)) OpenControls();
        }

        private void OpenControls()
        {
            controlsReturnPause = session.Paused;
            panel = Panel.Controls;
            session.SetUIBlocking(true);
            session.SetPaused(false);
        }

        private void DrawControls()
        {
            GameProfile p = session.Progression.Profile;
            Rect w = Modal(1060, 638, "操作指南", "键盘与鼠标 · 当前技能键帽会跟随你的自定义设置");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Rect keyboard = new Rect(w.x + 24, w.y + 112, 650, 442);
            Fill(keyboard, card);
            Text(new Rect(keyboard.x + 18, keyboard.y + 12, 610, 25), "移动与战斗", 16, jade, true);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 47, 52, 49), "W", "前", jade);
            DrawKeyCap(new Rect(keyboard.x + 23, keyboard.y + 103, 52, 49), "A", "左", jade);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 103, 52, 49), "S", "后", jade);
            DrawKeyCap(new Rect(keyboard.x + 139, keyboard.y + 103, 52, 49), "D", "右", jade);
            DrawKeyCap(new Rect(keyboard.x + 249, keyboard.y + 47, 72, 49), "J", "普通攻击", gold);
            DrawKeyCap(new Rect(keyboard.x + 331, keyboard.y + 47, 72, 49), "F", "生命药剂", gold);
            DrawKeyCap(new Rect(keyboard.x + 249, keyboard.y + 103, 154, 49), "SPACE", "向移动方向闪避", gold);
            Rect mouse = new Rect(keyboard.x + 447, keyboard.y + 46, 175, 106);
            Fill(mouse, new Color(.035f, .065f, .1f));
            Border(mouse, new Color(.29f, .43f, .51f));
            Fill(new Rect(mouse.center.x, mouse.y, 1, 64), new Color(.29f, .43f, .51f));
            Text(new Rect(mouse.x + 4, mouse.y + 10, 79, 20), "左键", 16, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 91, mouse.y + 10, 79, 20), "右键", 16, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 2, mouse.y + 36, 83, 17), "攻击 / 确认", 10, muted, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 89, mouse.y + 36, 83, 17), "取消瞄准", 10, muted, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 7, mouse.y + 74, 161, 18), "滚轮 · 调整镜头距离", 11, jade, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(keyboard.x + 23, keyboard.y + 167, 598, 42), "鼠标指向目标，按住左键或 J 连续普通攻击。\n按技能键进入瞄准，移动鼠标选点或调整朝向；左键确认，右键或 Esc 取消。", 12, pale, false, true);
            Rule(keyboard.x + 20, keyboard.y + 222, 609, jade);
            Text(new Rect(keyboard.x + 22, keyboard.y + 236, 605, 22), "技能快捷键 · 当前设置", 16, jade, true);
            for (int i = 0; i < GameBalance.HotbarSize; i++)
                DrawKeyCap(new Rect(keyboard.x + 23 + (i % 5) * 123, keyboard.y + 273 + (i / 5) * 59, 112, 50), GameBalance.KeyName(p.hotbarKeys[i]), "技能槽 " + (i + 1).ToString("00"), jade);
            Text(new Rect(keyboard.x + 23, keyboard.y + 397, 604, 29), "三页共用这些按键。技能图标左键进入瞄准，右键打开该技能的配置。", 12, muted, false, true);
            float right = w.x + 698;
            Text(new Rect(right, w.y + 114, 332, 23), "界面与冒险", 16, jade, true);
            string[] keys = { "I", "K", "H", "T", "Tab / ]", "[", "Esc" };
            string[] actions = { "行囊、装备与补给", "技能树、学习与配置", "远离敌人后返回营地", "进入传送门 / 通关返回", "下一页技能栏", "上一页技能栏", "取消瞄准 / 返回 / 暂停" };
            for (int i = 0; i < keys.Length; i++)
            {
                float rowY = w.y + 153 + i * 43;
                Rect key = new Rect(right, rowY, 84, 30);
                Fill(key, card);
                Border(key, new Color(.25f, .38f, .46f));
                Text(key, keys[i], 13, pale, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(right + 99, rowY + 4, 234, 25), actions[i], 13, muted);
            }
            Text(new Rect(right, w.y + 472, 330, 71), "技能学习需要等级、技能点和前置技能。\n背包和技能界面会暂停战斗，放心整备。\n鼠标滚轮也可滚动背包与技能树。", 12, muted, false, true);
            if (Button(new Rect(w.x + 24, w.y + 575, 650, 39), "自定义技能按键", gold)) OpenBindings();
            if (Button(new Rect(right, w.y + 575, 338, 39), controlsReturnPause ? "返回暂停菜单" : "返回冒险", jade)) ClosePanel();
        }

        private void DrawKeyCap(Rect r, string key, string action, Color accent)
        {
            Fill(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(.015f, .028f, .045f));
            Fill(r, new Color(.08f, .13f, .18f));
            Border(r, new Color(accent.r, accent.g, accent.b, .4f));
            Text(new Rect(r.x + 4, r.y + 5, r.width - 8, 23), key, 18, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(r.x + 4, r.y + 31, r.width - 8, 14), action, 10, muted, false, false, TextAnchor.MiddleCenter);
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
            if (panel == Panel.Controls)
            {
                panel = Panel.None;
                session.SetUIBlocking(false);
                session.SetPaused(controlsReturnPause);
                controlsReturnPause = false;
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
