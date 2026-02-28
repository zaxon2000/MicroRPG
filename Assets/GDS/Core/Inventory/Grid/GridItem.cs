namespace GDS.Core {
    public class GridItem {
        public Item Item;
        public Pos Pos;
        public GridItem(Item item, Pos pos) => (Item, Pos) = (item, pos);
    }

    public static class GridItemExt {
        public static readonly int[,] Rect1x1 = new int[1, 1] { { 1 } };
        public static readonly Size Size1x1 = new(1, 1);
        public static int[,] Shape(this Item item) => item is ShapeItem i ? i.Shape : Rect1x1;
        public static Size Size(this Item item) => item is ShapeItem i ? i.Size : Size1x1;
    }
}