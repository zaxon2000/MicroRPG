using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Demos.Basic;
using GDS.Demos.Backpack;
using GDS.Examples;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using BasicRecipe = GDS.Demos.Basic.Recipe;
using BasicCraftingBench = GDS.Demos.Basic.CraftingBench;

namespace GDS.Demos.Combined {

    [RequireComponent(typeof(UIDocument))]
    public class BackpackCrafting_Controller : MonoBehaviour {

        [Required, InlineEditor]
        public BackpackCrafting_Store Store;

        VisualElement _root;
        VisualElement _inventoryUI;
        VisualElement _hudBar;
        bool _uiVisible = false;

        void Awake() {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _inventoryUI = _root.Q<VisualElement>("InventoryUI");
            _hudBar = _root.Q<VisualElement>("HUDBar");

            // Manipulators sit on the full root so drag continues outside panel bounds.
            _root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = 80 }));
            _root.AddManipulator(new RotateGhostManipulator(Store));
            _root.AddManipulator(new TooltipManipulator(new BackpackTooltipView()));

            // HUD inventory button opens the UI.
            _root.Q<Button>("InventoryBtn").RegisterCallback<ClickEvent>(_ => ToggleUI());

            // Backpack tetris grid (left panel, top).
            var backpackView = _root.Q<GridBagView>("BackpackView");
            backpackView.CreateItemView = () => new IrregularGridItemView();
            backpackView.Init(Store.Backpack, Store.Ghost);

            // Storage flat grid (left panel, bottom).
            var storageView = _root.Q<ListBagView>("StorageView");
            storageView.Init(Store.Storage, 10);

            // Right panel: tab buttons + swappable SidePanel.
            var sidePanel = _root.Q<VisualElement>("SidePanel");
            _root.Q<Button>("ShopTabBtn").RegisterCallback<ClickEvent>(_ => Store.CraftingActive.SetValue(false));
            _root.Q<Button>("CraftTabBtn").RegisterCallback<ClickEvent>(_ => Store.CraftingActive.SetValue(true));

            _root.Observe(Store.CraftingActive, active => {
                sidePanel.Clear();
                if (active) BuildCraftingPanel(sidePanel, Store.CraftingBench);
                else BuildShopPanel(sidePanel);
            });

            // Per-panel close buttons — each hides only its own section.
            WireCloseButton("CloseBackpackBtn", "Backpack");
            WireCloseButton("CloseStorageBtn",  "Storage");
            WireCloseButton("CloseCenterBtn",   "Center");
            WireCloseButton("CloseRightBtn",    "Right");

            // Sell drop zone (right panel, bottom).
            var piggyIcon = _root.Q<VisualElement>("PiggyIcon");
            _root.Q<VisualElement>("SellArea").RegisterCallback<PointerUpEvent>(_ => {
                if (Store.Ghost.Value == null) return;
                Store.Bus.Publish(new SellCurrenItem());
                piggyIcon.TriggerClassAnimation("scale-150");
            });

            // Gold display + reset button.
            var goldLabel = _root.Q<Label>("PlayerGoldText");
            _root.Q<Button>("ResetPlayerGoldButton").RegisterCallback<ClickEvent>(_ => Store.ResetPlayerGold());
            _root.Observe(Store.PlayerGold, value => {
                goldLabel.text = value.ToString();
                goldLabel.TriggerClassAnimation("scale-150");
            });

            // Apply the initial closed state set by _uiVisible = false.
            SyncUIVisibility();
        }

        /// <summary>Pressing I toggles the entire inventory UI open or closed.</summary>
        void Update() {
            if (Keyboard.current.iKey.wasPressedThisFrame) ToggleUI();
        }

        void ToggleUI() {
            _uiVisible = !_uiVisible;
            SyncUIVisibility();
            if (_uiVisible) RestoreAllPanels();
        }

        /// <summary>Syncs InventoryUI and HUDBar visibility to the current _uiVisible state.</summary>
        void SyncUIVisibility() {
            _inventoryUI.style.display = _uiVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _hudBar.style.display      = _uiVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Resets all four panel sections to visible. Called whenever the inventory is reopened.</summary>
        void RestoreAllPanels() {
            foreach (var name in new[] { "Backpack", "Storage", "Center", "Right" })
                SetPanelVisible(name, true);
        }

        void WireCloseButton(string buttonName, string panelName) =>
            _root.Q<Button>(buttonName)?.RegisterCallback<ClickEvent>(_ => SetPanelVisible(panelName, false));

        void SetPanelVisible(string panelName, bool visible) {
            var panel = _root.Q<VisualElement>(panelName);
            if (panel != null)
                panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Populates the side panel with the shop view.</summary>
        void BuildShopPanel(VisualElement container) {
            var shopView = new ShopView();
            shopView.Init(Store.Shop, Store);
            container.Add(shopView);
        }

        /// <summary>Populates the side panel with the crafting bench, wiring ingredient and outcome slots.</summary>
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
