using System.Collections;
using System.Collections.Generic;
using GDS.Core;
using GDS.Examples;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Demos.Backpack {

    [RequireComponent(typeof(UIDocument))]
    public class Backpack_Controller : MonoBehaviour {
        [Required, InlineEditor]
        public Backpack_Store Store;

        void Awake() {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = 80 }));
            root.AddManipulator(new RotateGhostManipulator(Store));
            root.AddManipulator(new TooltipManipulator(new BackpackTooltipView()));

            var backpackView = root.Q<GridBagView>("BackpackView");
            backpackView.CreateItemView = () => new IrregularGridItemView();
            backpackView.Init(Store.Backpack, Store.Ghost);

            var storageView = root.Q<ListBagView>("StorageView");
            storageView.Init(Store.Storage, 10);

            var shopView = root.Q<ShopView>("ShopView");
            shopView.Init(Store.Shop, Store);

            var resetPlayerGoldButton = root.Q<Button>("ResetPlayerGoldButton");
            resetPlayerGoldButton.RegisterCallback<ClickEvent>(_ => Store.ResetPlayerGold());

            var piggyIcon = root.Q<VisualElement>("PiggyIcon");

            var sellArea = root.Q<VisualElement>("SellArea");
            sellArea.RegisterCallback<PointerUpEvent>(_ => {
                if (Store.Ghost.Value == null) return;
                Store.Bus.Publish(new SellCurrenItem());
                piggyIcon.TriggerClassAnimation("scale-150");
            });

            var playerGoldText = root.Q<Label>("PlayerGoldText");
            root.Observe(Store.PlayerGold, value => {
                playerGoldText.text = value.ToString();
                playerGoldText.TriggerClassAnimation("scale-150");
            });
        }
    }
}
