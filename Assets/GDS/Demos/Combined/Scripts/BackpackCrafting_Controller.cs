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

        enum ScreenMode { HUD, Inventory, Equipment, Settings }

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
        VisualElement _equipmentUI;
        VisualElement _settingsUI;
        VisualElement _hudBar;

        ScreenMode _currentScreen = ScreenMode.HUD;
        bool _initialized = false;

        // LiberateUI controllers for Settings panel.
        TabbedMenuController  _tabbedMenuController;
        KeyBindingController  _keyBindingController;

        /// <summary>Sets up the UI toggle infrastructure only. Bag wiring happens in Initialize().</summary>
        void Awake() {
            _root        = GetComponent<UIDocument>().rootVisualElement;
            _inventoryUI = _root.Q<VisualElement>("InventoryUI");
            _equipmentUI = _root.Q<VisualElement>("EquipmentUI");
            _settingsUI  = _root.Q<VisualElement>("SettingsUI");
            _hudBar      = _root.Q<VisualElement>("HUDBar");

            // HUD bar buttons.
            _root.Q<Button>("InventoryBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Inventory));
            _root.Q<Button>("EquipmentBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Equipment));
            _root.Q<Button>("SettingsBtn").RegisterCallback<ClickEvent>(_ => ShowScreen(ScreenMode.Settings));

            // Inventory close buttons.
            WireCloseButton("CloseBackpackBtn",  "BackpackWindow");
            WireCloseButton("CloseStorageBtn",   "StorageWindow");
            WireCloseButton("CloseCenterBtn",    "CenterWindow");
            WireCloseButton("CloseShopBtn",      "ShopWindow");
            WireCloseButton("CloseCraftingBtn",  "CraftingWindow");

            // Inventory window drag.
            AttachDragManipulator("BackpackHeader",  "BackpackWindow");
            AttachDragManipulator("StorageHeader",   "StorageWindow");
            AttachDragManipulator("CenterHeader",    "CenterWindow");
            AttachDragManipulator("ShopHeader",      "ShopWindow");
            AttachDragManipulator("CraftingHeader",  "CraftingWindow");

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

            // Gold display + reset button.
            var goldLabel = _root.Q<Label>("PlayerGoldText");
            _root.Q<Button>("ResetPlayerGoldButton").RegisterCallback<ClickEvent>(_ => _resetGold?.Invoke());
            _root.Observe(_playerGold, value => {
                goldLabel.text = value.ToString();
                goldLabel.TriggerClassAnimation("scale-150");
            });

            _initialized = true;
        }

        /// <summary>Keyboard shortcuts: I = Inventory, E = Equipment, Escape = close current screen.</summary>
        void Update() {
            if (!_initialized) return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
                ShowScreen(_currentScreen == ScreenMode.Inventory ? ScreenMode.HUD : ScreenMode.Inventory);
            else if (Keyboard.current.eKey.wasPressedThisFrame)
                ShowScreen(_currentScreen == ScreenMode.Equipment ? ScreenMode.HUD : ScreenMode.Equipment);
            else if (Keyboard.current.escapeKey.wasPressedThisFrame && _currentScreen != ScreenMode.HUD)
                ShowScreen(ScreenMode.HUD);
        }

        void ShowScreen(ScreenMode mode) {
            // If toggling the same screen, go back to HUD.
            if (_currentScreen == mode && mode != ScreenMode.HUD) {
                mode = ScreenMode.HUD;
            }

            // Cancel key binding listening when leaving settings.
            if (_currentScreen == ScreenMode.Settings && mode != ScreenMode.Settings)
                _keyBindingController?.CancelListening();

            _currentScreen = mode;
            SyncVisibility();

            if (mode == ScreenMode.Inventory) RestoreAllPanels();
        }

        void SyncVisibility() {
            _hudBar.style.display      = DisplayStyle.Flex;
            _inventoryUI.style.display = _currentScreen == ScreenMode.Inventory  ? DisplayStyle.Flex : DisplayStyle.None;

            if (_equipmentUI != null)
                _equipmentUI.style.display = _currentScreen == ScreenMode.Equipment ? DisplayStyle.Flex : DisplayStyle.None;
            if (_settingsUI != null)
                _settingsUI.style.display  = _currentScreen == ScreenMode.Settings  ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RestoreAllPanels() {
            foreach (var name in new[] { "BackpackWindow", "StorageWindow", "CenterWindow", "ShopWindow", "CraftingWindow" })
                SetPanelVisible(name, true);
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
