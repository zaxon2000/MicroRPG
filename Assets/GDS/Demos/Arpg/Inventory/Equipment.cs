using GDS.Core;

namespace GDS.Demos.Arpg {
    [System.Serializable]
    public class Equipment : SetBag {

        public SetSlot Weapon1 = new(nameof(Weapon1));
        public SetSlot Weapon2 = new(nameof(Weapon2));
        public SetSlot Helmet = new(nameof(Helmet));
        public SetSlot Body = new(nameof(Body));
        public SetSlot Boots = new(nameof(Boots));
        public SetSlot Gloves = new(nameof(Gloves));
        public SetSlot RingLeft = new(nameof(RingLeft));
        public SetSlot RingRight = new(nameof(RingRight));

        public Equipment() {
            Name = "Equipment";
            Slots = new() { Weapon1, Weapon2, Helmet, Body, Boots, Gloves, RingLeft, RingRight };
        }
    }

}