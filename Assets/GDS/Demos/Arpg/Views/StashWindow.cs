using System.Linq;
using GDS.Core;
using GDS.Core.Events;
using UnityEngine.UIElements;

namespace GDS.Demos.Arpg {

    public class StashWindow : WindowView {

        public StashWindow(Stash bag, Store store) {
            Init(bag.Name, () => store.Bus.Publish(new CloseWindow(bag)));

            void onTabChange(int i) => bag.CurrentIndex.SetValue(i);
            VisualElement CreateTabButton(Bag b, int index) => Dom.Button("tab-button", $"{b.Name}({index})", () => onTabChange(index));

            var TabContent = Dom.Div();
            var TabButtons = bag.Tabs.Select(CreateTabButton).ToArray();
            var TabBar = Dom.Div("row gap-v-10").Add(TabButtons);


            bag.CurrentIndex.Reset();
            Container.Add(TabBar, TabContent);

            this.Observe(bag.CurrentIndex, i => {
                TabContent.Clear();
                var view = new GridBagView().Init(bag.Current, store.Ghost, false);
                TabContent.Add(view);
                foreach (var btn in TabButtons) btn.WithoutClass("selected");
                TabButtons[i].WithClass("selected");
            });
        }
    }

}