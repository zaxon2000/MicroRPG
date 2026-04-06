using UnityEngine.UIElements;

namespace LiberateUI
{
    /// <summary>
    /// Inventory screen view - overlay screen that shows inventory items
    /// </summary>
    public class InventoryView : UIView
    {
        public InventoryView(VisualElement topElement) : base(topElement)
        {
            m_IsOverlay = true;
        }

        protected override void SetVisualElements()
        {
            // Inventory-specific visual elements setup
            // The actual inventory controller (InventoryController.cs) handles the item grid
        }

        protected override void RegisterButtonCallbacks()
        {
            // Register any inventory-specific button callbacks here if needed
        }

        public override void Dispose()
        {
            // Cleanup any event subscriptions
            base.Dispose();
        }
    }
}