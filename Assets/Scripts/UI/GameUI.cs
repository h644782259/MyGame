using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Resolution-independent runtime interface; no scene or package dependencies.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        private enum Panel { None, Inventory, Skills, Bindings, SaveLocation, Controls, UpgradeTransfer }
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
        private int hotbarPointerSlot = -1;
        private int hotbarPointerPage = -1;
        private int hotbarPointerSkill = -1;
        private bool hotbarPointerConfiguring;
        private bool hotbarDragging;
        private Vector2 hotbarPointerOrigin;
        private int hotbarPointerControl;
        private int hotbarReleaseFrame = -1;
        private bool suppressHotbarMouse;
        private int hotbarTouchFinger = -1000;
        private Vector2 hotbarTouchPosition;
        private readonly Rect[] hotbarSlots = new Rect[GameBalance.HotbarSize];
        private Rect hotbarBounds;
        private readonly Rect[] detailSlots = new Rect[GameBalance.HotbarSize];
        private string transferTargetId;
        private string transferSourceId;
        private Vector2 transferScroll;
        private Font font;
        private float scale = 1f;
        private float width = 1280f;
        private float height = 720f;
        private Vector2 guiOffset;
        private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        private readonly List<Rect> blockedRects = new List<Rect>();
        private readonly List<ItemData> bagItems = new List<ItemData>();
        private readonly List<ItemData> transferSources = new List<ItemData>();
        private GUIStyle invisibleButton;
        private GUIStyle scrollBar;
        private GUIStyle scrollThumb;
        private Texture2D thumbTexture;
        private Texture2D trackTexture;
        private readonly Texture2D[] crestTextures = new Texture2D[4];
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
                if (hotbarPointerSlot >= 0 || suppressHotbarMouse || Time.frameCount <= hotbarReleaseFrame) return true;
                if (!session.HasStarted || session.Paused || session.IsDead || panel != Panel.None) return true;
                if (MobileControls.IsScreenPointOverControls(Input.mousePosition)) return true;
                Vector2 mouse = Mouse;
                for (int i = 0; i < blockedRects.Count; i++)
                    if (blockedRects[i].Contains(mouse)) return true;
                return false;
            }
        }

        private Vector2 Mouse { get { return hotbarTouchFinger != -1000 ? hotbarTouchPosition : ScreenToUI(Input.mousePosition); } }
        private Vector2 ScreenToUI(Vector2 point) { return (new Vector2(point.x, Screen.height - point.y) - guiOffset) / scale; }
        public bool IsScreenPointOverUI(Vector2 point)
        {
            if (session == null || !session.HasStarted || session.InputBlocked || panel != Panel.None) return true;
            if (MobileControls.IsScreenPointOverControls(point)) return true;
            Vector2 position = ScreenToUI(point);
            foreach (Rect rect in blockedRects) if (rect.Contains(position)) return true;
            return false;
        }
        public bool TryBeginTouchSkill(int finger, Vector2 point)
        {
            if (!MobileControls.Active || hotbarPointerSlot >= 0 || session == null || !session.HasStarted || session.Paused || session.IsDead) return false;
            RefreshLayout();
            bool configuring = panel == Panel.Skills && !GameBalance.IsPassive(selectedSkill);
            if (panel != Panel.None && !configuring) return false;
            Rect[] slots = configuring ? detailSlots : hotbarSlots;
            Vector2 position = ScreenToUI(point);
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Contains(position)) continue;
                BeginHotbarPointer(i, position, configuring);
                hotbarTouchFinger = finger;
                hotbarTouchPosition = position;
                return true;
            }
            return false;
        }
        public void UpdateTouchSkill(int finger, Vector2 point, bool ended, bool cancelled)
        {
            if (finger != hotbarTouchFinger || hotbarPointerSlot < 0) return;
            Vector2 position = ScreenToUI(point);
            hotbarTouchPosition = position;
            ContinueHotbarPointer(position);
            if (!ended) return;
            if (cancelled) { CancelHotbarPointer(); return; }
            Rect[] slots = hotbarPointerConfiguring ? detailSlots : hotbarSlots;
            int target = -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].Contains(position)) { target = i; break; }
            CompleteHotbarPointer(target);
        }

        public void Initialize(GameSession gameSession)
        {
            session = gameSession;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "PingFang SC", "PingFangSC-Regular", "Heiti SC", "SimHei", "Arial" }, 18);
        }

        // Input and rendering share geometry, including before the first repaint
        // or immediately after a safe-area/orientation change.
        private void RefreshLayout()
        {
            Rect safe = MobileControls.SafeArea;
            scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            scale = Mathf.Max(.3f, scale);
            width = safe.width / scale;
            height = safe.height / scale;
            guiOffset = new Vector2(safe.x, Screen.height - safe.yMax);
            bool mobile = MobileControls.Active;
            float barWidth = mobile ? 362 : 282;
            hotbarBounds = new Rect((width - barWidth) * .5f, height - (mobile ? 177 : 141), barWidth, mobile ? 165 : 129);
            for (int slot = 0; slot < hotbarSlots.Length; slot++)
                hotbarSlots[slot] = new Rect(hotbarBounds.x + 10 + (slot % 5) * (mobile ? 69 : 53),
                    hotbarBounds.y + 27 + (slot / 5) * (mobile ? 65 : 50), mobile ? 64 : 48, mobile ? 61 : 46);
        }

        private void Update()
        {
            RefreshLayout();
            if (session == null) return;
            if (suppressHotbarMouse && !Input.GetMouseButton(0)) suppressHotbarMouse = false;
            if (hotbarPointerSlot >= 0 && (!session.HasStarted || session.Paused || session.IsDead ||
                session.Progression.Profile.hotbarPage != hotbarPointerPage ||
                (hotbarPointerConfiguring ? panel != Panel.Skills || GameBalance.IsPassive(selectedSkill) : panel != Panel.None))) CancelHotbarPointer();
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
                if (hotbarPointerSlot >= 0) { CancelHotbarPointer(); return; }
                SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
                if (charge != null && (charge.IsCharging || charge.CancelledThisFrame))
                {
                    charge.Cancel();
                    return;
                }
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
            if (panel == Panel.Controls || panel == Panel.SaveLocation || panel == Panel.Bindings || panel == Panel.UpgradeTransfer) return;
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
            RefreshLayout();
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Heiti SC", "Arial" }, 18);
            if (invisibleButton == null) BuildStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(guiOffset, Quaternion.identity, new Vector3(scale, scale, 1));
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
                else if (panel == Panel.UpgradeTransfer) DrawUpgradeTransfer();
                DrawNotification();
            }
            if (hotbarDragging && hotbarPointerSkill >= 0)
            {
                tooltip = null;
                DrawIcon(new Rect(Mouse.x + 11, Mouse.y + 11, 36, 36), UIIconAtlas.Skill(session.Progression.Profile.heroClass, hotbarPointerSkill), Color.white);
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

        private static string PlatformText(string value)
        {
            if (!MobileControls.Active || string.IsNullOrEmpty(value)) return value;
            return value.Replace("WASD 移动，鼠标瞄准", "拖动左侧摇杆移动，点击技能或按住攻击")
                .Replace("按空格闪避", "点击闪避按钮")
                .Replace("按 K ", "打开技能树").Replace("按 I ", "打开行囊")
                .Replace("按 T ", "点击传送按钮").Replace("按 H ", "点击回营按钮")
                .Replace("按 F ", "点击药剂按钮")
                .Replace(" · I", "").Replace(" · K", "").Replace(" · H", "").Replace(" · T", "");
        }

        private static string SkillCastHint(HeroClass hero, int skill, string key, bool mobile)
        {
            bool ground = SkillTargetingController.RequiresConfirmation(hero, skill);
            float duration = SkillChargeController.Duration(hero, skill);
            string start = mobile ? "点击技能" : "按 " + key + " 或左键";
            string timing = duration > 0 ? "蓄力 " + duration.ToString("0.##") + " 秒后施放" : "立即施放";
            if (ground)
                return start + (mobile ? "进入地面选点；松开选点或点击确认，" : "进入地面选点；左键确认，") +
                    timing + (mobile ? "。点击取消按钮取消。" : "。右键 / Esc 取消。");
            return start + timing + (duration > 0 ? (mobile ? "；闪避或取消按钮可中断。" : "；闪避、右键 / Esc 可中断。") : "，无需再次确认。");
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
            if (clicked) GameAudio.Play(SoundCue.UI);
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
            float y = (height - 438) * .5f;
            Fill(new Rect(x + 7, y + 9, 1040, 438), new Color(.006f, .012f, .021f, 1f));
            Fill(new Rect(x, y, 1040, 438), new Color(.048f, .074f, .112f, 1f));
            Border(new Rect(x, y, 1040, 438), new Color(.22f, .33f, .43f, 1f));
            Text(new Rect(x + 32, y + 24, 976, 43), "选择职业", 32, Color.white, true);
            Fill(new Rect(x + 32, y + 81, 976, 1), new Color(.2f, .3f, .39f, 1f));
            string[] roles = { "近战 · 范围斩击 · 耐久", "远程 · 控制 · 法术爆发", "远程 · 灵活 · 群体射击", "召唤 · 协同 · 灵兽守护" };
            for (int i = 0; i < 4; i++)
            {
                HeroClass hero = (HeroClass)i;
                Color accent = GameBalance.ClassColor(hero);
                Rect choice = new Rect(x + 32 + i * 247, y + 104, 235, 228);
                bool selected = selectedClass == hero;
                Fill(choice, selected ? Color.Lerp(new Color(.063f, .096f, .143f, 1f), accent, .12f) : new Color(.063f, .096f, .143f, 1f));
                Border(choice, selected ? accent : new Color(.22f, .32f, .42f, 1f), selected ? 2 : 1);
                Fill(new Rect(choice.x, choice.y, choice.width, 3), selected ? accent : new Color(.28f, .39f, .5f, 1f));
                Text(new Rect(choice.x + 17, choice.y + 16, 201, 19), selected ? "已选择" : "", 11, accent, true, false, TextAnchor.MiddleRight);
                DrawCrest(new Rect(choice.x + 74, choice.y + 40, 87, 90), hero, accent);
                Text(new Rect(choice.x + 17, choice.y + 145, 201, 36), GameBalance.ClassName(hero), 27, Color.white, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(choice.x + 17, choice.y + 193, 201, 21), roles[i], 12, pale, false, false, TextAnchor.MiddleCenter);
                if (GUI.Button(choice, GUIContent.none, invisibleButton)) { selectedClass = hero; GameAudio.Play(SoundCue.UI); }
            }
            bool canContinue = session.Progression.HasSave;
            if (Button(new Rect(x + 314, y + 359, 186, 50), "继续冒险", jade, canContinue, canContinue ? "读取本机最近的冒险存档。" : "开始一次冒险后可读取本地存档。"))
            {
                panel = Panel.None;
                session.ContinueGame();
            }
            if (Button(new Rect(x + 520, y + 359, 206, 50), "开始冒险", gold, true, "以" + GameBalance.ClassName(selectedClass) + "开始新冒险。", true))
            {
                if (canContinue) confirmNewGame = true;
                else StartSelectedHero();
            }
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
            else if (hero == HeroClass.Summoner)
            {
                CrestStroke(pixels, size, 45, 58, 35, 36, color, 5);
                CrestStroke(pixels, size, 35, 36, 58, 48, color, 5);
                CrestStroke(pixels, size, 83, 58, 93, 36, color, 5);
                CrestStroke(pixels, size, 93, 36, 70, 48, color, 5);
                CrestStroke(pixels, size, 45, 58, 45, 84, color, 4);
                CrestStroke(pixels, size, 45, 84, 64, 99, color, 4);
                CrestStroke(pixels, size, 64, 99, 83, 84, color, 4);
                CrestStroke(pixels, size, 83, 84, 83, 58, color, 4);
                CrestStroke(pixels, size, 51, 68, 55, 68, Color.white, 6);
                CrestStroke(pixels, size, 73, 68, 77, 68, Color.white, 6);
                CrestStroke(pixels, size, 64, 82, 64, 87, Color.white, 5);
                CrestStroke(pixels, size, 64, 18, 64, 35, dim, 4);
                CrestStroke(pixels, size, 55, 26, 73, 26, dim, 4);
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
            Rect objective = new Rect(16, 114, 282, 81);
            blockedRects.Add(objective);
            Box(objective, jade, false);
            Fill(new Rect(objective.x, objective.y, 3, objective.height), jade);
            Text(new Rect(objective.x + 13, objective.y + 8, 255, 17), "当前目标", 11, jade, true);
            string objectiveText = session.InDungeon
                ? session.DungeonCleared ? "沉星遗迹已通关" : "击败本轮敌人"
                : p.level < 2 ? "击败原野怪物，升至 2 级"
                : p.skillRanks[0] == 0 ? "学习首个职业技能"
                : "前往北方的沉星遗迹";
            string objectiveProgress = session.InDungeon
                ? session.DungeonCleared ? "返回营地整备 · T" : "第 " + session.DungeonWave + " / " + session.TotalWaves + " 波 · 剩余 " + session.Enemies.Count + " 个敌人"
                : p.level < 2 ? "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)
                : p.skillRanks[0] == 0 ? "可用技能点 " + p.skillPoints + " · K"
                : "收集装备，进入传送门 · T";
            Text(new Rect(objective.x + 13, objective.y + 29, 255, 24), objectiveText, 15, pale, true);
            Text(new Rect(objective.x + 13, objective.y + 57, 255, 18), PlatformText(objectiveProgress), 12, muted);
            if (objective.Contains(Mouse) && GUI.enabled)
                tooltip = PlatformText(session.Objective + (session.InDungeon ? "\n通关后按 T 返回营地。远离敌人后可按 H 提前撤离。" : "\n靠近紫色传送门按 T 进入副本。远离敌人后可按 H 回营。"));
            DrawMinimap();
            DrawHotbar();
            DrawChargeProgress();
            DrawDungeonStatus();
            DrawEdgeActions();
            EnemyController target = session.Player == null ? null : session.Player.AimTarget;
            if (target != null && !target.IsDead)
            {
                bool neutral = target.Tier == EnemyController.ThreatTier.Normal && !target.IsAggro;
                string state = neutral ? "中立" : target.Tier == EnemyController.ThreatTier.Normal ? "反击中" : "主动敌人";
                Color tint = neutral ? jade : target.Tier == EnemyController.ThreatTier.Elite ? gold : new Color(1, .55f, .45f);
                string effects = target.StatusEffects == null ? "" : target.StatusEffects.Summary;
                Rect targetInfo = new Rect((width - 560) * .5f, 68, 560, 19);
                blockedRects.Add(targetInfo);
                Text(targetInfo, target.DisplayName + " · " + state + (string.IsNullOrEmpty(effects) ? "" : " · " + effects), 11, tint, true, false, TextAnchor.MiddleCenter);
                if (targetInfo.Contains(Mouse) && GUI.enabled) tooltip = target.DisplayName + "\n" + target.TraitDescription;
            }
        }

        private void DrawTargetingHint()
        {
            SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
            if (targeting == null || !targeting.IsTargeting) return;
            Rect strip = new Rect((width - 490) * .5f, height - (MobileControls.Active ? 235 : 197), 490, 43);
            blockedRects.Add(strip);
            Box(strip, jade, false);
            Fill(new Rect(strip.x, strip.y, 3, strip.height), jade);
            Text(new Rect(strip.x + 10, strip.y + 5, strip.width - 20, 17), "准备施放 · " + targeting.SkillName, 12, jade, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(strip.x + 10, strip.y + 25, strip.width - 20, 14), MobileControls.Active ? "点选地面并松开，或点击确认按钮 · 点击取消按钮取消" : targeting.Hint, 10, pale, false, false, TextAnchor.MiddleCenter);
        }

        private void DrawChargeProgress()
        {
            SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
            if (charge == null || !charge.IsCharging || charge.SkillIndex < 0) return;
            GameProfile profile = session.Progression.Profile;
            Rect strip = new Rect((width - 226) * .5f, height - (MobileControls.Active ? 224 : 186), 226, 35);
            blockedRects.Add(strip);
            Box(strip, GameBalance.ClassColor(profile.heroClass), false);
            DrawIcon(new Rect(strip.x + 5, strip.y + 4, 27, 27), UIIconAtlas.Skill(profile.heroClass, charge.SkillIndex), Color.white);
            Text(new Rect(strip.x + 40, strip.y + 3, 178, 15), GameBalance.SkillName(profile.heroClass, charge.SkillIndex), 11, pale, true);
            Bar(new Rect(strip.x + 40, strip.y + 23, 175, 5), charge.Progress, GameBalance.ClassColor(profile.heroClass));
            if (strip.Contains(Mouse)) tooltip = MobileControls.Active ? "蓄力中 · 移动减速\n点击闪避或取消按钮中断蓄力。" : "蓄力中 · 移动减速\n空格闪避 / 右键 / Esc 取消。";
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
                Text(new Rect(victory.x + 18, victory.y + 63, 458, 22), "三波挑战完成 · 地面战利品可拾取", 13, pale, false, false, TextAnchor.MiddleCenter);
                if (Button(new Rect(victory.x + 99, victory.y + 105, 296, 36), MobileControls.Active ? "返回营地整备" : "返回营地整备  /  T", gold, true, "下一次遗迹挑战将提升难度。", true)) session.ReturnToCamp();
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
            bool mobile = MobileControls.Active;
            Rect bar = hotbarBounds;
            float x = bar.x;
            float y = bar.y;
            blockedRects.Add(bar);
            Box(bar, jade);
            if (Button(new Rect(x + 10, y + 4, 22, 18), "‹", jade, true, mobile ? "上一页技能栏" : "上一页技能栏 / [")) ChangePage(-1);
            Text(new Rect(x + 39, y + 5, 43, 17), (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages, 10, pale, true, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(x + 90, y + 4, 22, 18), "›", jade, true, mobile ? "下一页技能栏" : "下一页技能栏 / Tab 或 ]")) ChangePage(1);
            for (int slotIndex = 0; slotIndex < GameBalance.HotbarSize; slotIndex++)
            {
                int skill = LearnedSkillAtSlot(p, slotIndex);
                bool empty = skill < 0;
                int rank = empty ? 0 : p.skillRanks[skill];
                bool locked = empty;
                float cost = empty ? 0 : GameBalance.SkillEnergyCost(p.heroClass, skill);
                bool lacksEnergy = !locked && session.Player != null && session.Player.Energy < cost;
                float cooldown = empty || session.Player == null ? 0 : session.Player.CooldownRemaining(slotIndex);
                Rect slot = hotbarSlots[slotIndex];
                Color accent = empty ? muted : GameBalance.ClassColor(p.heroClass);
                Fill(slot, locked ? new Color(.04f, .06f, .085f) : card);
                Border(slot, new Color(accent.r, accent.g, accent.b, locked ? .23f : .55f));
                if (hotbarDragging && !hotbarPointerConfiguring && (slotIndex == hotbarPointerSlot || slot.Contains(Mouse))) Border(slot, gold, 2);
                if (!empty)
                    DrawIcon(new Rect(slot.center.x - (mobile ? 22 : 16), slot.y + 10, mobile ? 44 : 32, mobile ? 44 : 32), UIIconAtlas.Skill(p.heroClass, skill), locked ? new Color(.53f, .57f, .63f) : lacksEnergy ? new Color(.55f, .68f, .85f) : Color.white);
                else Text(new Rect(slot.x, slot.y + 9, slot.width, 32), "+", 20, new Color(.34f, .44f, .53f), false, false, TextAnchor.MiddleCenter);
                if (cooldown > .01f)
                {
                    float cover = slot.height * Mathf.Clamp01(cooldown / GameBalance.EffectiveCooldown(p.heroClass, skill, rank));
                    Fill(new Rect(slot.x + 1, slot.yMax - cover, slot.width - 2, cover), new Color(0, .025f, .04f, .76f));
                    Text(new Rect(slot.x, slot.y + 12, slot.width, 29), cooldown.ToString(cooldown >= 10 ? "0" : "0.0"), 15, pale, true, false, TextAnchor.MiddleCenter);
                }
                string key = GameBalance.KeyName(p.hotbarKeys[slotIndex]);
                if (!mobile)
                {
                    Fill(new Rect(slot.x + 2, slot.y + 2, Mathf.Max(14, key.Length * 7 + 4), 13), new Color(.015f, .025f, .04f, .93f));
                    Text(new Rect(slot.x + 4, slot.y + 1, 39, 15), key, 9, locked ? muted : pale, true);
                }
                if (lacksEnergy) Fill(new Rect(slot.x + 2, slot.yMax - 3, slot.width - 4, 2), new Color(.45f, .64f, 1f));
                bool hover = slot.Contains(Mouse);
                if (hover && GUI.enabled)
                {
                    Border(slot, gold);
                    tooltip = empty ? (mobile ? "未配置技能\n点击打开技能树，配置已学习技能。" : "空技能槽 · 仅显示已学习的技能。\n左键或右键打开技能树，配置已学习技能。\nTab / [ ] 切换技能栏。") :
                        GameBalance.SkillName(p.heroClass, skill) + " · " + GameBalance.SkillRankName(rank) + "\n" +
                        GameBalance.SkillDescription(p.heroClass, skill) + "\n冷却 " + GameBalance.EffectiveCooldown(p.heroClass, skill, rank).ToString("0.#") +
                        " 秒 · 消耗 " + cost.ToString("0") + " " + GameBalance.EnergyName(p.heroClass) +
                        "\n" + SkillCastHint(p.heroClass, skill, key, mobile) +
                        "\n拖到另一格移动或交换，拖到栏外取消。" + (mobile ? "\n换页共用冷却。" : "\n右键打开此技能配置 · 换页共用冷却。");
                }
                if (!mobile && hover && GUI.enabled && Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    Event.current.Use();
                    if (!empty) SelectSkill(skill);
                    TogglePanel(Panel.Skills);
                }
            }
            HandleHotbarPointer(hotbarSlots, false);
        }

        private void HandleHotbarPointer(Rect[] slots, bool configuring)
        {
            if (MobileControls.Active) return;
            int control = GUIUtility.GetControlID(configuring ? 192702 : 192701, FocusType.Passive);
            if (!GUI.enabled) return;
            Event input = Event.current;
            int hovered = -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].Contains(input.mousePosition)) { hovered = i; break; }
            if (input.type == EventType.MouseDown && input.button == 0 && hovered >= 0)
            {
                BeginHotbarPointer(hovered, input.mousePosition, configuring);
                hotbarPointerControl = control;
                GUIUtility.hotControl = control;
                input.Use();
                return;
            }
            if (hotbarPointerSlot < 0 || hotbarPointerConfiguring != configuring) return;
            if (input.type == EventType.MouseDrag && input.button == 0)
            {
                ContinueHotbarPointer(input.mousePosition);
                input.Use();
            }
            else if (input.type == EventType.MouseUp && input.button == 0)
            {
                ContinueHotbarPointer(input.mousePosition);
                CompleteHotbarPointer(hovered);
                input.Use();
            }
            else if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape)
            {
                CancelHotbarPointer();
                input.Use();
            }
        }

        private void BeginHotbarPointer(int slot, Vector2 position, bool configuring)
        {
            if (slot < 0 || slot >= GameBalance.HotbarSize || !session.CanChangeLoadout) return;
            hotbarPointerSlot = slot;
            hotbarPointerPage = session.Progression.Profile.hotbarPage;
            hotbarPointerSkill = LearnedSkillAtSlot(session.Progression.Profile, slot);
            hotbarPointerConfiguring = configuring;
            hotbarPointerOrigin = position;
            hotbarDragging = false;
        }

        private void ContinueHotbarPointer(Vector2 position)
        {
            if (hotbarPointerSlot >= 0 && hotbarPointerSkill >= 0 &&
                (position - hotbarPointerOrigin).sqrMagnitude * scale * scale >= 36f) hotbarDragging = true;
        }

        private void CompleteHotbarPointer(int targetSlot)
        {
            int source = hotbarPointerSlot;
            int skill = hotbarPointerSkill;
            bool dragged = hotbarDragging;
            bool configuring = hotbarPointerConfiguring;
            bool valid = source >= 0 && targetSlot >= 0 && targetSlot < GameBalance.HotbarSize &&
                session.CanChangeLoadout && !session.Paused && !session.IsDead &&
                (configuring ? panel == Panel.Skills : panel == Panel.None) && session.Progression.Profile.hotbarPage == hotbarPointerPage &&
                LearnedSkillAtSlot(session.Progression.Profile, source) == skill;
            CancelHotbarPointer();
            if (!valid) return;
            if (dragged)
            {
                if (source != targetSlot && session.MoveHotbarSkill(source, targetSlot)) GameAudio.Play(SoundCue.UI);
                return;
            }
            if (source != targetSlot) return;
            if (configuring)
            {
                GameProfile profile = session.Progression.Profile;
                if (profile.skillRanks[selectedSkill] <= 0 || GameBalance.IsPassive(selectedSkill)) return;
                if (session.AssignSkill(source, skill == selectedSkill ? -1 : selectedSkill)) GameAudio.Play(SoundCue.UI);
            }
            else if (skill < 0) { TogglePanel(Panel.Skills); GameAudio.Play(SoundCue.UI); }
            else
            {
                SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
                if (targeting != null) targeting.Begin(skill);
            }
        }

        private void CancelHotbarPointer()
        {
            if (hotbarPointerControl != 0 && GUIUtility.hotControl == hotbarPointerControl) GUIUtility.hotControl = 0;
            hotbarReleaseFrame = Time.frameCount;
            suppressHotbarMouse = Input.GetMouseButton(0);
            hotbarPointerSlot = hotbarPointerPage = hotbarPointerSkill = -1;
            hotbarPointerControl = 0;
            hotbarTouchFinger = -1000;
            hotbarDragging = false;
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
            if (!MobileControls.Active) Text(new Rect(r.x + 3, r.y + 1, r.width - 6, 12), key, 8, pale, true);
            if (!string.IsNullOrEmpty(badge))
            {
                Rect label = new Rect(r.xMax - 21, r.yMax - 15, 20, 14);
                Fill(label, new Color(.06f, .08f, .10f));
                Text(label, badge, 9, gold, true, false, TextAnchor.MiddleCenter);
            }
            if (hover) tooltip = PlatformText(hint);
            bool clicked = GUI.Button(r, GUIContent.none, invisibleButton);
            if (clicked) GameAudio.Play(SoundCue.UI);
            return clicked;
        }

        private void DrawEdgeActions()
        {
            GameProfile p = session.Progression.Profile;
            float x = width - (MobileControls.Active ? 284 : 238);
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
            if (MobileControls.Active && IconButton(new Rect(x + 230, y, 38, 38), "pause", "", "暂停冒险", muted)) session.SetPaused(true);
        }

        private static int SkillAtSlot(GameProfile profile, int slot)
        {
            if (profile == null || slot < 0 || slot >= GameBalance.HotbarSize) return -1;
            int index = profile.hotbarPage * GameBalance.HotbarSize + slot;
            if (profile.equippedSkills == null || index < 0 || index >= profile.equippedSkills.Length) return -1;
            int skill = profile.equippedSkills[index];
            return skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill) ? skill : -1;
        }

        private static int LearnedSkillAtSlot(GameProfile profile, int slot)
        {
            int skill = SkillAtSlot(profile, slot);
            return skill >= 0 && profile.skillRanks != null && skill < profile.skillRanks.Length && profile.skillRanks[skill] > 0 ? skill : -1;
        }

        private static string SlotSkillName(GameProfile profile, int slot)
        {
            int skill = LearnedSkillAtSlot(profile, slot);
            return skill < 0 ? "未配置" : GameBalance.SkillName(profile.heroClass, skill);
        }

        private void DrawSlotIdentity(Rect r, GameProfile profile, int slot, Color tint)
        {
            int skill = LearnedSkillAtSlot(profile, slot);
            float iconSize = Mathf.Min(24, r.height);
            if (skill >= 0) DrawIcon(new Rect(r.x, r.y + (r.height - iconSize) * .5f, iconSize, iconSize), UIIconAtlas.Skill(profile.heroClass, skill), Color.white);
            Text(new Rect(r.x + (skill < 0 ? 0 : iconSize + 4), r.y, r.width - (skill < 0 ? 0 : iconSize + 4), r.height), SlotSkillName(profile, slot), 11, tint, false, false, TextAnchor.MiddleLeft);
        }

        private static int AssignedSlot(GameProfile profile, int skill)
        {
            for (int i = 0; i < GameBalance.HotbarSize; i++) if (SkillAtSlot(profile, i) == skill) return i;
            return -1;
        }

        private void ChangePage(int direction)
        {
            if (hotbarPointerSlot >= 0) CancelHotbarPointer();
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
            Rect w = Modal(1160, 638, "行囊与装备", "");
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
            if (new Rect(left, w.y + 514, 232, 29).Contains(Mouse))
            {
                int baseCrit = p.heroClass == HeroClass.Ranger ? 14 : p.heroClass == HeroClass.Arcanist ? 10 : 8;
                int totalCrit = Mathf.RoundToInt(stats.CritChance * 100);
                int passiveCrit = Mathf.Max(0, totalCrit - baseCrit);
                string passive = p.heroClass == HeroClass.Ranger
                    ? GameBalance.SkillName(p.heroClass, 3) + "（" + GameBalance.SkillRankName(p.skillRanks[3]) + "）：+" + passiveCrit + " 个百分点"
                    : "被动加成：+0 个百分点";
                tooltip = "暴击几率来源\n" + GameBalance.ClassName(p.heroClass) + "基础：" + baseCrit + "%\n" + passive +
                    "\n" + baseCrit + "% + " + passiveCrit + "% = " + totalCrit + "%\n装备和角色等级目前不提供暴击率。\n暴击伤害为普通伤害的 1.65 倍。";
            }

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
                bool improvement = IsEquipmentUpgrade(item);
                Fill(row, chosen ? new Color(.1f, .2f, .23f) : card);
                Color color = GameBalance.RarityColor(item.rarity);
                Fill(new Rect(row.x, row.y, 3, row.height), color);
                if (chosen) Border(row, jade * new Color(1, 1, 1, .55f));
                Text(new Rect(row.x + 12, row.y + 8, improvement ? 188 : 260, 22), ItemTitle(item), 15, color, true);
                if (improvement) DrawEquipmentUpgradeTag(new Rect(row.x + 208, row.y + 10, 64, 18));
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
                    tooltip = ItemTitle(item) + "\n攻击 " + item.attack + " · 防御 " + item.defense + " · 生命 " + item.health +
                        (improvement ? "\n↑ " + EquipmentUpgradeHint(item) : "") +
                        "\n综合评分用于装备比较，不代表实际职业 DPS。\n点击查看替换属性；右侧价格按钮直接出售。";
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
            Rect potionSummary = new Rect(supply.x + 15, supply.y + 8, 530, 34);
            Text(potionSummary, "生命药剂  × " + p.potions, 15, pale, true, false, TextAnchor.MiddleLeft);
            if (potionSummary.Contains(Mouse)) tooltip = "生命药剂 · 按 F 使用\n恢复 50% 最大生命；背包整备时战斗暂停。";
            if (Button(new Rect(supply.x + 848, supply.y + 8, 248, 34), "购买药剂 · " + ProgressionService.PotionPrice + " 金", gold, p.gold >= ProgressionService.PotionPrice, "购买一瓶生命药剂。"))
                Feedback(progression.BuyPotion(), "已购买生命药剂 · -" + ProgressionService.PotionPrice + " 金币");
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
            if (IsEquipmentUpgrade(item))
            {
                Rect tag = new Rect(r.xMax - 88, r.y + 18, 70, 20);
                DrawEquipmentUpgradeTag(tag);
                if (tag.Contains(Mouse)) tooltip = EquipmentUpgradeHint(item);
            }
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
            if (!isEquipped) Text(new Rect(r.x + 18, r.y + 295, r.width - 36, 29), equipped == null ? "当前部位未穿戴" : "对比：" + ItemTitle(equipped), 12, muted, false, true);
            bool canEquip = item.level <= progression.Profile.level && !isEquipped;
            string equipCaption = isEquipped ? "已装备" : !canEquip ? "需要 Lv." + item.level : "装备此物品";
            if (Button(new Rect(r.x + 18, r.y + 338, 187, 40), equipCaption, jade, canEquip, null, true)) Feedback(progression.Equip(item.id), "已装备 " + item.name);
            bool maxUpgrade = item.upgradeLevel >= ProgressionService.MaximumUpgrade;
            int upgradeCost = progression.UpgradeCost(item);
            if (Button(new Rect(r.x + 219, r.y + 338, 187, 40), maxUpgrade ? "已强化至 +" + ProgressionService.MaximumUpgrade : "强化 · " + upgradeCost + " 金", gold, !maxUpgrade && progression.Profile.gold >= upgradeCost, maxUpgrade ? "此装备已达到强化上限。" : "消耗 " + upgradeCost + " 金币，永久提升这件装备的属性。"))
                Feedback(progression.Upgrade(item.id), "强化成功 · -" + upgradeCost + " 金币");
            bool hasSource = HasUpgradeTransferSource(item);
            if (Button(new Rect(r.x + 18, r.y + 388, r.width - 36, 30), hasSource ? "继承强化" : "继承强化 · 暂无同部位来源", jade, hasSource,
                hasSource ? "免费继承另一件同部位装备的强化；目标已有强化时，双方交换强化等级。" : "需要另一件同部位、已有强化的装备。")) OpenUpgradeTransfer(item);
        }

        private ItemData InventoryItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return session.Progression.Profile.inventory.Find(item => item != null && item.id == id);
        }

        private bool HasUpgradeTransferSource(ItemData target)
        {
            if (target == null) return false;
            return session.Progression.Profile.inventory.Exists(item => item != null && item.id != target.id && item.slot == target.slot && item.upgradeLevel > 0);
        }

        private void RebuildTransferSources(ItemData target)
        {
            transferSources.Clear();
            if (target != null)
                foreach (ItemData item in session.Progression.Profile.inventory)
                    if (item != null && item.id != target.id && item.slot == target.slot && item.upgradeLevel > 0) transferSources.Add(item);
            transferSources.Sort((a, b) =>
            {
                int comparison = IsEquipped(b).CompareTo(IsEquipped(a));
                if (comparison == 0) comparison = b.upgradeLevel.CompareTo(a.upgradeLevel);
                if (comparison == 0) comparison = ProgressionService.EquipmentScore(b).CompareTo(ProgressionService.EquipmentScore(a));
                return comparison != 0 ? comparison : string.CompareOrdinal(a.id, b.id);
            });
            if (!transferSources.Exists(item => item.id == transferSourceId))
                transferSourceId = transferSources.Count == 0 ? null : transferSources[0].id;
        }

        private void OpenUpgradeTransfer(ItemData target)
        {
            target = target == null ? null : InventoryItem(target.id);
            if (target == null || !HasUpgradeTransferSource(target))
            {
                session.Notify("暂无来源：需要另一件同部位、已有强化的装备。");
                return;
            }
            transferTargetId = target.id;
            transferSourceId = null;
            transferScroll = Vector2.zero;
            RebuildTransferSources(target);
            panel = Panel.UpgradeTransfer;
            session.SetUIBlocking(true);
        }

        private void ConfirmUpgradeTransfer()
        {
            ItemData target = InventoryItem(transferTargetId);
            ItemData source = InventoryItem(transferSourceId);
            if (target == null || source == null || source.id == target.id || source.slot != target.slot || source.upgradeLevel <= 0)
            {
                session.Notify("来源或目标已不可用，请重新选择同部位强化装备。");
                return;
            }
            if (source.upgradeLevel == target.upgradeLevel)
            {
                session.Notify("两件装备的强化等级相同，无需交换。");
                return;
            }
            int incomingRank = source.upgradeLevel;
            bool success = session.Progression.TransferUpgrade(source.id, target.id);
            Feedback(success, target.name + "已继承强化 +" + incomingRank + " · 免费，双方装备保留");
            if (!success) return;
            selectedItem = target.id;
            ReturnToInventory();
        }

        private void ReturnToInventory()
        {
            panel = Panel.Inventory;
            session.SetUIBlocking(true);
            RebuildBagItems();
            ResolveSelectedItem();
            inventoryScroll.y = Mathf.Clamp(inventoryScroll.y, 0, Mathf.Max(0, bagItems.Count * 76 + 4 - 330));
        }

        private void DrawUpgradeTransfer()
        {
            ItemData target = InventoryItem(transferTargetId);
            if (target == null)
            {
                ReturnToInventory();
                session.Notify("目标装备已不存在，请重新选择。");
                return;
            }
            RebuildTransferSources(target);
            Rect w = Modal(1160, 534, "继承强化", "");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ReturnToInventory();
            Text(new Rect(w.x + 24, w.y + 112, 280, 23), "选择强化来源", 16, jade, true);
            Text(new Rect(w.x + 320, w.y + 112, 732, 23), "强化预览", 15, jade, true);
            IconButton(new Rect(w.x + 1102, w.y + 107, 34, 30), "help", "", "强化继承规则\n免费，仅限同部位。目标未强化时完整转移；目标已有强化时，双方交换等级，不叠加或复制。\n两件装备保留，转移不自动换装。穿戴属性立即更新，生命只会受新上限限制，不会回复。\n可选择穿戴或背包中的来源；注意目标装备等级要求。", muted);
            Rect viewport = new Rect(w.x + 24, w.y + 149, 280, 299);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 264, Mathf.Max(297, transferSources.Count * 69 + 5));
            transferScroll.y = Mathf.Clamp(transferScroll.y, 0, Mathf.Max(0, content.height - viewport.height));
            GUIStyle previousThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            transferScroll = GUI.BeginScrollView(viewport, transferScroll, content, false, true, GUIStyle.none, scrollBar);
            for (int index = 0; index < transferSources.Count; index++)
            {
                ItemData item = transferSources[index];
                Rect row = new Rect(4, 4 + index * 69, 255, 62);
                bool selected = item.id == transferSourceId;
                Fill(row, selected ? new Color(.10f, .20f, .23f) : card);
                Border(row, selected ? gold : new Color(.18f, .30f, .36f));
                Text(new Rect(row.x + 11, row.y + 9, 234, 23), ItemTitle(item), 14, GameBalance.RarityColor(item.rarity), true);
                Text(new Rect(row.x + 11, row.y + 38, 232, 17), (IsEquipped(item) ? "当前穿戴" : "背包装备") + " · Lv." + item.level, 11, selected ? jade : muted);
                if (GUI.Button(row, GUIContent.none, invisibleButton)) transferSourceId = item.id;
            }
            GUI.EndScrollView();
            GUI.skin.verticalScrollbarThumb = previousThumb;
            ItemData source = InventoryItem(transferSourceId);
            ItemData sourcePreview = source == null ? null : session.Progression.PreviewUpgrade(source, target.upgradeLevel);
            ItemData targetPreview = source == null ? null : session.Progression.PreviewUpgrade(target, source.upgradeLevel);
            DrawTransferPreview(new Rect(w.x + 320, w.y + 149, 400, 284), "来源装备", source, sourcePreview, muted);
            DrawTransferPreview(new Rect(w.x + 736, w.y + 149, 400, 284), "目标装备", target, targetPreview, jade);
            if (Button(new Rect(w.x + 24, w.y + 467, 280, 42), "返回背包", jade)) ReturnToInventory();
            bool canTransfer = source != null && sourcePreview != null && targetPreview != null && source.upgradeLevel != target.upgradeLevel;
            string caption = source == null ? "请选择来源装备" : source.upgradeLevel == target.upgradeLevel ? "双方强化相同 · 无需交换" : target.upgradeLevel > 0 ? "确认交换强化 · 免费" : "确认继承强化 · 免费";
            if (Button(new Rect(w.x + 320, w.y + 467, 816, 42), caption, gold, canTransfer, null, canTransfer)) ConfirmUpgradeTransfer();
        }

        private void DrawTransferPreview(Rect r, string label, ItemData original, ItemData preview, Color accent)
        {
            Fill(r, card);
            Fill(new Rect(r.x, r.y, r.width, 3), accent);
            Text(new Rect(r.x + 18, r.y + 14, r.width - 36, 20), label, 13, accent, true);
            if (original == null)
            {
                Text(new Rect(r.x + 18, r.y + 105, r.width - 36, 48), "选择来源后查看强化与属性预览", 15, muted, false, true, TextAnchor.MiddleCenter);
                return;
            }
            bool levelLocked = original.level > session.Progression.Profile.level;
            Text(new Rect(r.x + 171, r.y + 15, 211, 18), "Lv." + original.level + (levelLocked ? " · 等级不足，暂不可穿戴" : IsEquipped(original) ? " · 当前穿戴" : " · 背包"), 11, levelLocked ? new Color(1, .61f, .47f) : muted, false, false, TextAnchor.MiddleRight);
            Text(new Rect(r.x + 18, r.y + 45, r.width - 36, 38), original.name, 23, GameBalance.RarityColor(original.rarity), true);
            Text(new Rect(r.x + 18, r.y + 89, r.width - 36, 35), "+" + original.upgradeLevel + "  →  " + (preview == null ? "—" : "+" + preview.upgradeLevel), 26, pale, true);
            Text(new Rect(r.x + 18, r.y + 137, 126, 17), "属性", 11, muted);
            Text(new Rect(r.x + 166, r.y + 137, 100, 17), "当前", 11, muted, false, false, TextAnchor.MiddleRight);
            Text(new Rect(r.x + 278, r.y + 137, 104, 17), "继承后", 11, jade, false, false, TextAnchor.MiddleRight);
            string[] labels = { "攻击", "防御", "生命" };
            int[] before = { original.attack, original.defense, original.health };
            int[] after = preview == null ? before : new[] { preview.attack, preview.defense, preview.health };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = r.y + 170 + i * 32;
                Text(new Rect(r.x + 18, y, 120, 24), labels[i], 14, muted);
                Text(new Rect(r.x + 158, y, 108, 24), before[i].ToString(), 17, pale, true, false, TextAnchor.MiddleRight);
                Text(new Rect(r.x + 278, y, 104, 24), preview == null ? "—" : after[i].ToString(), 17, after[i] >= before[i] ? jade : new Color(1, .59f, .47f), true, false, TextAnchor.MiddleRight);
            }
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

        private bool IsEquipmentUpgrade(ItemData item)
        {
            if (item == null || IsEquipped(item)) return false;
            ItemData current = session.Progression.Equipped(item.slot);
            if (current == null) return true;
            float score = ProgressionService.EquipmentScore(item);
            float currentScore = ProgressionService.EquipmentScore(current);
            return score > currentScore && !Mathf.Approximately(score, currentScore);
        }

        private string EquipmentUpgradeHint(ItemData item)
        {
            return (session.Progression.Equipped(item.slot) == null ? "此部位尚未穿戴装备。" : "综合评分高于当前同部位装备。") +
                (item.level > session.Progression.Profile.level ? "\n需要角色等级 " + item.level + "；目前等级不足。" : "");
        }

        private void DrawEquipmentUpgradeTag(Rect r)
        {
            Fill(r, new Color(.065f, .22f, .16f));
            Border(r, new Color(.3f, .85f, .54f, .45f));
            Text(r, "↑ 提升", 10, new Color(.57f, 1f, .67f), true, false, TextAnchor.MiddleCenter);
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
            Rect w = Modal(1160, 660, GameBalance.ClassName(p.heroClass) + " · 技能树", "");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 763, w.y + 28, 296, 32), "技能点 " + p.skillPoints + "   /   角色 Lv." + p.level, 18, gold, true, false, TextAnchor.MiddleRight);
            Rect branchHeading = new Rect(w.x + 24, w.y + 112, 267, 24);
            Text(branchHeading, "职业分支", 15, jade, true);
            if (branchHeading.Contains(Mouse)) tooltip = "沿分支从上到下学习，需先掌握前置技能。\n滚动查看高阶技能；每升一级获得 1 技能点。";
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
            DrawSkillDetail(new Rect(w.x + 550, w.y + 112, 586, GameBalance.IsPassive(selectedSkill) ? 393 : 510), selectedSkill);
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
            if (passive && new Rect(r.x + 18, r.y + 15, 550, 60).Contains(Mouse))
                tooltip = "被动技能学习后自动生效，不占主动技能槽。\n进阶继续强化效果，无需配置到快捷栏。";
            Text(new Rect(r.x + 18, r.y + 80, 550, 33), GameBalance.SkillDescription(p.heroClass, skill), 13, muted, false, true);
            string[] labels = { "当前冷却", "资源消耗", "初习解锁", "进阶成长" };
            string[] values = { passive ? "自动生效" : GameBalance.EffectiveCooldown(p.heroClass, skill, rank).ToString("0.#") + " 秒", passive ? "无需消耗" : GameBalance.SkillEnergyCost(p.heroClass, skill).ToString("0") + " " + GameBalance.EnergyName(p.heroClass), "Lv." + GameBalance.SkillRequiredLevels[skill], "强化 → 觉醒" };
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
                if (evolution.Contains(Mouse)) tooltip = GameBalance.SkillRankName(stage) + "：角色达到 " + GameBalance.SkillRankRequiredLevel(skill, stage) + " 级，消耗 1 技能点。\n" + evolutionText + (passive ? "\n学习后自动生效，无需配置到快捷栏。" : "\n冷却 " + GameBalance.EffectiveCooldown(p.heroClass, skill, stage).ToString("0.#") + " 秒。");
            }
            float actionX = r.x + 18;
            string reason = session.Progression.SkillLockReason(skill);
            bool canLearn = string.IsNullOrEmpty(reason);
            string caption = rank == 3 ? "已完全觉醒" : rank == 0 ? "学习初习 · 1 技能点" : "进阶" + GameBalance.SkillRankName(nextRank) + " · 1 技能点";
            if (Button(new Rect(actionX, r.y + 341, 236, 39), caption, gold, canLearn, reason, canLearn))
                Feedback(session.Progression.LearnSkill(skill), GameBalance.SkillName(p.heroClass, skill) + "已达到" + GameBalance.SkillRankName(p.skillRanks[skill]));
            Text(new Rect(r.x + 271, r.y + 341, 296, 40), rank == 3 ? "" : canLearn ? "下一阶段：" + GameBalance.SkillRankName(nextRank) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, nextRank) : reason, 12, muted, false, true);
            if (passive) return;
            Rect loadoutHeading = new Rect(actionX, r.y + 397, 365, 21);
            Text(loadoutHeading, "快捷栏 · 第 " + (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages + " 页", 12, jade, true);
            if (loadoutHeading.Contains(Mouse)) tooltip = "“+” 配置到槽位，“×” 卸下。\n升级保留快捷栏位置，三页共用技能冷却。";
            if (Button(new Rect(r.x + 436, r.y + 392, 58, 27), "‹ 页", jade, true, "上一页技能栏")) ChangePage(-1);
            if (Button(new Rect(r.x + 503, r.y + 392, 65, 27), "页 ›", jade, true, "下一页技能栏")) ChangePage(1);
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int equipped = LearnedSkillAtSlot(p, slot);
                bool current = equipped == skill;
                Rect target = new Rect(actionX + (slot % 5) * 112, r.y + 424 + (slot / 5) * 40, 102, 36);
                string key = GameBalance.KeyName(p.hotbarKeys[slot]);
                detailSlots[slot] = target;
                string hint = key + " · " + SlotSkillName(p, slot) + "\n" + (rank == 0 ? "先学习这项技能。" : current ? "点击从此槽卸下；不会清除技能冷却。" : "将" + GameBalance.SkillName(p.heroClass, skill) + "配置到此槽。") + "\n拖动已配置技能可移动或交换，拖到栏外取消。";
                Fill(target, current ? new Color(.13f, .2f, .2f) : new Color(.035f, .075f, .11f));
                Border(target, current || hotbarDragging && hotbarPointerConfiguring && (slot == hotbarPointerSlot || target.Contains(Mouse)) ? gold : new Color(jade.r, jade.g, jade.b, .4f));
                if (target.Contains(Mouse)) tooltip = hint;
                Text(new Rect(target.x + 6, target.y + 1, 70, 13), key, 10, pale, true);
                Text(new Rect(target.xMax - 17, target.y + 1, 12, 13), current ? "×" : "+", 11, current ? gold : jade, true);
                DrawSlotIdentity(new Rect(target.x + 6, target.y + 14, target.width - 12, 21), p, slot, equipped < 0 ? muted : pale);
            }
            HandleHotbarPointer(detailSlots, true);
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
            Rect w = Modal(840, 500, "自定义快捷键", "三页共用 10 个按键 · 点击槽位，然后按下新的按键");
            if (Button(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Rect pageHeading = new Rect(w.x + 27, w.y + 114, 620, 25);
            Text(pageHeading, "技能栏 · 第 " + (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages + " 页", 15, jade, true);
            if (pageHeading.Contains(Mouse)) tooltip = "可绑定字母、数字与 F1～F12。已占用的按键会交换位置。\n移动、界面和翻页按键保留。三页共用按键，技能配置独立。";
            if (Button(new Rect(w.x + 674, w.y + 110, 61, 28), "‹ 页", jade, rebindingSlot < 0)) ChangePage(-1);
            if (Button(new Rect(w.x + 748, w.y + 110, 68, 28), "页 ›", jade, rebindingSlot < 0)) ChangePage(1);
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect tile = new Rect(w.x + 24 + (i % 5) * 161, w.y + 157 + (i / 5) * 96, 148, 84);
                bool waiting = rebindingSlot == i;
                Fill(tile, waiting ? new Color(.2f, .18f, .12f) : card);
                Border(tile, waiting ? gold : jade * new Color(1, 1, 1, .3f));
                Text(new Rect(tile.x + 8, tile.y + 8, 132, 30), waiting ? "按键…" : GameBalance.KeyName(p.hotbarKeys[i]), waiting ? 21 : 25, waiting ? gold : pale, true, false, TextAnchor.MiddleCenter);
                DrawSlotIdentity(new Rect(tile.x + 17, tile.y + 46, 117, 26), p, i, pale);
                if (tile.Contains(Mouse)) tooltip = SlotSkillName(p, i) + " · " + GameBalance.KeyName(p.hotbarKeys[i]) + "\n点击后按新按键；Esc 取消。\n此设置同步三页快捷栏。";
                if (GUI.Button(tile, GUIContent.none, invisibleButton)) { rebindingSlot = i; GameAudio.Play(SoundCue.UI); }
            }
            if (rebindingSlot >= 0) Text(new Rect(w.x + 27, w.y + 360, 787, 27), "等待 " + GameBalance.KeyName(p.hotbarKeys[rebindingSlot]) + " 槽的新按键…  /  Esc 取消", 14, gold, true);
            if (Button(new Rect(w.x + 24, w.y + 437, 350, 39), "恢复默认 ZXCVB / 12345", gold, rebindingSlot < 0))
            {
                bool restored = true;
                for (int i = 0; i < GameBalance.HotbarSize; i++)
                    if (!session.Progression.SetHotbarKey(i, GameBalance.DefaultHotbarKeys[i])) { restored = false; break; }
                Feedback(restored, "已恢复默认技能按键");
            }
            if (Button(new Rect(w.x + 397, w.y + 437, 419, 39), bindingReturnPause ? "返回暂停菜单" : bindingReturnPanel == Panel.Controls ? "返回操作指南" : "返回技能研习", jade)) ClosePanel();
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
            if (Button(new Rect(w.x + 40, w.y + 350, 188, 37), GameAudio.Muted ? "声音：已静音" : "声音：已开启", jade))
            {
                GameAudio.Muted = !GameAudio.Muted;
                if (!GameAudio.Muted) GameAudio.Play(SoundCue.UI);
            }
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
            Text(new Rect(keyboard.x + 23, keyboard.y + 167, 598, 42), "左键 / J 普攻；自身、治疗、护盾及朝向技能按键后直接施放。\n地面技能先选点，再左键确认；右键 / Esc 取消。", 12, pale, false, true);
            Rule(keyboard.x + 20, keyboard.y + 222, 609, jade);
            Text(new Rect(keyboard.x + 22, keyboard.y + 236, 438, 22), "技能快捷键 · 第 " + (p.hotbarPage + 1) + " / " + GameBalance.HotbarPages + " 页", 16, jade, true);
            if (Button(new Rect(keyboard.x + 493, keyboard.y + 234, 60, 26), "‹ 页", jade)) ChangePage(-1);
            if (Button(new Rect(keyboard.x + 562, keyboard.y + 234, 65, 26), "页 ›", jade)) ChangePage(1);
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect keycap = new Rect(keyboard.x + 23 + (i % 5) * 123, keyboard.y + 273 + (i / 5) * 59, 112, 50);
                Fill(keycap, new Color(.08f, .13f, .18f));
                Border(keycap, new Color(jade.r, jade.g, jade.b, .4f));
                Text(new Rect(keycap.x + 7, keycap.y + 3, 98, 20), GameBalance.KeyName(p.hotbarKeys[i]), 16, pale, true);
                DrawSlotIdentity(new Rect(keycap.x + 7, keycap.y + 24, 98, 23), p, i, muted);
            }
            Text(new Rect(keyboard.x + 23, keyboard.y + 397, 604, 29), "快捷栏：点击施法，拖动换位；拖到栏外取消。右键打开技能配置。", 12, muted, false, true);
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
            Text(new Rect(w.x + 36, w.y + 289, 436, 41), PlatformText("留意地面攻击预警，按空格闪避。\n升级装备、学习技能后，再去挑战更强的敌人。"), 12, muted, false, true, TextAnchor.MiddleCenter);
        }

        private void DrawNotification()
        {
            if (string.IsNullOrEmpty(session.Notification)) return;
            bool overlay = panel != Panel.None || session.Paused || session.IsDead;
            Rect r = new Rect((width - 550) * .5f, overlay ? height - 49 : 26, 550, 40);
            Box(r, gold);
            Text(new Rect(r.x + 14, r.y + 3, r.width - 28, 34), PlatformText(session.Notification), 14, pale, true, true, TextAnchor.MiddleCenter);
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
            if (panel == Panel.UpgradeTransfer)
            {
                ReturnToInventory();
                return;
            }
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
