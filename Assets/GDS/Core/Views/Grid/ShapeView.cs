using UnityEngine.UIElements;

namespace GDS.Core {
    public class ShapeView : VisualElement {
        public ShapeView(int[,] shape, int cellSize) {
            var (h, w) = shape.GetLength2D();
            for (var i = 0; i < h; i++) {
                for (var j = 0; j < w; j++) {
                    if (shape[i, j] == 0) continue;
                    Add(ShapeCell().Translate(j * cellSize, i * cellSize).SetSize(cellSize));
                }
            }
        }

        VisualElement ShapeCell() => Dom.Div("absolute shape-cell").PickIgnore();
    }
}