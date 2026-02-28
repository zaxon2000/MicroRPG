using System;
using System.Collections.Generic;
using System.Linq;
using GDS.Core;
using GDS.Core.Events;

namespace GDS.Demos.Arpg {

    [Serializable]
    public class Stash : Bag {
        public Observable<int> CurrentIndex = new(0);
        public GridBag Current => Tabs.ElementAtOrDefault(CurrentIndex.Value);

        public List<GridBag> Tabs = new() {
            new GridBag() { Name = "Tab1", Size = new(10,10) },
            new GridBag() { Name = "Tab2", Size = new(10,10) },
        };

        public void SetCurrentIndex(int i) => CurrentIndex.SetValue(i);

        public override Result Add(Item item) {
            return Current.Add(item);
        }

        public override Result CanAdd(Item item) {
            return Current.CanAdd(item);
        }
    }
}
