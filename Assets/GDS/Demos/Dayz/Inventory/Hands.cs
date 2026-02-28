using System.Collections;
using System.Collections.Generic;
using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Dayz {

    public class Hands : ListBag {
        public Hands() { Size = 1; }
        public ListSlot Slot => Slots[0];
    }

}