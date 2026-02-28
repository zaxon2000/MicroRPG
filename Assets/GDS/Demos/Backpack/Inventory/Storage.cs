using System;
using GDS.Core;

namespace GDS.Demos.Backpack {
    [Serializable]
    public class Storage : DenseListBag {
        public Storage() => Size = 40;
    }

}