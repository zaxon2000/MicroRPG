using GDS.Core;

namespace GDS.Examples {
    [System.Serializable]
    public class QuickSlot_PlayerInventory : GridBag {
        public void UpdateItem(Item item) {
            var inventoryItem = GridItems.Find(gi => gi.Item.Id == item.Id)?.Item;
            if (inventoryItem == null) return;
            if (inventoryItem.CopyFrom(item)) {
                NotifyChanged(inventoryItem);
            }
        }
    }
}
