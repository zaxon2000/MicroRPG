using System;
using GDS.Core;

namespace GDS.Demos.Backpack {

    [Serializable]
    public class Backpack : GridBag {
        public Backpack() => Size = new(8, 8);
    }

}