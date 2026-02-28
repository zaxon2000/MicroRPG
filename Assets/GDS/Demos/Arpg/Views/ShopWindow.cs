using GDS.Core;
using GDS.Core.Events;

namespace GDS.Demos.Arpg {

    public class ShopWindow : WindowView {

        public ShopWindow(Shop bag, Store store) {
            Init(bag.Name, () => store.Bus.Publish(new CloseWindow(bag)));
            var shop = new GridBagView().WithName("Shop").Init(bag, store.Ghost, false);
            Container.Add(shop);
        }
    }

}