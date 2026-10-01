using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Independent touch ownership keeps movement, combat and skill dragging separate.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class MobileControls : MonoBehaviour
    {
        private enum Role { Move, Attack, Skill, Aim, Consumed }
        private readonly Dictionary<int, Role> fingers = new Dictionary<int, Role>();
        private readonly List<int> staleFingers = new List<int>();
        private static MobileControls instance;
        public static bool SimulationEnabled { get; set; }
        public static bool Active
        {
            get
            {
#if UNITY_IOS || UNITY_ANDROID
                return true;
#else
                return SimulationEnabled;
#endif
            }
        }
        public static Vector2 Move { get; private set; }
        public static bool AttackHeld { get; private set; }
        private static bool dodge, potion;
        private int moveFinger = -1000;
        private GameSession session;
        private GameUI ui;
        private Texture2D disc;
        public static bool ConsumeDodge() { bool value = dodge; dodge = false; return Active && value; }
        public static bool ConsumePotion() { bool value = potion; potion = false; return Active && value; }
        public static Rect SafeArea { get { return Active && Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height); } }
        private float Scale { get { Rect safe = SafeArea; return Mathf.Max(.3f, Mathf.Min(safe.width / 1280f, safe.height / 720f)); } }
        private Vector2 Size { get { return SafeArea.size / Scale; } }
        private Vector2 Offset { get { Rect safe = SafeArea; return new Vector2(safe.x, Screen.height - safe.yMax); } }
        private Rect Joystick { get { return new Rect(36, Size.y - 205, 158, 158); } }
        private Rect Attack { get { return new Rect(Size.x - 183, Size.y - 210, 108, 108); } }
        private Rect Dodge { get { return new Rect(Size.x - 295, Size.y - 265, 78, 78); } }
        private Rect Potion { get { return new Rect(Size.x - 148, Size.y - 338, 76, 76); } }
        private Rect Cancel { get { return new Rect(Size.x - 291, Size.y - 353, 78, 58); } }

        public void Initialize(GameSession owner) { instance = this; session = owner; ui = owner.GetComponent<GameUI>(); ResetInput(); }
        public static void ResetInput()
        {
            Move = Vector2.zero; AttackHeld = dodge = potion = false;
            if (instance != null) { instance.fingers.Clear(); instance.moveFinger = -1000; }
        }
        private Vector2 ToUI(Vector2 screen) { return (new Vector2(screen.x, Screen.height - screen.y) - Offset) / Scale; }
        public Vector2 ControlScreenPoint(string name)
        {
            Rect control = name == "move" ? Joystick : name == "dodge" ? Dodge : name == "potion" ? Potion : name == "cancel" ? Cancel : Attack;
            Vector2 point = control.center * Scale + Offset;
            return new Vector2(point.x, Screen.height - point.y);
        }
        public static bool IsScreenPointOverControls(Vector2 screen)
        {
            if (!Active || instance == null || instance.session == null || instance.session.InputBlocked) return false;
            Vector2 point = instance.ToUI(screen);
            return instance.Joystick.Contains(point) || instance.Attack.Contains(point) || instance.Dodge.Contains(point) || instance.Potion.Contains(point) || instance.Cancel.Contains(point);
        }
        private void Update()
        {
            if (!Active || session == null || !session.HasStarted) { ResetInput(); return; }
            if (session.InputBlocked)
            {
                Move = Vector2.zero; AttackHeld = dodge = potion = false; moveFinger = -1000;
                staleFingers.Clear();
                foreach (KeyValuePair<int, Role> finger in fingers) if (finger.Value != Role.Skill) staleFingers.Add(finger.Key);
                foreach (int finger in staleFingers) fingers.Remove(finger);
            }
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                ProcessPointer(touch.fingerId, touch.phase, touch.position);
            }
#if UNITY_EDITOR
            if (SimulationEnabled && Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) ProcessPointer(-2, TouchPhase.Began, Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) ProcessPointer(-2, TouchPhase.Ended, Input.mousePosition);
                else if (Input.GetMouseButton(0)) ProcessPointer(-2, TouchPhase.Moved, Input.mousePosition);
            }
