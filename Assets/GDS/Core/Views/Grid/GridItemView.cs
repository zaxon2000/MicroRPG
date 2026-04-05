using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    public abstract class BaseGridItemView : BaseItemView {
        public int CellSize = 64;
    }

    public class GridItemView : BaseGridItemView {

        protected VisualElement shape = Dom.Div("absolute item-shape");

        public GridItemView() { this.Add("item-view", shape, image, quant); }

        override public void Render() {
            if (item == null) { return; }
            image.sprite = item.Icon;
            quant.text = item.StackSize.ToString();
            quant.SetVisible(item.Stackable);

            this.SetSize(item.Size(), CellSize);

            if (item is ShapeItem shapeItem) {
                image.SetSize(shapeItem.BaseSize, CellSize);
                image.Rotate((float)shapeItem.Direction);
                image.Translate(GridMath.AdjustPosForSizeAndDir(shapeItem.BaseSize, shapeItem.Direction), CellSize);
            } else {
                image.SetSize(Item.Size(), CellSize);
                image.Rotate(0);
                image.Translate(0, 0);
            }
        }
    }
}