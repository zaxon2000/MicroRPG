using UnityEngine.UIElements;

namespace LiberateUI
{
    /// <summary>
    /// Settings screen view - overlay screen with tabbed interface for game settings
    /// </summary>
    public class SettingsView : UIView
    {
        private TabbedMenuController m_TabbedMenuController;
        private KeyBindingController m_KeyBindingController;
        private Button m_BackButton;
        private DropdownField m_ArtStyleDropdown;

        public SettingsView(VisualElement topElement) : base(topElement)
        {
            m_IsOverlay = true;
        }

        protected override void SetVisualElements()
        {
            // m_TopElement IS the settings screen (from the template instance)
            // Initialize tabbed menu for Graphics/Audio/Controls/Gameplay tabs
            m_TabbedMenuController = new TabbedMenuController(m_TopElement);
            m_TabbedMenuController.RegisterTabCallbacks();
            m_TabbedMenuController.SelectFirstTab();

            // Initialize key binding controller
            m_KeyBindingController = new KeyBindingController(m_TopElement);
            m_KeyBindingController.RegisterKeyBindingCallbacks();

            // Get UI elements
            m_BackButton = m_TopElement.Q<Button>("back-button");
            m_ArtStyleDropdown = m_TopElement.Q<DropdownField>(className:"art-style-dropdown");

            // Initialize art style dropdown with current theme's art style
            InitializeArtStyleDropdown();
        }

        protected override void RegisterButtonCallbacks()
        {
            if (m_BackButton != null)
            {
                m_BackButton.RegisterCallback<ClickEvent>(OnBackButtonClicked);
            }

            if (m_ArtStyleDropdown != null)
            {
                m_ArtStyleDropdown.RegisterValueChangedCallback(OnArtStyleChanged);
            }
        }

        private void OnBackButtonClicked(ClickEvent evt)
        {
            // Cancel any active key binding listening
            if (m_KeyBindingController != null)
            {
                m_KeyBindingController.CancelListening();
            }

            // Trigger hide event to return to previous screen
            MainMenuUIEvents.SettingsScreenHidden?.Invoke();
        }

        public override void Hide()
        {
            // Cancel any active key binding listening when hiding
            if (m_KeyBindingController != null)
            {
                m_KeyBindingController.CancelListening();
            }

            base.Hide();
        }

        public override void Dispose()
        {
            if (m_BackButton != null)
            {
                m_BackButton.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
            }

            if (m_ArtStyleDropdown != null)
            {
                m_ArtStyleDropdown.UnregisterValueChangedCallback(OnArtStyleChanged);
            }

            // Clean up key binding controller
            if (m_KeyBindingController != null)
            {
                m_KeyBindingController.CancelListening();
                m_KeyBindingController.UnregisterKeyBindingCallbacks();
            }

            base.Dispose();
        }

        private void InitializeArtStyleDropdown()
        {
            if (m_ArtStyleDropdown == null)
                return;

            // Get current art style from ThemeManager
            ThemeManager themeManager = UnityEngine.Object.FindFirstObjectByType<ThemeManager>();
            if (themeManager != null)
            {
                string currentArtStyle = themeManager.GetCurrentArtStyle();
                if (!string.IsNullOrEmpty(currentArtStyle))
                {
                    m_ArtStyleDropdown.value = currentArtStyle;
                }
            }
        }

        private void OnArtStyleChanged(ChangeEvent<string> evt)
        {
            // Notify ThemeManager of art style change
            ThemeEvents.ArtStyleChanged?.Invoke(evt.newValue);
        }
    }
}