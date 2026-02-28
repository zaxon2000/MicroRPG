using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Demos.Basic;
using GDS.Demos.Backpack;
using GDS.Examples;
using UnityEngine;
using UnityEngine.UIElements;
using BasicRecipe = GDS.Demos.Basic.Recipe;
using BasicCraftingBench = GDS.Demos.Basic.CraftingBench;

namespace GDS.Demos.Combined {

    [RequireComponent(typeof(UIDocument))]
    public class BackpackCrafting_Controller : MonoBehaviour {

        [Required, InlineEditor]
        public BackpackCrafting_Store Store;

        void Awake() {
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Tetris-style drag with rotation support, same settings as the Backpack demo.
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = 80 }));
            root.AddManipulator(new RotateGhostManipulator(Store));
            root.AddManipulator(new TooltipManipulator(new BackpackTooltipView()));

            // Backpack tetris grid (left panel, top).
            var backpackView = root.Q<GridBagView>("BackpackView");
            backpackView.CreateItemView = () => new IrregularGridItemView();
            backpackView.Init(Store.Backpack, Store.Ghost);

            // Storage flat grid (left panel, bottom).
            var storageView = root.Q<ListBagView>("StorageView");
            storageView.Init(Store.Storage, 10);

            // Right panel: tab buttons + swappable SidePanel.
            var sidePanel = root.Q<VisualElement>("SidePanel");
            var shopTabBtn = root.Q<Button>("ShopTabBtn");
            var craftTabBtn = root.Q<Button>("CraftTabBtn");

            shopTabBtn.RegisterCallback<ClickEvent>(_ => Store.CraftingActive.SetValue(false));
            craftTabBtn.RegisterCallback<ClickEvent>(_ => Store.CraftingActive.SetValue(true));

            // Rebuild the SidePanel whenever the active tab changes.
            root.Observe(Store.CraftingActive, active => {
                sidePanel.Clear();
                if (active) BuildCraftingPanel(sidePanel, Store.CraftingBench);
                else BuildShopPanel(sidePanel);
            });

            // Sell drop zone (right panel, bottom).
            var piggyIcon = root.Q<VisualElement>("PiggyIcon");
            var sellArea = root.Q<VisualElement>("SellArea");
            sellArea.RegisterCallback<PointerUpEvent>(_ => {
                if (Store.Ghost.Value == null) return;
                Store.Bus.Publish(new SellCurrenItem());
                piggyIcon.TriggerClassAnimation("scale-150");
            });

            // Gold display + reset button (center / character sheet area).
            var goldLabel = root.Q<Label>("PlayerGoldText");
            var resetBtn = root.Q<Button>("ResetPlayerGoldButton");
            resetBtn.RegisterCallback<ClickEvent>(_ => Store.ResetPlayerGold());
            root.Observe(Store.PlayerGold, value => {
                goldLabel.text = value.ToString();
                goldLabel.TriggerClassAnimation("scale-150");
            });
        }

        /// <summary>Populates the side panel with the shop view.</summary>
        void BuildShopPanel(VisualElement container) {
            var shopView = new ShopView();
            shopView.Init(Store.Shop, Store);
            container.Add(shopView);
        }

        /// <summary>Populates the side panel with the crafting bench view, wiring up ingredient slots and the outcome slot.</summary>
        void BuildCraftingPanel(VisualElement container, BasicCraftingBench bench) {
            var vta = Resources.Load<VisualTreeAsset>("CraftingBenchView");
            if (vta == null) { Debug.LogError("[BackpackCrafting] CraftingBenchView not found in Resources."); return; }

            container.Add(vta.Instantiate());
            container.Add(BuildRecipeList(bench.Recipes));

            var outcomeSlot = container.Q<SlotView>("OutcomeSlot");
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
