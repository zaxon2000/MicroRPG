using GDS.Core;
using GDS.Core.Events;

namespace GDS.Examples {
    [System.Serializable]
    public class QuickSlot_Shortcuts : ListBag {
        public void UpdateItem(Item item) {
            var slot = Slots.Find(s => s.Item?.Id == item.Id);
            if (slot == null) return;
            slot.Item = item;
            NotifyChanged(slot);
        }

        public override Result AddAt(Slot slot, Item item) {
            // Remove duplicate if it already exists
            var lastSlot = Slots.Find(s => s.Item?.Id == item.Id);
            if (lastSlot != null) {
                lastSlot.Clear();
                NotifyChanged(lastSlot);
            }

            // Clone with same id
            slot.Item = item.Clone(true);
            NotifyChanged((ListSlot)slot);
            return new PlaceItemSuccess(item, null);
        }
    }
}
