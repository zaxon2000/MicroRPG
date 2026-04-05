using UnityEngine.UIElements;

namespace GDS.Core {
    public class GridGhostView : VisualElement {
        int CellSize;
        bool HasIrregularShapes = true;
        Pos lastPos;
        string itemId;
        Direction itemDirection;
        VisualElement cell = Dom.Div("shape-cell");

        public void Init(int cellSize, bool hasIrregularShapes) {
            CellSize = cellSize;
            HasIrregularShapes = hasIrregularShapes;
            if (hasIrregularShapes == false) { Add(cell); }
        }

        public void Build(Item item) {
            if (item == null) { itemId = null; return; }
            if (item.Id == itemId && itemDirection == item.Direction()) return;
            itemId = item.Id;
            itemDirection = item.Direction();
            if (HasIrregularShapes) {
                Clear();
                Add(new ShapeView(item.Shape(), CellSize));
            } else {
                cell.SetSize(item.Size(), CellSize);
            }
        }

        public void Translate(Pos pos) {
            if (pos == lastPos) return;
            lastPos = pos;
            this.Translate(pos, CellSize);
        }
    }

}