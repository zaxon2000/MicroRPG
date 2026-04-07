using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Demos.Backpack;
using GDS.Examples;
using LiberateUI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using BasicRecipe        = GDS.Demos.Basic.Recipe;
using BasicCraftingBench = GDS.Demos.Basic.CraftingBench;
using BackpackBag        = GDS.Demos.Backpack.Backpack;
using BackpackShop       = GDS.Demos.Backpack.Shop;

namespace GDS.Demos.Combined {

    [RequireComponent(typeof(UIDocument))]
    public class BackpackCrafting_Controller : MonoBehaviour {

        enum ScreenMode { HUD, Inventory, Character, Settings }

        // ── Bag & observable references — set by PlayerInventory.Start() via Initialize() ──────
        PlayerInventoryStore  _invStore;
        BackpackBag           _backpack;
        Storage               _storage;
        BackpackShop          _shop;
        BasicCraftingBench    _craftingBench;
        Observable<int>       _playerGold;
        System.Action         _resetGold;

        VisualElement _root;
        VisualElement _inventoryUI;
        VisualElement _characterUI;
        VisualElement _settingsUI;
        VisualElement _hudBar;
        Label _coinCountLabel;

        ScreenMode _currentScreen = ScreenMode.HUD;
        bool _initialized = false;
        bool _characterVisible = false;
        bool _characterPositioned = false;

        // LiberateUI controllers for Settings panel.
        TabbedMenuController  _tabbedMenuController;
        KeyBindingController  _keyBindingController;