#endif
        }
        public bool ProcessPointer(int finger, TouchPhase phase, Vector2 screen)
        {
            if (!Active || session == null || !session.HasStarted) return false;
            Vector2 point = ToUI(screen);
            SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
            Role role;
            bool ended = phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            if (phase == TouchPhase.Began)
            {
                if (ui != null && ui.TryBeginTouchSkill(finger, screen)) role = Role.Skill;
                else if (session.InputBlocked) return false;
                else if (Joystick.Contains(point) && moveFinger == -1000) { role = Role.Move; moveFinger = finger; }
                else if (Attack.Contains(point))
                {
                    if (targeting != null && targeting.IsTargeting) { targeting.Confirm(); role = Role.Consumed; }
                    else role = Role.Attack;
                }
                else if (Dodge.Contains(point)) { dodge = true; role = Role.Consumed; }
                else if (Potion.Contains(point)) { potion = true; role = Role.Consumed; }
                else if (Cancel.Contains(point))
                {
                    if (targeting != null) targeting.Cancel();
                    SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
                    if (charge != null) charge.Cancel();
                    role = Role.Consumed;
                }
                else if (targeting != null && targeting.IsTargeting && (ui == null || !ui.IsScreenPointOverUI(screen))) role = Role.Aim;
                else return false;
                fingers[finger] = role;
            }
            else if (!fingers.TryGetValue(finger, out role)) return false;
            if (role == Role.Skill) ui.UpdateTouchSkill(finger, screen, ended, phase == TouchPhase.Canceled);
            else if (role == Role.Move)
            {
                Vector2 delta = (point - Joystick.center) / 60f;
                Move = ended || session.InputBlocked ? Vector2.zero : Vector2.ClampMagnitude(new Vector2(delta.x, -delta.y), 1);
                if (ended) moveFinger = -1000;
            }
            else if (role == Role.Aim && targeting != null && targeting.IsTargeting)
            {
                Ray ray = Camera.main.ScreenPointToRay(screen);
                float distance;
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out distance)) targeting.SetTarget(ray.GetPoint(distance));
                if (ended) { if (phase == TouchPhase.Canceled) targeting.Cancel(); else targeting.Confirm(); }
            }
            if (ended) fingers.Remove(finger);
            AttackHeld = !session.InputBlocked && fingers.ContainsValue(Role.Attack);
            return true;
        }
        private void OnGUI()
        {
            if (!Active || session == null || session.InputBlocked) return;
            if (disc == null)
            {
                disc = new Texture2D(96, 96, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color[96 * 96];
                for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++) pixels[y * 96 + x] = new Color(1, 1, 1, Mathf.Clamp01(47 - Vector2.Distance(new Vector2(x, y), new Vector2(47.5f, 47.5f))));
                disc.SetPixels(pixels); disc.Apply();
            }
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Offset, Quaternion.identity, new Vector3(Scale, Scale, 1));
            Circle(Joystick, new Color(.10f, .19f, .23f, .72f), "");
            Rect thumb = new Rect(Joystick.center.x - 28 + Move.x * 49, Joystick.center.y - 28 - Move.y * 49, 56, 56);
            Circle(thumb, new Color(.38f, .78f, .71f, .82f), "");
            SkillTargetingController targeting = session.Player.GetComponent<SkillTargetingController>();
            SkillChargeController charge = session.Player.GetComponent<SkillChargeController>();
            Circle(Attack, AttackHeld ? new Color(.76f, .54f, .20f, .95f) : new Color(.43f, .31f, .15f, .9f), targeting != null && targeting.IsTargeting ? "confirm" : "attack");
            Circle(Dodge, new Color(.13f, .32f, .38f, .9f), "dodge");
            Circle(Potion, new Color(.18f, .38f, .27f, .9f), "potion");
            if (targeting != null && targeting.IsTargeting || charge != null && charge.IsCharging) Circle(Cancel, new Color(.38f, .17f, .20f, .9f), "cancel");
            GUI.matrix = oldMatrix; GUI.color = oldColor;
        }
        private void Circle(Rect rect, Color color, string icon)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, disc);
            GUI.color = Color.white;
            if (string.IsNullOrEmpty(icon)) return;
            float size = Mathf.Min(rect.width, rect.height) * .52f;
            Rect centered = new Rect(rect.center.x - size * .5f, rect.center.y - size * .5f, size, size);
            // Texture glyphs cannot inherit a temporary GUIContent string from
            // another MonoBehaviour's OnGUI (for example the notification toast).
            GUI.DrawTexture(centered, UIIconAtlas.Utility(icon), ScaleMode.ScaleToFit, true);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) ResetInput(); }
        private void OnDisable() { ResetInput(); }
        private void OnDestroy() { if (disc != null) Destroy(disc); if (instance == this) { ResetInput(); instance = null; } }
    }
}
