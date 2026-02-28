using UnityEngine;

namespace GDS.Core {
    [CreateAssetMenu(menuName = "SO/Core/ShapeItemBase")]
    public class ShapeItemBase : ItemBase {
        public Shape Shape;
        override public Item CreateItem() => new ShapeItem() { Base = this, Name = Name, StackSize = MaxStackSize };
    }

    [System.Serializable]
    public class ShapeItem : Item {
        public Direction Direction = Direction.N;
        public int[,] BaseShape => (Base as ShapeItemBase).Shape.Mask;
        public int[,] Shape => BaseShape.Rotate(Direction);
        public Size BaseSize => GridMath.GetSize(BaseShape);
        public Size Size => GridMath.GetSize(Shape);
    }

    public static class ShapeItemExt {
        public static Direction Direction(this Item item) => item is ShapeItem i ? i.Direction : Core.Direction.N;
    }
}