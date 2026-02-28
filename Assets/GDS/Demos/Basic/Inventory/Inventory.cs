using System;
using GDS.Core;

namespace GDS.Demos.Basic {

    [Serializable]
    public class Inventory : GridBag {
        public Inventory() {
            Name = "Inventory";
            Size = new Size(10, 4);
            CellSize = 48;
        }
    }

}