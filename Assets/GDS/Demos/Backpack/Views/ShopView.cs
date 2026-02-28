using System.Collections.Generic;
using GDS.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GDS.Demos.Backpack {

#if UNITY_6000_0_OR_NEWER
    [UxmlElement]    
#endif

    public partial class ShopView : VisualElement {

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ShopView> { }
#endif

        public ShopView() : base() {
            var uxml = Resources.Load<VisualTreeAsset>("Backpack_ShopView");
            uxml.CloneTree(this);
            rerollButton = this.Q<Button>("RerollButton");
            slots = this.Query<ShopSlotView>().ToList();
        }

        Button rerollButton;
        List<ShopSlotView> slots;
        Shop Shop;
        Store Store;
        int CellSize = 80;

        /// <summary>Initialises the shop view. Accepts any Store subclass so the view can be reused outside the Backpack demo.</summary>
        public void Init(Shop shop, Store store) {
            Shop = shop;
            Store = store;

            shop.CollectionReset += OnCollectionReset;
            shop.ItemChanged += OnItemChanged;
            rerollButton.RegisterCallback<ClickEvent>(_ => Store.Bus.Publish(new RerollShop()));

            foreach (var slot in Shop.Slots) {
                slots[slot.Index].Init(shop, slot, CellSize);
                slots[slot.Index].Render();
            }
        }

        void OnCollectionReset() {
            foreach (var slot in Shop.Slots) {
                slots[slot.Index].Render();
            }
        }

        void OnItemChanged(ListSlot slot) {
            slots[slot.Index].Render();
        }

    }
}
