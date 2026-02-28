using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GDS.Core;
using GDS.Core.Events;
using GDS.Common.Events;

namespace GDS.Demos.Arpg {
    [RequireComponent(typeof(UIDocument))]
    public class Arpg_Controller : MonoBehaviour {

        [Required]
        public Arpg_Store Store;

        [Space(16)]
        public PlayerInventory PlayerInventory;

        void Awake() {
            Store.Init(PlayerInventory);

            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GridItemView()));
            root.AddManipulator(new HighlightSlotManipulator(Store));
            root.AddManipulator(new TooltipManipulator(new ArpgTooltipView()));

            var left = root.Q<VisualElement>("Left");
            var right = root.Q<VisualElement>("Right");

            var equipmentView = root.Q<EquipmentView>("EquipmentView");
            equipmentView.Init(PlayerInventory.Equipment);

            var inventoryView = root.Q<GridBagView>("InventoryView");
            inventoryView.Init(PlayerInventory.Inventory, Store.Ghost, false);

            var playerGoldLabel = root.Q<Label>("PlayerGoldLabel");
            var inventoryCloseButton = root.Q<VisualElement>("InventoryWindow").Q<Button>("CloseButton");
            inventoryCloseButton.clicked += () => Store.Bus.Publish(new CloseUI());

            root.Observe(Store.UiOpen, value => root.SetVisible(value));
            root.Observe(Store.SideWindow, value => {
                left.Clear();
                if (value == null) return;
                left.Add(CreateSideWindowView(value, Store));
            });
            root.Observe(Store.PlayerGold, value => {
                playerGoldLabel.text = value.ToString();
            });

            var backdrop = root.Q<VisualElement>("Backdrop");
            backdrop.RegisterCallback<PointerUpEvent>(e => {
                if (Store.Ghost.Value == null) return;
                Store.Bus.Publish(new DropWorldItem(Store.Ghost.Value));
            });
        }

        //  Creates a side window 
        VisualElement CreateSideWindowView(object handle, Arpg_Store store) => handle switch {
            CharacterSheet b => new CharacterSheetWindow(b, store),
            Stash b => new StashWindow(b, store),
            Shop b => new ShopWindow(b, store),
            _ => Dom.Div()
        };

        // These methods are called by the PlayerInput component
        public void OnCloseUi() => Store.Bus.Publish(new CloseUI());
        public void OnToggleInventory() => Store.Bus.Publish(new ToggleUI());
        public void OnToggleCharacterSheet() => Store.ToggleCharacterSheet();
    }

}