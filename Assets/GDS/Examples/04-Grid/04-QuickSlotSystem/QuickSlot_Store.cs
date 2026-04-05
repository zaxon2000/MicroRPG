using System.Linq;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine;
namespace GDS.Examples {

    [CreateAssetMenu(menuName = "SO/Examples/QuickSlot/QuickSlot_Store")]
    public class QuickSlot_Store : Store {
        public QuickSlot_Store() {
            Bus.On<PickItem>(OnPickItem);
            Bus.On<PlaceItem>(OnPlaceItem);
        }

        public QuickSlot_PlayerInventory PlayerInventory { get; private set; }
        public QuickSlot_Shortcuts Shortcuts { get; private set; }
        public Observable<Item> Hands { get; private set; }

        Bag lastBag;
        Slot lastSlot;

        public void Init(QuickSlot_PlayerInventory playerInventory, QuickSlot_Shortcuts shortcuts, Observable<Item> hands) {
            PlayerInventory = playerInventory;
            Shortcuts = shortcuts;
            Hands = hands;
        }

        void OnPickItem(PickItem e) {
            lastBag = e.Bag;
            lastSlot = e.Bag.FindSlot(e.Item);

            if (e.Bag is QuickSlot_Shortcuts) {
                e.Bag.Remove(e.Item);
                return;
            }

            Result result = e.Bag.Remove(e.Item);
            UpdateGhost(result);
            Bus.Publish(result);


        }

        void OnPlaceItem(PlaceItem e) {
            Result result = e.Bag.AddAt(e.Slot, e.Item);
            UpdateGhost(result);
            Bus.Publish(result);

            if (e.Bag is not QuickSlot_PlayerInventory && lastBag is QuickSlot_PlayerInventory) {
                lastBag.AddAt(lastSlot, e.Item);
            }
            lastBag = null;
            lastSlot = null;
        }

        public void OnShortcutPressed(int index) {
            Debug.Log("on shortcut " + index);
            var slot = Shortcuts.Slots.ElementAt(index);
            if (slot == null) return;
            // Clone into "Hands" with same id
            Hands.SetValue(slot.Item?.Clone(true));
        }

        public void OnUseCurrentItem() {
            // use current item / weapon
            if (Hands.Value is not QuickSlot_Weapon i) return;
            if (i.Ammo == 0) return;
            i.Ammo -= 1;
            Hands.Notify();
            PlayerInventory.UpdateItem(i);
            Shortcuts.UpdateItem(i);
        }
    }

}