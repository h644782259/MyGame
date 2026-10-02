namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool CloseMobileInventoryDetail()
        {
            // Match the visible compact equipment detail, not a stale flag on a
            // tablet, supply page, or another modal. Preserve both scroll anchors.
            if(!MobileControls.Active || panel!=Panel.Inventory || session.Paused ||
                MobileCollectionLayout.SideBySideInventory(MobileControls.Layout.Width) ||
                mobileInventoryTab==2 || !mobileInventoryDetail)return false;
            mobileInventoryDetail=false;
            CancelMobileScroll();BlockUITransition();return true;
        }
    }
}
