using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    public class GridItemView : ItemView {

        VisualElement shape;
        public int CellSize = 64;

        override public void Render() {
            base.Render();

            this.SetSize(Item.Size(), CellSize);

            if (shape == null) {
                shape = Dom.Div("absolute item-shape").PickIgnoreAll();
                Insert(0, shape);
            }

            if (Item is ShapeItem shapeItem) {
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