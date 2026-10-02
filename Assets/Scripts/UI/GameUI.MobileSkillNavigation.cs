using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool MobileSkillRowClicked(Rect row)
        { return GUI.enabled && touchScrollSuppressed != Time.frameCount && GUI.Button(row, GUIContent.none, invisibleButton); }
        private bool CloseMobileSkillDetail()
        {
            if (!MobileControls.Active || panel != Panel.Skills || SkillIconPresentation.SideBySide(MobileControls.Layout.Width) || !mobileSkillDetail) return false;
            mobileSkillDetail = false; CancelMobileScroll(); BlockUITransition(); return true;
        }
        private void ResetMobileSkillNavigation() { mobileSkillDetail = false; CancelMobileScroll(); }
    }
}
