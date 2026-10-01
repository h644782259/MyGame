using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class MobileValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            // Reuse the exact save-isolation boundary used by the inventory fixture.
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game });
            MobileControls controls = game.GetComponent<MobileControls>();
            check(controls != null, "Mobile controls are installed by runtime bootstrap");
            GameUI ui = game.GetComponent<GameUI>();
            GameProfile originalProfile = game.Progression.Profile;
            bool originalSimulation = MobileControls.SimulationEnabled;
            bool originalEnabled = game.Player.enabled;
            Vector3 originalPosition = game.Player.transform.position;
            Quaternion originalRotation = game.Player.transform.rotation;
            string originalError = game.Progression.LastError;
            object notification = Field(typeof(GameSession), "notification").GetValue(game);
            object notificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
            string save = game.Progression.SaveFilePath;
            string[] paths = { save, save + ".bak", save + ".tmp" };
            byte[][] bytes = new byte[paths.Length][];
            for (int i = 0; i < paths.Length; i++) bytes[i] = File.Exists(paths[i]) ? File.ReadAllBytes(paths[i]) : null;
            try
            {
                log?.Invoke("MOBILE INPUT — editor multi-touch simulation, real movement, skill drag and safe-area bounds");
                MobileControls.SimulationEnabled = true;
                MobileControls.ResetInput();
                game.Player.enabled = false;
                game.SetPaused(false);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.12f)) yield return tick;
                Vector2 move = controls.ControlScreenPoint("move");
                Vector2 attack = controls.ControlScreenPoint("attack");
                Vector2 dodge = controls.ControlScreenPoint("dodge");
                Vector2 potion = controls.ControlScreenPoint("potion");
                Rect safe = MobileControls.SafeArea;
                check(safe.Contains(move) && safe.Contains(attack) && safe.Contains(dodge) && safe.Contains(potion), "Touch control centers stay inside the screen safe area");
                check(controls.ProcessPointer(11, TouchPhase.Began, move), "Left joystick captures its own finger");
                controls.ProcessPointer(11, TouchPhase.Moved, move + Vector2.right * 200);
                controls.ProcessPointer(12, TouchPhase.Began, attack);
                check(MobileControls.Move.x > .9f && MobileControls.AttackHeld, "Two fingers can move and hold attack simultaneously");
                controls.ProcessPointer(12, TouchPhase.Ended, attack);
                check(!MobileControls.AttackHeld && MobileControls.Move.x > .9f, "Releasing attack preserves the independent movement finger");
                controls.ProcessPointer(11, TouchPhase.Canceled, move);
                check(MobileControls.Move == Vector2.zero, "Cancelled movement clears the joystick without a stuck direction");
                controls.ProcessPointer(21, TouchPhase.Began, dodge);
                check(MobileControls.ConsumeDodge() && !MobileControls.ConsumeDodge(), "One dodge touch is consumed exactly once");
                controls.ProcessPointer(21, TouchPhase.Ended, dodge);
                controls.ProcessPointer(22, TouchPhase.Began, potion);
                check(MobileControls.ConsumePotion() && !MobileControls.ConsumePotion(), "One potion touch is consumed exactly once");
                controls.ProcessPointer(22, TouchPhase.Ended, potion);
                controls.ProcessPointer(23, TouchPhase.Began, attack);
                controls.ProcessPointer(24, TouchPhase.Began, dodge);
                game.SetPaused(true);
                Call(controls, "Update");
                check(!MobileControls.AttackHeld && MobileControls.Move == Vector2.zero && !MobileControls.ConsumeDodge(), "Blocking gameplay clears held and queued touch commands");
                game.SetPaused(false);
                controls.ProcessPointer(23, TouchPhase.Stationary, attack);
                check(!MobileControls.AttackHeld, "A stale pre-pause finger cannot restart attack after resume");

                Vector3 beforeMove = game.Player.transform.position;
                controls.ProcessPointer(31, TouchPhase.Began, move);
                controls.ProcessPointer(31, TouchPhase.Moved, move + Vector2.right * 200);
                game.Player.enabled = true;
                foreach (object tick in Wait(.16f)) yield return tick;
                game.Player.enabled = false;
                controls.ProcessPointer(31, TouchPhase.Ended, move);
                check(game.Player.transform.position.x > beforeMove.x + .15f, "PlayerController actually moves from the virtual joystick");

                GameProfile testProfile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile));
                testProfile.level = 40;
                testProfile.hotbarPage = 0;
                for (int i = 0; i < testProfile.equippedSkills.Length; i++) testProfile.equippedSkills[i] = -1;
                testProfile.skillRanks[0] = testProfile.skillRanks[1] = 1;
                testProfile.equippedSkills[0] = 0;
                testProfile.equippedSkills[1] = 1;
                Set(game.Progression, "Profile", testProfile);
                // Batch mode need not repaint Game View. Use the same layout
                // method as runtime Update, touch input and OnGUI, not test rectangles.
                Call(ui, "RefreshLayout");
                Rect[] slots = (Rect[])Field(typeof(GameUI), "hotbarSlots").GetValue(ui);
                float scale = (float)Field(typeof(GameUI), "scale").GetValue(ui);
                Vector2 offset = (Vector2)Field(typeof(GameUI), "guiOffset").GetValue(ui);
                Func<Vector2, Vector2> screen = point => new Vector2(point.x * scale + offset.x, Screen.height - point.y * scale - offset.y);
                check(slots.Length == 10 && slots[0].width == 64 && slots[9].height == 61, "Mobile HUD retains ten enlarged touch skill slots");
                Rect bounds = (Rect)Field(typeof(GameUI), "hotbarBounds").GetValue(ui);
                for (int i = 0; i < slots.Length; i++)
                {
                    check(slots[i].width == 64 && slots[i].height == 61 && bounds.Contains(slots[i].min) && bounds.Contains(slots[i].max - Vector2.one * .01f), "Mobile skill slot " + i + " uses the rendered layout inside its HUD bounds");
                    for (int j = i + 1; j < slots.Length; j++) check(!slots[i].Overlaps(slots[j]), "Mobile skill slots " + i + " and " + j + " do not overlap");
                }
                Vector2 first = screen(slots[0].center), second = screen(slots[1].center);
                check(controls.ProcessPointer(41, TouchPhase.Began, first), "Skill touch is captured separately from attack and movement");
                controls.ProcessPointer(41, TouchPhase.Moved, second);
                controls.ProcessPointer(41, TouchPhase.Ended, second);
                check(game.Progression.Profile.equippedSkills[0] == 1 && game.Progression.Profile.equippedSkills[1] == 0, "Touch dragging swaps the actual learned skill slots and saves the page");
                controls.ProcessPointer(42, TouchPhase.Began, second);
                controls.ProcessPointer(42, TouchPhase.Moved, Vector2.zero);
                controls.ProcessPointer(42, TouchPhase.Ended, Vector2.zero);
                check(game.Progression.Profile.equippedSkills[1] == 0 && !MobileControls.AttackHeld, "Dropping a skill outside the hotbar cancels without starting basic attack");
                MobileControls.SimulationEnabled = false;
                MobileControls.ResetInput();