        /// <summary>Sets up the UI toggle infrastructure only. Bag wiring happens in Initialize().</summary>
        void Awake() {
            _root        = GetComponent<UIDocument>().rootVisualElement;
            _inventoryUI = _root.Q<VisualElement>("InventoryUI");
            _characterUI = _root.Q<VisualElement>("CharacterUI");
            _settingsUI  = _root.Q<VisualElement>("SettingsUI");
            _hudBar      = _root.Q<VisualElement>("HUDBar");

            // HUD bar buttons.
            _root.Q<Button>("InventoryBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Inventory));
            _root.Q<Button>("CharacterBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Character));
            _root.Q<Button>("SettingsBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Settings));

            // Inventory close buttons.
            WireCloseButton("CloseBackpackBtn",  "BackpackWindow");
            WireCloseButton("CloseStorageBtn",   "StorageWindow");
            WireCloseButton("CloseShopBtn",      "ShopWindow");
            WireCloseButton("CloseCraftingBtn",  "CraftingWindow");

            // Inventory window drag.
            AttachDragManipulator("BackpackHeader",  "BackpackWindow");
            AttachDragManipulator("StorageHeader",   "StorageWindow");
            AttachDragManipulator("ShopHeader",      "ShopWindow");
            AttachDragManipulator("CraftingHeader",  "CraftingWindow");

            // Coin count label in Character panel.
            _coinCountLabel = _root.Q<Label>("CoinCountLabel");

            // Character panel drag — grab anywhere on the panel to drag.
            if (_characterUI != null) {
                var charHandle = _characterUI.Q<VisualElement>("character-preview-root");
                if (charHandle != null)
                    charHandle.AddManipulator(new WindowDragManipulator(charHandle, _characterUI));
            }

            // Settings panel — tabbed menu & key binding.
            if (_settingsUI != null) {
                _tabbedMenuController = new TabbedMenuController(_settingsUI);
                _tabbedMenuController.RegisterTabCallbacks();
                _tabbedMenuController.SelectFirstTab();

                _keyBindingController = new KeyBindingController(_settingsUI);
                _keyBindingController.RegisterKeyBindingCallbacks();

                // Wire the Back action button to close settings.
                var backAction = _settingsUI.Q<VisualElement>("back-action");
                backAction?.Q<Button>("button")?.RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.HUD));
            }

            SyncVisibility();
        }

        /// <summary>
        /// Called by PlayerInventory.Start() to connect all inventory bags and observables to the UI.
        /// PlayerInventory owns all state; the controller is purely a view layer.
        /// </summary>
        public void Initialize(
            PlayerInventoryStore  invStore,
            BackpackBag           backpack,
            Storage               storage,
            BackpackShop          shop,
            BasicCraftingBench    craftingBench,
            Observable<int>       playerGold,
            System.Action         resetGold) {

            _invStore       = invStore;
            _backpack       = backpack;
            _storage        = storage;
            _shop           = shop;
            _craftingBench  = craftingBench;
            _playerGold     = playerGold;
            _resetGold      = resetGold;

            // Manipulators need the event bus and ghost observable.
            _root.AddManipulator(new DragDropManipulator(_invStore, new GhostItemWithRotation() { CellSize = 27 }));
            _root.AddManipulator(new RotateGhostManipulator(_invStore));
            _root.AddManipulator(new TooltipManipulator(new BackpackTooltipView()));

            // Backpack tetris grid.
            var backpackView = _root.Q<GridBagView>("BackpackView");
            backpackView.CreateItemView = () => new IrregularGridItemView();
            backpackView.Init(_backpack, _invStore.Ghost);

            // Storage flat grid.
            var storageView = _root.Q<ListBagView>("StorageView");
            storageView.Init(_storage, 9);

            // Shop panel (own window).
            var shopPanel = _root.Q<VisualElement>("ShopPanel");
            BuildShopPanel(shopPanel);

            // Crafting panel (own window).
            var craftingPanel = _root.Q<VisualElement>("CraftingPanel");
            BuildCraftingPanel(craftingPanel, _craftingBench);

            // Sell drop zone.
            var piggyIcon = _root.Q<VisualElement>("PiggyIcon");
            _root.Q<VisualElement>("SellArea").RegisterCallback<PointerUpEvent>(_ => {
                if (_invStore.Ghost.Value == null) return;
                _invStore.Bus.Publish(new SellCurrenItem());
                piggyIcon.TriggerClassAnimation("scale-150");
            });

            // Track coin count — update whenever backpack or storage changes.
            _backpack.CollectionChanged += UpdateCoinCount;
            _storage.CollectionChanged  += UpdateCoinCount;
            UpdateCoinCount();

            _initialized = true;
        }

        /// <summary>Keyboard shortcuts: I = Inventory, C = Character, Escape = close current screen.</summary>
        void Update() {
            if (!_initialized) return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
                ShowScreen(_currentScreen == ScreenMode.Inventory ? ScreenMode.HUD : ScreenMode.Inventory);
            else if (Keyboard.current.cKey.wasPressedThisFrame)
                ShowScreen(ScreenMode.Character);
            else if (Keyboard.current.escapeKey.wasPressedThisFrame) {
                if (_characterVisible) ToggleCharacterPanel();
                if (_currentScreen != ScreenMode.HUD) ShowScreen(ScreenMode.HUD);
            }
        }

        void ShowScreen(ScreenMode mode) {
            // Character panel toggles independently of other screens.
            if (mode == ScreenMode.Character) {
                ToggleCharacterPanel();
                return;
            }

            // If toggling the same screen, go back to HUD.
            if (_currentScreen == mode && mode != ScreenMode.HUD) {
                mode = ScreenMode.HUD;
            }

            // Cancel key binding listening when leaving Settings.
            if (_currentScreen == ScreenMode.Settings && mode != ScreenMode.Settings)
                _keyBindingController?.CancelListening();

            _currentScreen = mode;
            SyncVisibility();

            if (mode == ScreenMode.Inventory) RestoreAllPanels();
        }

        void ToggleCharacterPanel() {
            _characterVisible = !_characterVisible;
            if (_characterUI == null) return;

            _characterUI.style.display = _characterVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_characterVisible) {
                UpdateCoinCount();
                if (!_characterPositioned) {
                    _characterUI.RegisterCallback<GeometryChangedEvent>(PositionCharacterPanel);
                }
            }
        }

