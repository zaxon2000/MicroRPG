using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Demos.Backpack;
using GDS.Examples;
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

        // ── Bag & observable references — set by PlayerInventory.Start() via Initialize() ──────
        PlayerInventoryStore  _invStore;
        BackpackBag           _backpack;
        Storage               _storage;
        BackpackShop          _shop;
        BasicCraftingBench    _craftingBench;
        Observable<int>       _playerGold;
        Observable<bool>      _craftingActive;
        System.Action         _resetGold;

        VisualElement _root;
        VisualElement _inventoryUI;
        VisualElement _hudBar;
        bool _uiVisible = false;
        bool _initialized = false;

        /// <summary>Sets up the UI toggle infrastructure only. Bag wiring happens in Initialize().</summary>
        void Awake() {
            _root        = GetComponent<UIDocument>().rootVisualElement;
            _inventoryUI = _root.Q<VisualElement>("InventoryUI");
            _hudBar      = _root.Q<VisualElement>("HUDBar");

            _root.Q<Button>("InventoryBtn").RegisterCallback<ClickEvent>(_ => ToggleUI());

            // Close buttons.
            WireCloseButton("CloseBackpackBtn", "BackpackWindow");
            WireCloseButton("CloseStorageBtn",  "StorageWindow");
            WireCloseButton("CloseCenterBtn",   "CenterWindow");
            WireCloseButton("CloseRightBtn",    "RightWindow");

            // Window drag.
            AttachDragManipulator("BackpackHeader", "BackpackWindow");
            AttachDragManipulator("StorageHeader",  "StorageWindow");
            AttachDragManipulator("CenterHeader",   "CenterWindow");
            AttachDragManipulator("RightHeader",    "RightWindow");

            SyncUIVisibility();
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
            Observable<bool>      craftingActive,
            System.Action         resetGold) {

            _invStore       = invStore;
            _backpack       = backpack;
            _storage        = storage;
            _shop           = shop;
            _craftingBench  = craftingBench;
            _playerGold     = playerGold;
            _craftingActive = craftingActive;
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
            storageView.Init(_storage, 10);

            // Shop / Craft tab buttons + swappable SidePanel.
            var sidePanel = _root.Q<VisualElement>("SidePanel");
            _root.Q<Button>("ShopTabBtn").RegisterCallback<ClickEvent>(_ => _craftingActive.SetValue(false));
            _root.Q<Button>("CraftTabBtn").RegisterCallback<ClickEvent>(_ => _craftingActive.SetValue(true));

            _root.Observe(_craftingActive, active => {
                sidePanel.Clear();
                if (active) BuildCraftingPanel(sidePanel, _craftingBench);
                else        BuildShopPanel(sidePanel);
            });

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

        /// <summary>Pressing I toggles the entire inventory UI open or closed.</summary>
        void Update() {
            if (_initialized && Keyboard.current.iKey.wasPressedThisFrame) ToggleUI();
        }

        void ToggleUI() {
            _uiVisible = !_uiVisible;
            SyncUIVisibility();
            if (_uiVisible) RestoreAllPanels();
        }

        void SyncUIVisibility() {
            _inventoryUI.style.display = _uiVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _hudBar.style.display      = _uiVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void RestoreAllPanels() {
            foreach (var name in new[] { "BackpackWindow", "StorageWindow", "CenterWindow", "RightWindow" })
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
