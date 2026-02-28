using GDS.Common.Events;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine;

namespace GDS.Demos.Dayz {

    [CreateAssetMenu(menuName = "SO/Demos/Dayz/Dayz_Store")]
    public class Dayz_Store : Store {
        public Dayz_Store() {
            Bus.On<PickItem>(OnPickItem);
            Bus.On<PlaceItem>(OnPlaceItem);

            Bus.On<PickWorldItem>(OnPickWorldItem);
            Bus.On<DropWorldItem>(OnDropWorldItem);
        }


        public Equipment Equipment { get; private set; }
        public Hotbar Hotbar { get; private set; }
        public Hands Hands { get; private set; }

        public void Init(Equipment equipment, Hands hands, Hotbar hotbar) {
            Equipment = equipment;
            Hands = hands;
            Hotbar = hotbar;
        }


        public override void Reset() {
            base.Reset();
        }

        void OnPickItem(PickItem e) {
            Result result = e.Bag.Remove(e.Item);
            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnPlaceItem(PlaceItem e) {
            Result result = e.Bag.AddAt(e.Slot, e.Item);
            UpdateGhost(result);
            Bus.Publish(result);
        }

        void OnDropWorldItem(DropWorldItem e) {
            if (Ghost.Value == null) return;
            Bus.Publish(new DropWorldItemSuccess(Ghost.Value));
            Ghost.Reset();
        }

        void OnPickWorldItem(PickWorldItem e) {
            Debug.Log("Should find a bag that fits the item");
            var result = Equipment.TryToFitItem(e.WorldItem.Item);
            Bus.Publish(result.MapTo(new PickWorldItemSuccess(e.WorldItem)));
        }

    }

}