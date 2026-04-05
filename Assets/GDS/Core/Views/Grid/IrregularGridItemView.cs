using UnityEngine.UIElements;

namespace GDS.Core {
    public class IrregularGridItemView : BaseGridItemView {

        protected VisualElement shapeContainer = Dom.Div("absolute");

        public IrregularGridItemView() { this.Add("item-view", shapeContainer, image, quant); }

        override public void Render() {

            if (item == null) { return; }
            image.sprite = item.Icon;
            quant.text = item.StackSize.ToString();
            quant.SetVisible(item.Stackable);

            this.SetSize(Item.Size(), CellSize);

            if (Item is ShapeItem shapeItem) {
                image.SetSize(shapeItem.BaseSize, CellSize);
                image.Rotate((float)shapeItem.Direction);
                image.Translate(GridMath.AdjustPosForSizeAndDir(shapeItem.BaseSize, shapeItem.Direction), CellSize);
            } else {
                image.SetSize(Item.Size(), CellSize);
                image.Rotate(0);
                image.Translate(0, 0);
            }

            shapeContainer.Clear();
            shapeContainer.Add(new ShapeView(Item.Shape(), CellSize));
        }
    }
}