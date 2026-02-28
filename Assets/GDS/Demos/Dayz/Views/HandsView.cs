using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Demos.Dayz {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]    
#endif

    public partial class HandsView : VisualElement {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<HandsView> { }
#endif

        public HandsView() {
            var uxml = Resources.Load<VisualTreeAsset>("DayzHandsView");
            uxml.CloneTree(this);

            bagView = this.Q<ListBagView>();
            container = this.Q<VisualElement>("Container");
        }

        Hands Hands;
        ListBagView bagView;
        VisualElement container;
        Store Store;

        public void Init(Hands hands, Store store) {
            Hands = hands;
            Store = store;

            bagView.Init(hands);
            Hands.ItemChanged += OnItemChanged;

            if (Hands.Slot.Full()) container.Add(TryCreateContainerItemView(Hands.Slot.Item));
        }

        ContainerItemView TryCreateContainerItemView(Item item) {
            if (item is Dayz_ContainerItem c) return new ContainerItemView(c, Store);
            return null;
        }

        void OnItemChanged(ListSlot slot) {
            container.Clear();
            if (!slot.Empty()) {
                container.Add(TryCreateContainerItemView(slot.Item));
            }
        }
    }
}