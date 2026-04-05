using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Examples {
    public class MinimalGrid_Controller : MonoBehaviour {
        [Required]
        public Store Store;
        public GridBag bag = new() { Size = new(10, 5), CellSize = 100 };

        void OnEnable() {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.AddManipulator(new DragDropManipulator(Store, new GhostItemWithRotation() { CellSize = bag.CellSize }));
            root.AddManipulator(new RotateGhostManipulator(Store));
            root.AddManipulator(new TooltipManipulator());

            var gridBagView = root.Q<GridBagView>();
            gridBagView.CreateItemView = () => new IrregularGridItemView();
            gridBagView.Init(bag, Store.Ghost);
        }
    }
}