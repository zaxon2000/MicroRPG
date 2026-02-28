using UnityEngine;
using UnityEngine.UIElements;
using GDS.Core;

namespace GDS.Demos.Backpack {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif

    public partial class ShopSlotView : VisualElement, IItemContext, IHoveredItemContext {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ShopSlotView> { }
#endif

        public ShopSlotView() {
            Clear();
            ClearClassList();

            var uxml = Resources.Load<VisualTreeAsset>("Backpack_ShopSlotView");
            uxml.CloneTree(this);

            PriceLabel = this.Q<Label>(nameof(PriceLabel));
            ItemContainer = this.Q<VisualElement>(nameof(ItemContainer));
            PriceContainer = this.Q<VisualElement>(nameof(PriceContainer));
        }

        public Bag Bag { get; set; }
        public Slot Slot { get; set; }
        public Item Item => Slot?.Item;
        public Rect WorldBound => ItemImage.worldBound;

        Label PriceLabel;
        VisualElement ItemContainer;
        VisualElement PriceContainer;
        VisualElement ItemImage = new();
        int CellSize;

        public void Init(Bag bag, Slot slot, int cellSize) {
            Bag = bag;
            Slot = slot;
            CellSize = cellSize;
        }

        public void Render() {
            EnableInClassList("empty", Slot.Empty());
            if (Slot.Empty()) return;
            if (Item is not Backpack_Item item) return;

            ItemContainer.Clear();
            ItemContainer.Add(new ShapeView(item.Shape, CellSize).SetSize(item.Size, CellSize).WithClass("absolute shop-item-shape").PickIgnore());
            ItemImage = new Image { sprite = item.Icon }.SetSize(item.Size, CellSize).WithClass("absolute").PickIgnore();
            ItemContainer.Add(ItemImage);
            PriceLabel.text = item.Cost().ToString();
        }

    }
}
