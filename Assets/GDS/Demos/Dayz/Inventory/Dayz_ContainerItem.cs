using System.Linq;
using GDS.Core;
using GDS.Demos.Dayz;

namespace GDS.Demos.Dayz {
    [System.Serializable]
    public class Dayz_ContainerItem : Dayz_Item {
        public ListBag ListCapacity;
        public GridBag GridCapacity;

        public static Dayz_ContainerItem Create(Dayz_ContainerItemBase itemBase) {
            var listSlots = itemBase.ListCapacity.Select((t, index) => CreateListSlot(t, index)).ToList();
            var list = new ListBag { Slots = listSlots };
            var grid = new GridBag { Size = itemBase.GridCapacity };
            return new Dayz_ContainerItem() { Base = itemBase, Name = itemBase.Name, ListCapacity = list, GridCapacity = grid };
        }

        static ListSlot CreateListSlot(Dayz_Tag tag, int index) {
            var slot = new ListSlot() { Index = index };
            slot.Tags.Add(tag);
            return slot;
        }
    }
}
