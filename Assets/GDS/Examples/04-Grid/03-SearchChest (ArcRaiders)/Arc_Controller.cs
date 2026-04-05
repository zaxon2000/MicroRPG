using GDS.Core;
using GDS.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {

    public class Arc_Controller : MonoBehaviour {
        [Required]
        public Arc_Store Store;

        [Space(16)]
        public Arc_Chest gridBag;

        VisualElement Left;

        void OnEnable() {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation()));
            root.AddManipulator(new RotateGhostManipulator(Store));
            root.Hide();

            var gridBagView = root.Q<GridBagView>();
            gridBagView.CreateItemView = () => new Arc_GridItemView();
            gridBagView.Init(gridBag, Store.Ghost);

            Left = root.Q<VisualElement>("Left");
            root.Observe(Store.UiOpen, value => {
                root.SetVisible(value);
            });

            root.Observe(Store.SideWindow, OpenWindow);
        }

        void OnDestroy() {
            // Dispose of async tasks
            if (Store.SideWindow.Value is Arc_Chest bag) bag.Dispose();
        }

        void OpenWindow(object handle) {

            Left.Clear();
            if (handle is Arc_Chest b) {
                Left.Add(new Arc_ChestWindowView(b, Store));
            }
        }

        // Called by Player Input script
        void OnCloseUI() { Store.Bus.Publish(new CloseUI()); }
        void OnToggleUI() { Store.Bus.Publish(new ToggleUI()); }

    }

}