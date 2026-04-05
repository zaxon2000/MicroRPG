using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {
    public class QuickSlot_Controller : MonoBehaviour {
        const int CellSize = 100;
        [Required]
        public QuickSlot_Store Store;

        public Observable<Item> Hands = new(null);
        public QuickSlot_Shortcuts Shortcuts = new() { Size = 4 };
        public QuickSlot_PlayerInventory PlayerInventory = new() { Size = new(4, 4), CellSize = CellSize };


        void OnEnable() {
            Store.Init(PlayerInventory, Shortcuts, Hands);

            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = CellSize }));
            root.AddManipulator(new RotateGhostManipulator(Store));
            root.AddManipulator(new HighlightSlotManipulator(Store));

            var playerInventoryView = root.Q<GridBagView>();
            playerInventoryView.CreateItemView = () => new QuickSlot_GridItemView() { CellSize = CellSize };
            playerInventoryView.Init(PlayerInventory, Store.Ghost, false);

            var shortcutsView = root.Q<ListBagView>().Init(Shortcuts, -1, "Shortcuts", () => new QuickSlot_SlotView());

            var handsView = root.Q<VisualElement>("Hands");
            var customItemView = new QuickSlot_ItemView();
            handsView.Add(customItemView);

            var useButton = root.Q<Button>("UseButton");
            useButton.RegisterCallback<ClickEvent>(_ => Store.OnUseCurrentItem());

            root.Observe(Hands, value => { customItemView.Item = value; });

        }

        // Input Actions
        void OnShortcut1() => Store.OnShortcutPressed(0);
        void OnShortcut2() => Store.OnShortcutPressed(1);
        void OnShortcut3() => Store.OnShortcutPressed(2);
        void OnShortcut4() => Store.OnShortcutPressed(3);
    }
}