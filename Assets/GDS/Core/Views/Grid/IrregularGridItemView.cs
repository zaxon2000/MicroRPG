using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Core {
    public class IrregularGridItemView : ItemView {

        protected VisualElement shapeContainer;
        public int CellSize = 64;

        override public void Render() {
            base.Render();

            this.SetSize(Item.Size(), CellSize);

            if (shapeContainer == null) {
                shapeContainer = Dom.Div("absolute").PickIgnoreAll();
                Insert(0, shapeContainer);
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


            shapeContainer.Clear();
            shapeContainer.Add(new ShapeView(Item.Shape(), CellSize));
        }
    }
}