        void PositionCharacterPanel(GeometryChangedEvent evt) {
            _characterUI.UnregisterCallback<GeometryChangedEvent>(PositionCharacterPanel);
            var parent = _characterUI.parent;
            float parentWidth  = parent.resolvedStyle.width;
            float parentHeight = parent.resolvedStyle.height;
            float panelWidth   = _characterUI.resolvedStyle.width;
            float panelHeight  = _characterUI.resolvedStyle.height;
            _characterUI.style.left = parentWidth  - panelWidth  - 10;
            _characterUI.style.top  = parentHeight - panelHeight - 80;
            _characterPositioned = true;
        }

        void SyncVisibility() {
            _hudBar.style.display      = DisplayStyle.Flex;
            _inventoryUI.style.display = _currentScreen == ScreenMode.Inventory  ? DisplayStyle.Flex : DisplayStyle.None;

            // Character panel is toggled independently — not managed here.
            if (_settingsUI != null)
                _settingsUI.style.display  = _currentScreen == ScreenMode.Settings  ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RestoreAllPanels() {
            foreach (var name in new[] { "BackpackWindow", "StorageWindow", "ShopWindow", "CraftingWindow" })
                SetPanelVisible(name, true);
        }

        /// <summary>Counts all Coin items across backpack and storage, updating the Character panel label.</summary>
        void UpdateCoinCount() {
            if (_coinCountLabel == null) return;

            int total = 0;
            foreach (var item in _backpack.Items)
                if (item != null && string.Equals(item.Name, "Coin", System.StringComparison.OrdinalIgnoreCase))
                    total += item.StackSize;
            foreach (var item in _storage.Items)
                if (item != null && string.Equals(item.Name, "Coin", System.StringComparison.OrdinalIgnoreCase))
                    total += item.StackSize;

            _coinCountLabel.text = total.ToString();
        }

        void WireCloseButton(string buttonName, string windowName) =>
            _root.Q<Button>(buttonName)?.RegisterCallback<ClickEvent>(_ => SetPanelVisible(windowName, false));

        void AttachDragManipulator(string headerName, string windowName) {
            var header = _root.Q<VisualElement>(headerName);
            var window = _root.Q<VisualElement>(windowName);
            if (header != null && window != null)
                header.AddManipulator(new WindowDragManipulator(header, window));
        }

        void SetPanelVisible(string panelName, bool visible) {
            var panel = _root.Q<VisualElement>(panelName);
            if (panel != null)
                panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void BuildShopPanel(VisualElement container) {
            var shopView = new ShopView();
            shopView.Init(_shop, _invStore);
            container.Add(shopView);
        }

        void BuildCraftingPanel(VisualElement container, BasicCraftingBench bench) {
            var vta = Resources.Load<VisualTreeAsset>("CraftingBenchView");
            if (vta == null) { Debug.LogError("[BackpackCrafting] CraftingBenchView not found in Resources."); return; }

            container.Add(vta.Instantiate());
            container.Add(BuildRecipeList(bench.Recipes));

            var outcomeSlot    = container.Q<SlotView>("OutcomeSlot");
            var ingredientView = container.Q<ListBagView>("ListBagView");

            ingredientView.Init(bench);
            outcomeSlot.Init(bench, bench.OutcomeSlot.Value);
            container.Observe(bench.OutcomeSlot, _ => outcomeSlot.Render());
        }

        VisualElement BuildRecipeList(List<BasicRecipe> recipes) =>
            Dom.Div(recipes.Where(r => r != null).Select(BuildRecipeRow).ToArray());

        VisualElement BuildRecipeRow(BasicRecipe recipe) => Dom.Div("row mt-10",
            Dom.Div("row gap-h-5",
                recipe.Ingredients.Where(i => i != null)
                    .Select(i => Dom.Image("default-border", i.Icon)).ToArray()),
            new Label("￫") { style = { height = 32, fontSize = 24 } },
            Dom.Image("default-border ml-5", recipe.Outcome.Icon)
        );
    }
}
