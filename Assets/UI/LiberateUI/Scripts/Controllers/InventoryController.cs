using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiberateUI
{
    [System.Serializable]
    public class ItemData
    {
        public string name;
        public Texture2D image;
        public int damage;
        public int armor;
        public string enchantment;
        public string description;

        public ItemData(string itemName, Texture2D itemImage)
        {
            name = itemName;
            image = itemImage;

            // Fallback stats based on item name
            damage = GetRandomStat(50, 150);
            armor = GetRandomStat(5, 25);
            enchantment = GetRandomEnchantment();
            description = GetLoremIpsumDescription();
        }

        private int GetRandomStat(int min, int max)
        {
            return Random.Range(min, max);
        }

        private string GetRandomEnchantment()
        {
            var enchantments = new[]
            {
                "Fire Resistance 15%",
                "Ice Damage +10",
                "Lightning Strike 20%",
                "Poison Immunity",
                "Critical Hit +5%",
                "Magic Shield 25%",
                "Vampire Drain 8%",
                "Empty Enchantment Slot"
            };
            return enchantments[Random.Range(0, enchantments.Length)];
        }

        private string GetLoremIpsumDescription()
        {
            return "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
        }
    }
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset itemCardTemplate;
        [SerializeField] private VisualTreeAsset itemPopUpTemplate;
        [SerializeField] private UIDocument inventoryDocument;
        [SerializeField] private ThemeManager themeManager;

        private VisualElement inventoryGrid;
        private VisualElement itemPopUp;
        private readonly List<string> excludedItems = new() { "inventory_container" };
        private Texture2D fallbackImage;

        private void Awake()
        {
            if (inventoryDocument == null)
                inventoryDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            InitializeInventory();
            ThemeEvents.ArtStyleChanged += OnArtStyleChanged;
        }

        private void OnDisable()
        {
            ThemeEvents.ArtStyleChanged -= OnArtStyleChanged;
        }

        private void InitializeInventory()
        {
            if (inventoryDocument?.rootVisualElement == null) return;

            inventoryGrid = inventoryDocument.rootVisualElement.Q<VisualElement>("inventory-grid");
            if (inventoryGrid == null)
            {
                Debug.LogError("Could not find inventory-grid element in InventoryContainer.uxml");
                return;
            }

            LoadFallbackImage();
            LoadItemCardTemplate();
            PopulateInventoryGrid();
        }

        private void LoadFallbackImage()
        {
            fallbackImage = null;
        }

        private void LoadItemCardTemplate()
        {
            if (itemCardTemplate == null)
                Debug.LogError("Could not load ItemCard template from Resources");
        }

        private void PopulateInventoryGrid()
        {
            if (itemCardTemplate == null) return;

            inventoryGrid.Clear();

            var itemTextures = LoadInventoryItemTextures();

            foreach (var (itemName, texture) in itemTextures)
            {
                CreateItemCard(itemName, texture);
            }
        }

        private List<(string name, Texture2D texture)> LoadInventoryItemTextures()
        {
            var items = new List<(string, Texture2D)>();
            string artStyle = GetCurrentArtStyle();
            string itemsPath = $"LiberateUI/{artStyle}/Items";
            var textures = Resources.LoadAll<Texture2D>(itemsPath);

            foreach (var texture in textures)
            {
                if (IsExcludedItem(texture.name)) continue;

                items.Add((texture.name, texture));
            }

            return items.OrderBy(item => item.Item1).ToList();
        }

        private bool IsExcludedItem(string itemName)
        {
            return excludedItems.Any(excluded => itemName.Contains(excluded));
        }

        private void CreateItemCard(string itemName, Texture2D itemTexture)
        {
            var itemCard = itemCardTemplate.Instantiate();

            var itemNameLabel = itemCard.Q<Label>("item-name");
            var itemImageElement = itemCard.Q<VisualElement>("item-image");

            if (itemNameLabel != null)
            {
                itemNameLabel.text = FormatItemName(itemName);
            }

            if (itemImageElement != null && itemTexture != null)
            {
                itemImageElement.style.backgroundImage = new StyleBackground(itemTexture);
            }

            // Add click handler to show popup
            itemCard.RegisterCallback<ClickEvent>(evt => ShowItemPopUp(itemName, itemTexture));

            inventoryGrid.Add(itemCard);
        }

        private string FormatItemName(string rawName)
        {
            return rawName.Replace("_", " ")
                         .Replace("-", " ")
                         .ToTitleCase();
        }

        private void ShowItemPopUp(string itemName, Texture2D itemTexture)
        {
            if (itemPopUpTemplate == null)
            {
                Debug.LogError("ItemPopUp template is not assigned");
                return;
            }

            // Remove existing popup if any
            HideItemPopUp();

            // Create and show popup
            itemPopUp = itemPopUpTemplate.Instantiate();
            itemPopUp.AddToClassList("screen__anchor--center");

            // Create item data with fallbacks
            var itemData = new ItemData(itemName, itemTexture ?? fallbackImage);

            // Update popup content
            UpdatePopUpContent(itemData);

            // Add popup to root visual element
            inventoryDocument.rootVisualElement.Add(itemPopUp);

            // Setup close handlers
            SetupPopUpCloseHandlers();
        }

        private void UpdatePopUpContent(ItemData itemData)
        {
            // Update title
            var titleLabel = itemPopUp.Q<Label>("item-title");
            if (titleLabel != null)
            {
                titleLabel.text = FormatItemName(itemData.name);
            }

            // Update popup item image (in preview section)
            var popupImageElement = itemPopUp.Q<VisualElement>("item-image");
            if (popupImageElement != null && itemData.image != null)
            {
                popupImageElement.style.backgroundImage = new StyleBackground(itemData.image);
            }

            // Update stats
            var damageLabel = itemPopUp.Q<Label>("damage-stat");
            if (damageLabel != null)
            {
                damageLabel.text = $"Damage: {itemData.damage}";
            }

            var armorLabel = itemPopUp.Q<Label>("armor-stat");
            if (armorLabel != null)
            {
                armorLabel.text = $"Armor: +{itemData.armor}";
            }

            var enchantmentLabel = itemPopUp.Q<Label>("enchantment-stat");
            if (enchantmentLabel != null)
            {
                enchantmentLabel.text = itemData.enchantment;
            }

            var enchantmentSlotLabel = itemPopUp.Q<Label>("enchantment-slot");
            if (enchantmentSlotLabel != null)
            {
                enchantmentSlotLabel.text = "Empty Enchantment Slot";
            }

            // Update description
            var descriptionLabel = itemPopUp.Q<Label>("description-text");
            if (descriptionLabel != null)
            {
                descriptionLabel.text = itemData.description;
            }
        }

        private void SetupPopUpCloseHandlers()
        {
            // Close on background click
            var popupBackground = itemPopUp.Q<VisualElement>("item-popup");
            if (popupBackground != null)
            {
                popupBackground.RegisterCallback<ClickEvent>(evt =>
                {
                    // Only close if clicking the background, not the content
                    if (evt.target == popupBackground)
                    {
                        HideItemPopUp();
                    }
                });
            }

            // Setup action buttons (they need to be instantiated from templates)
            SetupActionButtons();
        }

        private void SetupActionButtons()
        {
            // Find buttons within template instances
            var sellActionInstance = itemPopUp.Q<VisualElement>("sell-action");
            var equipActionInstance = itemPopUp.Q<VisualElement>("equip-action");
            var closeActionInstance = itemPopUp.Q<VisualElement>("close-action");

            // Get the button from each template instance
            var sellButton = sellActionInstance?.Q<Button>("button");
            var equipButton = equipActionInstance?.Q<Button>("button");
            var closeButton = closeActionInstance?.Q<Button>("button");

            if (sellButton != null)
            {
                sellButton.RegisterCallback<ClickEvent>(evt => OnSellButtonClicked());
            }

            if (equipButton != null)
            {
                equipButton.RegisterCallback<ClickEvent>(evt => OnEquipButtonClicked());
            }

            if (closeButton != null)
            {
                closeButton.RegisterCallback<ClickEvent>(evt => OnCloseButtonClicked());
            }
        }

        private void OnSellButtonClicked()
        {
            Debug.Log("Sell button clicked");
            HideItemPopUp();
        }

        private void OnEquipButtonClicked()
        {
            Debug.Log("Equip button clicked");
            HideItemPopUp();
        }

        private void OnCloseButtonClicked()
        {
            HideItemPopUp();
        }

        private void HideItemPopUp()
        {
            if (itemPopUp != null)
            {
                itemPopUp.RemoveFromHierarchy();
                itemPopUp = null;
            }
        }

        private void OnArtStyleChanged(string newArtStyle)
        {
            Debug.Log($"[InventoryController] Art style changed to: {newArtStyle}. Reloading inventory...");
            LoadFallbackImage();
            PopulateInventoryGrid();
        }

        private string GetCurrentArtStyle()
        {
            if (themeManager == null)
            {
                themeManager = FindFirstObjectByType<ThemeManager>();
            }

            if (themeManager != null)
            {
                return themeManager.GetCurrentArtStyle();
            }

            Debug.LogWarning("[InventoryController] ThemeManager not found, defaulting to DarkHandDrawnFantasy");
            return "DarkHandDrawnFantasy";
        }
    }
    public static class StringExtensions
    {
        public static string ToTitleCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) +
                            (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
                }
            }
            return string.Join(" ", words);
        }
    }
}
