using GDS.Core;
using UnityEngine;

namespace GDS.Examples {

    [CreateAssetMenu(menuName = "SO/Examples/Arc/Arc_ItemBase")]
    public class Arc_ItemBase : ShapeItemBase {
        public override Item CreateItem() => new Arc_Item { Base = this, Name = Name, StackSize = MaxStackSize };
    }

    [System.Serializable]
    public class Arc_Item : ShapeItem {
        public bool IsIdentified = true;
    }
}
