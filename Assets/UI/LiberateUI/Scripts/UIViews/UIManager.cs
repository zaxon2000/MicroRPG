using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiberateUI
{
    /// <summary>
    /// High-level manager for the LiberateUI menu system.
    /// Handles routing between inventory and settings screens.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        private UIDocument m_MainMenuDocument;

        private UIView m_CurrentView;

        private List<UIView> m_AllViews = new List<UIView>();
        private List<UIView> m_OverlayViews = new List<UIView>();

        // Overlay screens
        private UIView m_InventoryView;
        private UIView m_SettingsView;
        private UIView m_CharacterPreviewView;

        // VisualTree element names
        private const string k_InventoryViewName = "inventory-instance";
        private const string k_SettingsViewName = "settings-instance";
        private const string k_CharacterPreviewViewName = "character-preview-instance";

        // Menu buttons
        private Button m_InventoryButton;
        private Button m_EquipmentButton;
        private Button m_WeaponsButton;
        private Button m_SkillsButton;
        private Button m_SettingsButton;

        private void OnEnable()
        {
            m_MainMenuDocument = GetComponent<UIDocument>();

            SetupViews();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();

            foreach (UIView view in m_AllViews)
            {
                view.Dispose();
            }
        }

        private void SetupViews()
        {
            VisualElement root = m_MainMenuDocument.rootVisualElement;

            // Create overlay views
            m_InventoryView = new InventoryView(root.Q<VisualElement>(k_InventoryViewName));
            m_SettingsView = new SettingsView(root.Q<VisualElement>(k_SettingsViewName));
            m_CharacterPreviewView = new CharacterPreviewView(root.Q<VisualElement>(k_CharacterPreviewViewName));

            // Track overlay views separately for easy toggling
            m_OverlayViews.Add(m_InventoryView);
            m_OverlayViews.Add(m_SettingsView);
            m_OverlayViews.Add(m_CharacterPreviewView);

            // Track all views for disposal
            m_AllViews.AddRange(m_OverlayViews);

            // Setup menu button callbacks
            SetupMenuButtons(root);
        }

        private void SetupMenuButtons(VisualElement root)
        {
            m_InventoryButton = root.Q<Button>("inventory-button");
            m_EquipmentButton = root.Q<Button>("equipment-button");
            m_WeaponsButton = root.Q<Button>("weapons-button");
            m_SkillsButton = root.Q<Button>("skills-button");
            m_SettingsButton = root.Q<Button>("settings-button");

            if (m_InventoryButton != null)
                m_InventoryButton.RegisterCallback<ClickEvent>(evt => OnInventoryButtonClicked());

            if (m_EquipmentButton != null)
                m_EquipmentButton.RegisterCallback<ClickEvent>(evt => OnEquipmentButtonClicked());

            if (m_WeaponsButton != null)
                m_WeaponsButton.RegisterCallback<ClickEvent>(evt => OnWeaponsButtonClicked());

            if (m_SkillsButton != null)
                m_SkillsButton.RegisterCallback<ClickEvent>(evt => OnSkillsButtonClicked());

            if (m_SettingsButton != null)
                m_SettingsButton.RegisterCallback<ClickEvent>(evt => OnSettingsButtonClicked());
        }

        private void SubscribeToEvents()
        {
            MainMenuUIEvents.InventoryScreenShown += OnInventoryScreenShown;
            MainMenuUIEvents.InventoryScreenHidden += OnInventoryScreenHidden;
            MainMenuUIEvents.SettingsScreenShown += OnSettingsScreenShown;
            MainMenuUIEvents.SettingsScreenHidden += OnSettingsScreenHidden;
        }

        private void UnsubscribeFromEvents()
        {
            MainMenuUIEvents.InventoryScreenShown -= OnInventoryScreenShown;
            MainMenuUIEvents.InventoryScreenHidden -= OnInventoryScreenHidden;
            MainMenuUIEvents.SettingsScreenShown -= OnSettingsScreenShown;
            MainMenuUIEvents.SettingsScreenHidden -= OnSettingsScreenHidden;
        }

        // Button click handlers - toggle behavior
        private void OnInventoryButtonClicked()
        {
            ToggleOverlayView(m_InventoryView);
        }

        private void OnEquipmentButtonClicked()
        {
            ToggleOverlayView(m_CharacterPreviewView);
        }

        private void OnWeaponsButtonClicked()
        {
            Debug.Log("Weapons button clicked - not implemented yet");
        }

        private void OnSkillsButtonClicked()
        {
            Debug.Log("Skills button clicked - not implemented yet");
        }

        private void OnSettingsButtonClicked()
        {
            ToggleOverlayView(m_SettingsView);
        }

        /// <summary>
        /// Generic toggle method for any overlay view
        /// </summary>
        private void ToggleOverlayView(UIView targetView)
        {
            if (targetView == null) return;

            // If clicking the currently open view, close it
            if (m_CurrentView == targetView && !targetView.IsHidden)
            {
                targetView.Hide();
                m_CurrentView = null;
                return;
            }

            // Close all other overlays and open the target
            HideAllOverlays();
            targetView.Show();
            m_CurrentView = targetView;
        }

        /// <summary>
        /// Hides all overlay views
        /// </summary>
        private void HideAllOverlays()
        {
            foreach (UIView overlay in m_OverlayViews)
            {
                if (!overlay.IsHidden)
                {
                    overlay.Hide();
                }
            }
            m_CurrentView = null;
        }

        // Overlay screen event handlers (for external triggers)
        private void OnInventoryScreenShown()
        {
            ToggleOverlayView(m_InventoryView);
        }

        private void OnInventoryScreenHidden()
        {
            if (!m_InventoryView.IsHidden)
            {
                m_InventoryView.Hide();
                m_CurrentView = null;
            }
        }

        private void OnSettingsScreenShown()
        {
            ToggleOverlayView(m_SettingsView);
        }

        private void OnSettingsScreenHidden()
        {
            if (!m_SettingsView.IsHidden)
            {
                m_SettingsView.Hide();
                m_CurrentView = null;
            }
        }
    }
}