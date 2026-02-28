using UnityEngine;
using UnityEngine.UIElements;
using GDS.Common.Events;
using GDS.Core;

namespace GDS.Demos.Dayz {
    [RequireComponent(typeof(UIDocument))]
    public class Dayz_Controller : MonoBehaviour {
        [Required, InlineEditor]
        public Dayz_Store Store;

        [Space(12)]
        public bool ShowDebugLabels = true;

        [Space(12)]
        public Equipment Equipment = new();
        public Hands Hands = new();
        public Hotbar Hotbar = new();

        bool uiOpen = true;
        VisualElement root;

        void OnValidate() {
            if (root == null) return;
            root.EnableInClassList("show-debug-labels", ShowDebugLabels);
        }

        void Awake() {
            Store.Init(Equipment, Hands, Hotbar);

            root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation()));
            root.AddManipulator(new HighlightSlotManipulator(Store));
            root.AddManipulator(new RotateGhostManipulator(Store));

            root.EnableInClassList("show-debug-labels", ShowDebugLabels);

            var equipmentView = root.Q<EquipmentView>();
            equipmentView.Init(Equipment, Store);

            var hotbarView = root.Q<ListBagView>("HotbarView");
            hotbarView.Init(Store.Hotbar);

            var handsView = root.Q<HandsView>("HandsView");
            handsView.Init(Hands, Store);

            var backdrop = root.Q<VisualElement>("Backdrop");
            backdrop.RegisterCallback<PointerUpEvent>(e => {
                if (e.button != 0) return;
                if (Store.Ghost.Value == null) return;
                Store.Bus.Publish(new DropWorldItem(Store.Ghost.Value));
            });
        }

        // These methods are called by the PlayerInput component
        public void OnCloseUi() {
            uiOpen = false;
            root.SetVisible(uiOpen);
        }
        public void OnToggleInventory() {
            uiOpen = !uiOpen;
            root.SetVisible(uiOpen);
        }
    }

}