using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Core.Events;

namespace GDS.Demos.Dayz {
    [System.Serializable]
    public class Equipment : SetBag {

        public SetSlot Weapon1 = new(nameof(Weapon1));
        public SetSlot Weapon2 = new(nameof(Weapon2));
        public SetSlot Back = new(nameof(Back));
        public SetSlot Vest = new(nameof(Vest));
        public SetSlot Feet = new(nameof(Feet));
        public SetSlot Head = new(nameof(Head));

        public Equipment() {
            Name = "Equipment";
            Slots = new() { Weapon1, Weapon2, Back, Vest, Feet, Head };
        }

        IEnumerable<Dayz_ContainerItem> ContainerItems => Items.OfType<Dayz_ContainerItem>();

        public Result TryToFitItem(Item item) {
            // try to find a suitable slot in equipment slots first
            // this is essentially an "auto-equip" mechanic
            foreach (var slot in Slots) {
                if (slot.Full()) continue;
                if (slot.Tags.Intersect(item.Base.Tags).Count() > 0) {
                    var result = AddAt(slot, item);
                    if (result is Success) return result;
                }
            }

            // could not find any equipment slots that could fit the item
            // traverse all equipment items that are containers and try to fit into one of those
            foreach (var ci in ContainerItems) {
                var result = ci.GridCapacity.Add(item);
                if (result is Success) return result;
            }

            // could not find any bags/slots that could fit the item
            return Result.Fail;
        }
    }

}