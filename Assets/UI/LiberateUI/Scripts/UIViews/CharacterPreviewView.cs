using UnityEngine.UIElements;

namespace LiberateUI
{
    /// <summary>
    /// Character preview screen view - overlay screen that shows character equipment and stats
    /// </summary>
    public class CharacterPreviewView : UIView
    {
        public CharacterPreviewView(VisualElement topElement) : base(topElement)
        {
            m_IsOverlay = true;
        }

        protected override void SetVisualElements()
        {
            // Character preview-specific visual elements setup
            // Future: Set up item slots, character model display, stats display
        }

        protected override void RegisterButtonCallbacks()
        {
            // Register any character preview-specific button callbacks here if needed
        }

        public override void Dispose()
        {
            // Cleanup any event subscriptions
            base.Dispose();
        }
    }
}