#if !UNITY_IOS && !UNITY_ANDROID
                check(!MobileControls.Active && MobileControls.Move == Vector2.zero && !MobileControls.AttackHeld, "Desktop mode leaves virtual input inactive after simulation");
#endif
            }
            finally
            {
                MobileControls.ResetInput();
                MobileControls.SimulationEnabled = originalSimulation;
                Call(ui, "CancelHotbarPointer");
                Set(game.Progression, "Profile", originalProfile);
                Set(game.Progression, "LastError", originalError);
                game.SetPaused(false);
                game.SetUIBlocking(false);
                game.Player.Teleport(originalPosition);
                game.Player.transform.rotation = originalRotation;
                game.Player.RefreshStats(false);
                game.Player.enabled = originalEnabled;
                Field(typeof(GameSession), "notification").SetValue(game, notification);
                Field(typeof(GameSession), "notificationUntil").SetValue(game, notificationUntil);
                for (int i = 0; i < paths.Length; i++)
                {
                    if (bytes[i] != null) File.WriteAllBytes(paths[i], bytes[i]);
                    else if (File.Exists(paths[i])) File.Delete(paths[i]);
                }
            }
        }
        private static IEnumerable<object> Wait(float seconds)
        {
            double until = Time.timeAsDouble + seconds;
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (Time.timeAsDouble < until)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Mobile input simulation received no runtime updates.");
                yield return null;
            }
        }
        private static FieldInfo Field(Type type, string name) { return type.GetField(name, Hidden); }
        private static void Call(object owner, string name) { owner.GetType().GetMethod(name, Hidden).Invoke(owner, null); }
        private static void Set(object owner, string name, object value) { owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value }); }
    }
}
