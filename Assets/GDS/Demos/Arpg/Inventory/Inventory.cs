using System.Collections;
using System.Collections.Generic;
using GDS.Core;
using UnityEngine;

namespace GDS.Demos.Arpg {

    [System.Serializable]
    public class Inventory : GridBag {
        public Inventory() {
            Size = new(12, 6);
        }
    }
}
