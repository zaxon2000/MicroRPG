using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {

    [RequireComponent(typeof(UIDocument))]
    public class MoveItemsGrid_Controller : MonoBehaviour {
        [Required]
        public MoveItems_Store store;

        [Space(12)]
        public GridBag bagLeft = new() { Size = new(6, 6) };

        [Space(12)]
        public GridBag bagRight = new() { Size = new(6, 6) };

        void OnEnable() {
            store.Main = bagLeft;
            store.Secondary = bagRight;

            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(store));

            var leftBagView = root.Q<GridBagView>("Left");
            leftBagView.Init(bagLeft, store.Ghost);

            var rightBagView = root.Q<GridBagView>("Right");
            rightBagView.Init(bagRight, store.Ghost);
        }
    }

}