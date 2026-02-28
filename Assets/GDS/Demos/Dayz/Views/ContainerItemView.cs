using GDS.Core;
using UnityEngine.UIElements;

namespace GDS.Demos.Dayz {
    public class ContainerItemView : VisualElement {

        public int Index;
        public ContainerItemView(Dayz_ContainerItem item, Store store) {
            AddToClassList("mt-20");
            var panelTitle = PanelTitle(item);
            var listBagView = new ListBagView().Init(item.ListCapacity, 0, "", () => new CapacitySlotView());
            var gridBagView = new GridBagView().Init(item.GridCapacity, store.Ghost);

            this.Add(panelTitle, listBagView, gridBagView);
        }

        VisualElement PanelTitle(Dayz_ContainerItem item) {
            var el = Dom.Div("container-item-header");
            var image = new Image { sprite = item.Base.Icon }.WithClass("container-item-header-icon");
            var label = Dom.Label("container-item-header-text", item.Name);
            el.Add(image, label);
            return el;
        }

    }
}